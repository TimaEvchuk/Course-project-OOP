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

            // 1. Update user properties
            userToUpdate.IsPremium = true;
            userToUpdate.PremiumStartDate = DateTime.Today;
            userToUpdate.PremiumEndDate = DateTime.Today.AddMonths(1);

            // 2. Save user update to the database
            await _unitOfWork.CompleteAsync();

            // 3. Update the CurrentUser in AuthenticationService to reflect the changes in the current session
            _authenticationService.CurrentUser.IsPremium = true;
            _authenticationService.CurrentUser.PremiumStartDate = userToUpdate.PremiumStartDate;
            _authenticationService.CurrentUser.PremiumEndDate = userToUpdate.PremiumEndDate;

            // 4. Create a notification object BUT DO NOT SAVE IT HERE
            var enableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
            if (enableSuccessNotifications)
            {
                var successNotification = new Notification
                {
                    Message = "Вы перешли на тариф 'Премиум'!",
                    Type = Models.Enums.NotificationType.ActionSuccess
                    // UserId and Timestamp will be set by the receiver (MainViewModel)
                };
                // 5. Send the unsaved notification to the hub (MainViewModel) which will save it
                _messenger.Send(new NewNotificationMessage(successNotification));
            }
            
            // 6. Send other UI-related messages
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
