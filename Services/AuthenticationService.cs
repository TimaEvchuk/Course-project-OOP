using System;
using System.Threading.Tasks;
using Plantify.Data;
using Plantify.Models;
using BCrypt.Net; // For password hashing
using Microsoft.EntityFrameworkCore;
using System.Linq; // For .FirstOrDefault() and .Any()
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;

namespace Plantify.Services
{
    public class AuthenticationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;

        public User? CurrentUser { get; private set; }

        public AuthenticationService(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
        }

        public async Task<bool> SignIn(string login, string password)
        {
            var user = (await _unitOfWork.Users.GetAllAsync(
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
                    _unitOfWork.Users.Update(user);

                    var expiredNotification = new Notification
                    {
                        UserId = user.Id,
                        Message = "Подписка на тариф 'Премиум' истекла!",
                        Timestamp = DateTime.Now,
                        Type = Models.Enums.NotificationType.NeedsCare
                    };
                    await _unitOfWork.Notifications.AddAsync(expiredNotification);
                    await _unitOfWork.CompleteAsync();

                    // Send message to update UI
                    _messenger.Send(new NewNotificationMessage(expiredNotification));
                }

                CurrentUser = user;
                return true;
            }
            CurrentUser = null;
            return false;
        }

        public async Task<bool> Register(string login, string email, string password)
        {
            // Check if user already exists
            if ((await _unitOfWork.Users.FindAsync(u => u.Login == login)).Any())
            {
                // User with this login already exists
                return false;
            }

            // Get default role (e.g., "Клиент")
            var defaultRole = (await _unitOfWork.Roles.FindAsync(r => r.Name == "Клиент")).FirstOrDefault();
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

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            CurrentUser = newUser; // Auto-sign in after registration
            return true;
        }

        public void SignOut()
        {
            CurrentUser = null;
        }

        public bool IsCurrentUserPremium()
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
