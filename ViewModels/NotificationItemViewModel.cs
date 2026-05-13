using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plantify.Models;
using System;

namespace Plantify.ViewModels
{
    public partial class NotificationItemViewModel : BaseViewModel
    {
        private readonly Action<NotificationItemViewModel> _dismissAction;
        private readonly Notification _notification;

        public int NotificationId => _notification.Id;
        public string Text => _notification.Message;
        public DateTime Timestamp => _notification.Timestamp;

        public NotificationItemViewModel(Notification notification, Action<NotificationItemViewModel> dismissAction)
        {
            _notification = notification;
            _dismissAction = dismissAction;
        }

        [RelayCommand]
        private void Dismiss()
        {
            _dismissAction?.Invoke(this);
        }
    }
}
