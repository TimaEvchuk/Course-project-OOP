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

            // In a real app, this would involve payment processing.
            // Here, we simulate a successful purchase.

            var userToUpdate = await _unitOfWork.Users.GetByIdAsync(_authenticationService.CurrentUser.Id);
            if (userToUpdate == null)
            {
                MessageBox.Show("Ошибка: Не удалось найти текущего пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            userToUpdate.IsPremium = true;
            userToUpdate.PremiumStartDate = DateTime.Today;
            userToUpdate.PremiumEndDate = DateTime.Today.AddMonths(1); // 1 month subscription

            _unitOfWork.Users.Update(userToUpdate);
            await _unitOfWork.CompleteAsync();

            // Update the CurrentUser in AuthenticationService to reflect the changes
            _authenticationService.CurrentUser.IsPremium = true;
            _authenticationService.CurrentUser.PremiumStartDate = userToUpdate.PremiumStartDate;
            _authenticationService.CurrentUser.PremiumEndDate = userToUpdate.PremiumEndDate;

            // Send notification and update messages
            var enableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
            if (enableSuccessNotifications)
            {
                var successNotification = new Notification
                {
                    UserId = _authenticationService.CurrentUser.Id,
                    Message = "Вы перешли на тариф 'Премиум'!",
                    Timestamp = DateTime.Now,
                    Type = Models.Enums.NotificationType.ActionSuccess
                };
                await _unitOfWork.Notifications.AddAsync(successNotification);
                await _unitOfWork.CompleteAsync();

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
