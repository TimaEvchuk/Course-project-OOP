using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Services;
using Microsoft.Win32;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Plantify.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel, IRecipient<PremiumStatusChangedMessage>
    {
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        [ObservableProperty]
        private string? _userLogin;

        [ObservableProperty]
        private string? _userEmail;

        [ObservableProperty]
        private string? _userAvatarPath;

        [ObservableProperty]
        private string? _userRole;

        [ObservableProperty]
        private string? _subscriptionStatusText;

        [ObservableProperty]
        private string? _subscriptionButtonText;

        [ObservableProperty]
        private bool _isPremium;

        [ObservableProperty]
        private DateTime? _premiumStartDate;

        [ObservableProperty]
        private DateTime? _premiumEndDate;

        [ObservableProperty]
        private bool _enableCareNotifications;

        [ObservableProperty]
        private bool _enableSuccessNotifications;

        public SettingsViewModel(IMessenger messenger, AuthenticationService authenticationService, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _messenger = messenger;
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;

            _messenger.Register(this);

            LoadUserData();
            LoadNotificationSettings();
        }

        public void Receive(PremiumStatusChangedMessage message)
        {
            LoadUserData();
        }

        private void LoadUserData()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                UserLogin = currentUser.Login;
                UserEmail = currentUser.Email;
                UserAvatarPath = currentUser.AvatarPath;
                UserRole = string.Join(", ", currentUser.Roles.Select(r => r.Name));
                
                IsPremium = _authenticationService.IsPremiumActive();
                if (IsPremium)
                {
                    SubscriptionStatusText = "Премиум-тариф активен";
                    SubscriptionButtonText = "Отменить подписку";
                    PremiumStartDate = currentUser.PremiumStartDate;
                    PremiumEndDate = currentUser.PremiumEndDate;
                }
                else
                {
                    SubscriptionStatusText = "У вас стандартный тариф.";
                    SubscriptionButtonText = "Получить премиум";
                    PremiumStartDate = null;
                    PremiumEndDate = null;
                }
            }
        }
        
        private void LoadNotificationSettings()
        {
            EnableCareNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableCareNotifications");
            EnableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
        }
        
        partial void OnEnableCareNotificationsChanged(bool value)
        {
            UpdateSetting("NotificationSettings:EnableCareNotifications", value);
        }

        partial void OnEnableSuccessNotificationsChanged(bool value)
        {
            UpdateSetting("NotificationSettings:EnableSuccessNotifications", value);
        }

        private void UpdateSetting<T>(string key, T value)
        {
            try
            {
                var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var json = File.ReadAllText(appSettingsPath);
                var jsonNode = JsonNode.Parse(json);

                if (jsonNode == null) return;

                var keys = key.Split(':');
                if (keys.Length == 2)
                {
                    var sectionKey = keys[0];
                    var propertyKey = keys[1];

                    if (jsonNode[sectionKey] == null)
                    {
                        jsonNode[sectionKey] = new JsonObject();
                    }
                    
                    if (value != null)
                    {
                        jsonNode[sectionKey]![propertyKey] = JsonValue.Create(value);

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        File.WriteAllText(appSettingsPath, jsonNode.ToJsonString(options));
                    }
                }
            }
            catch (Exception ex)
            {
                // In a real app, you'd want to log this error.
                Console.WriteLine($"Error updating appsettings.json: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SubscriptionAction()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;

            if (IsPremium)
            {
                // Cancel subscription
                currentUser.IsPremium = false;
                currentUser.PremiumStartDate = null;
                currentUser.PremiumEndDate = null;
                await _unitOfWork.Users.UpdateAsync(currentUser);
                await _unitOfWork.CompleteAsync();

                // Refresh the entire view
                LoadUserData();
                _messenger.Send(new PremiumStatusChangedMessage(currentUser));
            }
            else
            {
                _messenger.Send(new ShowPremiumPurchaseOverlayMessage());
            }
        }
        
        [RelayCommand]
        private void ShowPremiumPurchaseOverlay()
        {
            _messenger.Send(new ShowPremiumPurchaseOverlayMessage());
        }


        [RelayCommand]
        private void Logout()
        {
            _authenticationService.Logout();
            _messenger.Send(new UserLoggedOutMessage());
        }

        [RelayCommand]
        private void ShowChangePasswordOverlay()
        {
            _messenger.Send(new ShowChangePasswordOverlayMessage());
        }

        [RelayCommand]
        private async Task ChangeAvatar()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var sourcePath = openFileDialog.FileName;
                var currentUser = _authenticationService.CurrentUser;
                if (currentUser == null) return;

                var extension = Path.GetExtension(sourcePath);
                var fileName = $"avatar_{currentUser.Id}_{DateTime.Now.Ticks}{extension}";

                // Assuming the solution is run from the project's root in debug.
                // A more robust solution might need a better way to find the project root.
                var projectRoot = AppDomain.CurrentDomain.BaseDirectory;
                var avatarsDir = Path.GetFullPath(Path.Combine(projectRoot, "..\\..\\..\\Images\\Avatars"));
                
                Directory.CreateDirectory(avatarsDir);
                var destPath = Path.Combine(avatarsDir, fileName);

                File.Copy(sourcePath, destPath, true);

                currentUser.AvatarPath = destPath; // Save the absolute path
                await _unitOfWork.Users.UpdateAsync(currentUser);
                await _unitOfWork.CompleteAsync();

                // Update the UI
                UserAvatarPath = currentUser.AvatarPath;
                _messenger.Send(new UserAvatarChangedMessage(UserAvatarPath));
            }
        }
    }
}
