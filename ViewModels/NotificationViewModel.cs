using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class NotificationViewModel : BaseViewModel
    {
        private readonly IMessenger _messenger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AuthenticationService _authenticationService;

        [ObservableProperty]
        private ObservableCollection<NotificationItemViewModel> _notifications = new();

        public NotificationViewModel(IMessenger messenger, IServiceScopeFactory scopeFactory, AuthenticationService authenticationService)
        {
            _messenger = messenger;
            _scopeFactory = scopeFactory;
            _authenticationService = authenticationService;

            LoadNotificationsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadNotifications()
        {
            if (_authenticationService.CurrentUser == null) return;
            
            using (var scope = _scopeFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var notifications = await unitOfWork.Notifications.GetAllAsync(
                    filter: n => n.UserId == _authenticationService.CurrentUser.Id && !n.IsDismissed,
                    orderBy: q => q.OrderByDescending(n => n.Timestamp)
                );

                Notifications.Clear();
                foreach (var notification in notifications)
                {
                    Notifications.Add(new NotificationItemViewModel(notification, DismissNotification));
                }
            }
            OnPropertyChanged(nameof(HasNotifications));
        }

        public void AddNewNotification(Notification notification)
        {
            var newNotificationVm = new NotificationItemViewModel(notification, DismissNotification);
            Notifications.Insert(0, newNotificationVm);
            OnPropertyChanged(nameof(HasNotifications));
        }

        private async void DismissNotification(NotificationItemViewModel item)
        {
            if (Notifications.Contains(item))
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var notificationInDb = await unitOfWork.Notifications.GetByIdAsync(item.NotificationId);
                    if (notificationInDb != null)
                    {
                        unitOfWork.Notifications.Delete(notificationInDb);
                        await unitOfWork.CompleteAsync();
                        Notifications.Remove(item);
                        OnPropertyChanged(nameof(HasNotifications));
                    }
                }
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
