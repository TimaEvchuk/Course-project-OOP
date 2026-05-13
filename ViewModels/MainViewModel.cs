using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using Plantify.Models;
using Plantify.Data;
using Plantify.Services;

namespace Plantify.ViewModels
{
    public partial class MainViewModel : BaseViewModel, 
        IRecipient<NavigateMessage>, 
        IRecipient<ShowPlantDetailMessage>, 
        IRecipient<CloseOverlayMessage>,
        IRecipient<UserLoggedInMessage>,
        IRecipient<NotificationsUpdatedMessage>,
        IRecipient<CloseNotificationsPanelMessage>,
        IRecipient<ActionCompletedMessage>,
        IRecipient<ShowAddUserPlantOverlayMessage>
    {
        [ObservableProperty]
        private BaseViewModel _currentViewModel = null!;

        [ObservableProperty]
        private string _currentPageTitle = "";

        [ObservableProperty]
        private BaseViewModel? _overlayViewModel;

        [ObservableProperty]
        private bool _isLoggedIn = false;
        
        [ObservableProperty]
        private bool _isNotificationsPanelOpen;

        [ObservableProperty]
        private bool _hasNewNotifications;

        private readonly IServiceProvider _serviceProvider;
        private readonly IMessenger _messenger;

        public NotificationViewModel NotificationViewModel { get; }

        public string NotificationIconPath => HasNewNotifications 
            ? "pack://application:,,,/Images/icons/bell-active.png" 
            : "pack://application:,,,/Images/icons/bell-default.png";

        public MainViewModel(IServiceProvider serviceProvider, IMessenger messenger, NotificationViewModel notificationViewModel)
        {
            _serviceProvider = serviceProvider;
            _messenger = messenger;
            NotificationViewModel = notificationViewModel;

            _messenger.RegisterAll(this);

            Navigate(typeof(LoginViewModel), "Вход");
        }

        partial void OnHasNewNotificationsChanged(bool value)
        {
            OnPropertyChanged(nameof(NotificationIconPath));
        }

        partial void OnIsNotificationsPanelOpenChanged(bool value)
        {
            if (value)
            {
                HasNewNotifications = false;
            }
        }

        private void Navigate(Type viewModelType, string pageTitle)
        {
            CurrentViewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
            CurrentPageTitle = pageTitle;
        }

        public void Receive(NavigateMessage message)
        {
            if (message.Value == typeof(EncyclopediaViewModel))
            {
                Navigate(typeof(EncyclopediaViewModel), "Энциклопедия");
            }
            else if (message.Value == typeof(PlantManagementViewModel))
            {
                Navigate(typeof(PlantManagementViewModel), "Управление каталогом");
            }
            else if (message.Value == typeof(RegisterViewModel))
            {
                Navigate(typeof(RegisterViewModel), "Регистрация");
            }
            else if (message.Value == typeof(LoginViewModel))
            {
                IsLoggedIn = false;
                Navigate(typeof(LoginViewModel), "Вход");
            }
            else if (message.Value == typeof(MyGardenViewModel))
            {
                Navigate(typeof(MyGardenViewModel), "Мой сад");
            }
        }

        public void Receive(ShowPlantDetailMessage message)
        {
            OverlayViewModel = new PlantDetailViewModel(message.Value, _messenger);
        }


        public void Receive(CloseOverlayMessage message)
        {
            OverlayViewModel = null;
        }
        
        public void Receive(CloseNotificationsPanelMessage message)
        {
            IsNotificationsPanelOpen = false;
        }

        public void Receive(ActionCompletedMessage message)
        {
            NotificationViewModel.AddActionCompletedNotification(message.Message);
            HasNewNotifications = true;
        }

        public void Receive(UserLoggedInMessage message)
        {
            IsLoggedIn = true;
            Navigate(typeof(MyGardenViewModel), "Мой сад");
        }
        
        public void Receive(NotificationsUpdatedMessage message)
        {
            NotificationViewModel.UpdateNotifications(message.WateringCount, message.FertilizingCount);
            if (NotificationViewModel.HasNotifications)
            {
                HasNewNotifications = true;
            }
        }

        public void Receive(ShowAddUserPlantOverlayMessage message)
        {
            var addUserPlantVM = _serviceProvider.GetRequiredService<AddUserPlantViewModel>();
            addUserPlantVM.Initialize(message);
            
            OverlayViewModel = addUserPlantVM;
            
            // Load data *after* the overlay is set
            addUserPlantVM.LoadAllPlantsCommand.Execute(null);
        }

        [RelayCommand]
        private void ToggleNotificationsPanel()
        {
            IsNotificationsPanelOpen = !IsNotificationsPanelOpen;
        }

        [RelayCommand]
        private void ShowAddPlantOverlay()
        {
            _messenger.Send(new ShowAddUserPlantOverlayMessage((Models.Plant?)null));
        }

        [RelayCommand]
        private void GoToMyGarden() => Navigate(typeof(MyGardenViewModel), "Мой сад");

        [RelayCommand]
        private void GoToEncyclopedia() => Navigate(typeof(EncyclopediaViewModel), "Энциклопедия");

        [RelayCommand]
        private void GoToSchedule() => Navigate(typeof(ScheduleViewModel), "Расписание");
        
        [RelayCommand]
        private void GoToSettings() => Navigate(typeof(PlantManagementViewModel), "Управление каталогом");
    }
}
