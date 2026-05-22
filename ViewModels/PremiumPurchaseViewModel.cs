using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Services;
using System;
using System.Threading.Tasks;
using System.Windows; // For MessageBox

namespace Plantify.ViewModels
{
    public partial class PremiumPurchaseViewModel : BaseViewModel
    {
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public PremiumPurchaseViewModel(IMessenger messenger, AuthenticationService authenticationService, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _messenger = messenger;
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        [RelayCommand]
        private async Task BuyPremium()
        {
            if (_authenticationService.CurrentUser == null)
            {
                MessageBox.Show("Ошибка: Пользователь не авторизован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var userToUpdate = await _unitOfWork.Users.GetByIdAsync(_authenticationService.CurrentUser.Id);
            if (userToUpdate == null)
            {
                MessageBox.Show("Ошибка: Не удалось найти текущего пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            userToUpdate.IsPremium = true;
            userToUpdate.PremiumStartDate = DateTime.Today;
            userToUpdate.PremiumEndDate = DateTime.Today.AddMonths(1);

            Notification? successNotification = null;
            var enableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
            if (enableSuccessNotifications)
            {
                successNotification = new Notification
                {
                    Message = "Вы перешли на тариф 'Премиум'!",
                    Type = Models.Enums.NotificationType.ActionSuccess,
                    UserId = userToUpdate.Id,
                    Timestamp = DateTime.Now
                };
                await _unitOfWork.Notifications.AddAsync(successNotification);
            }
            
    
            await _unitOfWork.CompleteAsync();
            _unitOfWork.DetachAllEntities();

   
            _authenticationService.CurrentUser.IsPremium = true;
            _authenticationService.CurrentUser.PremiumStartDate = userToUpdate.PremiumStartDate;
            _authenticationService.CurrentUser.PremiumEndDate = userToUpdate.PremiumEndDate;

            if (successNotification != null)
            {
                _messenger.Send(new NewNotificationMessage(successNotification));
            }
            
            _messenger.Send(new CloseOverlayMessage());
            _messenger.Send(new PremiumStatusChangedMessage(userToUpdate));
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }
    }
}
