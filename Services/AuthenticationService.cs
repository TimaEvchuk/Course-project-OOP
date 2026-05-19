using System;
using System.Threading.Tasks;
using Plantify.Data;
using Plantify.Models;
using BCrypt.Net; // For password hashing
using Microsoft.EntityFrameworkCore;
using System.Linq; // For .FirstOrDefault() and .Any()
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Plantify.Services
{
    public class AuthenticationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMessenger _messenger;

        public User? CurrentUser { get; private set; }

        public AuthenticationService(IServiceScopeFactory scopeFactory, IMessenger messenger)
        {
            _scopeFactory = scopeFactory;
            _messenger = messenger;
        }

        public async Task<bool> SignIn(string login, string password)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                
                var user = (await unitOfWork.Users.GetAllAsync(
                    filter: u => u.Login == login,
                    include: q => q.Include(u => u.Roles)))
                    .FirstOrDefault();

                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    // Check for expired subscription
                    if (user.IsPremium && user.PremiumEndDate.HasValue && user.PremiumEndDate.Value.Date < DateTime.Today)
                    {
                        user.IsPremium = false;
                        user.PremiumStartDate = null;
                        user.PremiumEndDate = null;
                        
                        var expiredNotification = new Notification
                        {
                            UserId = user.Id,
                            Message = "Подписка на тариф 'Премиум' истекла!",
                            Timestamp = DateTime.Now,
                            Type = Models.Enums.NotificationType.Warning
                        };
                        await unitOfWork.Notifications.AddAsync(expiredNotification);
                        _messenger.Send(new NewNotificationMessage(expiredNotification));
                        
                        // Trim garden to 5 plants
                        var userPlants = (await unitOfWork.UserPlants.FindAsync(p => p.UserId == user.Id)).ToList();
                        if (userPlants.Count > 5)
                        {
                            var plantsToDelete = userPlants.OrderByDescending(p => p.Id).Skip(5).ToList();
                            unitOfWork.UserPlants.RemoveRange(plantsToDelete);
                            
                            var trimNotification = new Notification
                            {
                                UserId = user.Id,
                                Message = $"Ваш сад был сокращен до 5 растений.",
                                Timestamp = DateTime.Now,
                                Type = Models.Enums.NotificationType.Warning
                            };
                            await unitOfWork.Notifications.AddAsync(trimNotification);
                            _messenger.Send(new NewNotificationMessage(trimNotification));
                        }

                        await unitOfWork.CompleteAsync();
                    }

                    CurrentUser = user;
                    return true;
                }
            }
            CurrentUser = null;
            return false;
        }

        public async Task<bool> Register(string login, string email, string password)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Check if user already exists
                if ((await unitOfWork.Users.FindAsync(u => u.Login == login)).Any())
                {
                    // User with this login already exists
                    return false;
                }

                // Get default role (e.g., "Клиент")
                var defaultRole = (await unitOfWork.Roles.FindAsync(r => r.Name == "Клиент")).FirstOrDefault();
                if (defaultRole == null)
                {
                    // Default role not found, this indicates a setup issue (e.g., roles not seeded)
                    return false;
                }

                var newUser = new User
                {
                    Login = login,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                };
                newUser.Roles.Add(defaultRole); // Assign default role

                await unitOfWork.Users.AddAsync(newUser);
                await unitOfWork.CompleteAsync();

                CurrentUser = newUser; // Auto-sign in after registration
                return true;
            }
        }

        public void Logout()
        {
            CurrentUser = null;
            AppState.NeedsCareNotifiedToday = false;
        }

        public bool IsPremiumActive()
        {
            if (CurrentUser == null || !CurrentUser.IsPremium)
            {
                return false;
            }

            // A null end date can be treated as a permanent subscription
            return CurrentUser.PremiumEndDate == null || CurrentUser.PremiumEndDate.Value.Date >= DateTime.Today;
        }
    }
}
