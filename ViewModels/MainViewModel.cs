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
using System.Windows.Media;
using System.Windows;

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

        [ObservableProperty]
        private Brush _currentBackground;
        
        [ObservableProperty]
        private GridLength _headerHeight;

        private readonly IServiceProvider _serviceProvider;
        private readonly IMessenger _messenger;
        
        private readonly Brush _defaultBackground = new SolidColorBrush((Color)Application.Current.FindResource("ColorMilkWhite"));
        private readonly Brush _authBackground;

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

            // Define brushes after resources are loaded
            var stops = new GradientStopCollection
            {
                new GradientStop { Color = (Color)Application.Current.FindResource("ColorDarkGreen"), Offset = 0.5 },
                new GradientStop { Color = (Color)Application.Current.FindResource("ColorForestGreen"), Offset = 0.5 }
            };
            _authBackground = new LinearGradientBrush(stops, new Point(0, 1), new Point(1, 0));
            _authBackground.Freeze(); // Freeze for performance
            
            _currentBackground = _defaultBackground;
            _headerHeight = new GridLength(0); // Start with no header

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
            if (viewModelType == typeof(LoginViewModel) || viewModelType == typeof(RegisterViewModel))
            {
                CurrentBackground = _authBackground;
                HeaderHeight = new GridLength(0);
            }
            else
            {
                CurrentBackground = _defaultBackground;
                HeaderHeight = new GridLength(80);
            }
            
            CurrentViewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);

            if (CurrentViewModel is ITitledViewModel titledViewModel && !string.IsNullOrEmpty(titledViewModel.Title))
            {
                CurrentPageTitle = titledViewModel.Title;
            }
            else
            {
                CurrentPageTitle = pageTitle;
            }
        }

        public void Receive(NavigateMessage message)
        {
            if (message.Value == typeof(EncyclopediaViewModel))
            {
                Navigate(typeof(EncyclopediaViewModel), "Справочник");
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

        public void Receive(NewNotificationMessage message)
        {
            // The notification is already created and saved by the sender.
            // This receiver's only job is to pass it to the UI.
            NotificationViewModel.AddNewNotification(message.Notification);
            HasNewNotifications = true;
        }

        public async void Receive(UserLoggedInMessage message)
        {
            CurrentUser = message.User;
            IsLoggedIn = true;
            IsAdmin = CurrentUser.Roles.Any(r => r.Name == "Администратор");
            IsContentManager = CurrentUser.Roles.Any(r => r.Name == "Контент-менеджер");
            CurrentUserAvatarPath = CurrentUser.AvatarPath;
            CurrentBackground = _defaultBackground; // Set default background on login
            
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
        private void GoToEncyclopedia() => Navigate(typeof(EncyclopediaViewModel), "Справочник");

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
