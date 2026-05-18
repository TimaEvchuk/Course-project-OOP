using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;
using Microsoft.Extensions.DependencyInjection;
using System;
using Plantify.Models;
using Plantify.Data;
using Plantify.Services;
using System.Linq;

namespace Plantify.ViewModels
{
    public partial class MainViewModel : BaseViewModel, 
        IRecipient<NavigateMessage>, 
        IRecipient<ShowPlantDetailMessage>, 
        IRecipient<CloseOverlayMessage>,
        IRecipient<UserLoggedInMessage>,
        IRecipient<NewNotificationMessage>,
        IRecipient<CloseNotificationsPanelMessage>,
        IRecipient<ShowAddUserPlantOverlayMessage>,
        IRecipient<ShowAddUserAdminOverlayMessage>,
        IRecipient<ShowPremiumPurchaseOverlayMessage>,
        IRecipient<ShowAddPlantSuggestionOverlayMessage>,
        IRecipient<ShowAddEditPlantOverlayMessage>,
        IRecipient<ShowChangePasswordOverlayMessage>,
        IRecipient<UserLoggedOutMessage>,
        IRecipient<UserAvatarChangedMessage>
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
        private bool _isAdmin = false;

        [ObservableProperty]
        private bool _isContentManager = false;
        
        [ObservableProperty]
        private bool _isNotificationsPanelOpen;

        [ObservableProperty]
        private bool _hasNewNotifications;
        
        [ObservableProperty]
        private string? _currentUserAvatarPath;

        private readonly IServiceProvider _serviceProvider;
        private readonly IMessenger _messenger;
        
        public User? CurrentUser { get; private set; }

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
                // Reload notifications when panel is opened to show the most recent state
                NotificationViewModel.LoadNotificationsCommand.Execute(null);
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
                CurrentUser = null;
                IsAdmin = false;
                IsContentManager = false;
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

        public async void Receive(NewNotificationMessage message)
        {
            if (CurrentUser == null) return;

            // Create a new Notification instance to ensure its ID is 0 and it's not being tracked
            var newNotification = new Notification
            {
                Message = message.Notification.Message,
                Type = message.Notification.Type,
                UserId = CurrentUser.Id,
                Timestamp = DateTime.UtcNow,
                IsDismissed = false // Default to not dismissed
            };

            using (var scope = _serviceProvider.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.Notifications.AddAsync(newNotification);
                await unitOfWork.CompleteAsync();
            }

            // Pass the newly created and saved notification (now with an Id from the DB) to the ViewModel
            NotificationViewModel.AddNewNotification(newNotification);
            HasNewNotifications = true;
        }

        public async void Receive(UserLoggedInMessage message)
        {
            CurrentUser = message.User;
            IsLoggedIn = true;
            IsAdmin = CurrentUser.Roles.Any(r => r.Name == "Администратор");
            IsContentManager = CurrentUser.Roles.Any(r => r.Name == "Контент-менеджер");
            CurrentUserAvatarPath = CurrentUser.AvatarPath;
            
            // Also load notifications on login
            await NotificationViewModel.LoadNotificationsCommand.ExecuteAsync(null);
            Navigate(typeof(MyGardenViewModel), "Мой сад");
        }
        
        public void Receive(ShowAddUserPlantOverlayMessage message)
        {
            var addUserPlantVM = _serviceProvider.GetRequiredService<AddUserPlantViewModel>();
            addUserPlantVM.Initialize(message);
            
            OverlayViewModel = addUserPlantVM;
            
            addUserPlantVM.LoadAllPlantsCommand.Execute(null);
        }

        public void Receive(ShowAddUserAdminOverlayMessage message)
        {
            OverlayViewModel = _serviceProvider.GetRequiredService<AddUserViewModel>();
        }

        public void Receive(ShowPremiumPurchaseOverlayMessage message)
        {
            OverlayViewModel = _serviceProvider.GetRequiredService<PremiumPurchaseViewModel>();
        }

        public void Receive(ShowAddPlantSuggestionOverlayMessage message)
        {
            OverlayViewModel = _serviceProvider.GetRequiredService<AddPlantSuggestionViewModel>();
        }

        public async void Receive(ShowAddEditPlantOverlayMessage message)
        {
            var vm = _serviceProvider.GetRequiredService<AddEditPlantViewModel>();
            await vm.InitializeAsync(message.Value);
            OverlayViewModel = vm;
        }

        public void Receive(ShowChangePasswordOverlayMessage message)
        {
            OverlayViewModel = _serviceProvider.GetRequiredService<ChangePasswordViewModel>();
        }

        public void Receive(UserLoggedOutMessage message)
        {
            IsLoggedIn = false;
            CurrentUser = null;
            IsAdmin = false;
            IsContentManager = false;
            CurrentUserAvatarPath = null;
            Navigate(typeof(LoginViewModel), "Вход");
        }

        public void Receive(UserAvatarChangedMessage message)
        {
            CurrentUserAvatarPath = message.Value;
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
        private void GoToDashboard() => Navigate(typeof(DashboardViewModel), "Дэшборд");

        [RelayCommand]
        private void GoToAdminPanel() => Navigate(typeof(AdminPanelViewModel), "Админ-панель");

        [RelayCommand]
        private void GoToContentManagerPanel() => Navigate(typeof(PlantManagementViewModel), "Менеджер-панель");

        [RelayCommand]
        private void GoToSettings() => Navigate(typeof(SettingsViewModel), "Настройки");
    }
}
