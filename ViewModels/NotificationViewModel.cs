using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;
using System.Collections.ObjectModel;

namespace Plantify.ViewModels
{
    public partial class NotificationViewModel : BaseViewModel
    {
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private ObservableCollection<NotificationItemViewModel> _notifications = new();

        public NotificationViewModel(IMessenger messenger)
        {
            _messenger = messenger;
        }

        public void UpdateNotifications(int wateringCount, int fertilizingCount)
        {
            // For now, we clear and add. In the future, we might want to avoid duplicates.
            Notifications.Clear();
            if (wateringCount > 0)
            {
                Notifications.Add(new NotificationItemViewModel($"Требуется полить {wateringCount} растений", RemoveNotification));
            }
            if (fertilizingCount > 0)
            {
                Notifications.Add(new NotificationItemViewModel($"Требуется удобрить {fertilizingCount} растений", RemoveNotification));
            }
            OnPropertyChanged(nameof(HasNotifications));
        }

        public void AddActionCompletedNotification(string message)
        {
            var notification = new NotificationItemViewModel(message, RemoveNotification);
            Notifications.Insert(0, notification);
            OnPropertyChanged(nameof(HasNotifications));
        }

        private void RemoveNotification(NotificationItemViewModel item)
        {
            if (Notifications.Contains(item))
            {
                Notifications.Remove(item);
                OnPropertyChanged(nameof(HasNotifications));
            }
        }

        public bool HasNotifications => Notifications.Count > 0;

        [RelayCommand]
        private void Close()
        {
            _messenger.Send(new CloseNotificationsPanelMessage());
        }
    }
}
