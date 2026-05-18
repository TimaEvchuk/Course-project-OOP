using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Services;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
using Plantify.Models.DTOs;
using System.Collections.Generic;
using Plantify.Models;

namespace Plantify.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel, IRecipient<PremiumStatusChangedMessage>
    {
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IDialogService _dialogService;

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

        public SettingsViewModel(IMessenger messenger, AuthenticationService authenticationService, IUnitOfWork unitOfWork, IConfiguration configuration, IDialogService dialogService)
        {
            _messenger = messenger;
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _dialogService = dialogService;

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
            var detachedCurrentUser = _authenticationService.CurrentUser;
            if (detachedCurrentUser == null) return;

            if (IsPremium)
            {
                // Cancel subscription
                var userToUpdate = await _unitOfWork.Users.GetByIdAsync(detachedCurrentUser.Id);
                if (userToUpdate == null) return;

                userToUpdate.IsPremium = false;
                userToUpdate.PremiumStartDate = null;
                userToUpdate.PremiumEndDate = null;
                
                await _unitOfWork.CompleteAsync();

                _messenger.Send(new NewNotificationMessage(new Notification { Message = "Подписка успешно отменена.", Type = Models.Enums.NotificationType.ActionSuccess }));

                // Manually update the state of the service's CurrentUser to match
                detachedCurrentUser.IsPremium = false;
                detachedCurrentUser.PremiumStartDate = null;
                detachedCurrentUser.PremiumEndDate = null;

                // Refresh the entire view
                LoadUserData();
                _messenger.Send(new PremiumStatusChangedMessage(detachedCurrentUser));
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
            var filePath = _dialogService.ShowOpenFileDialog("Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*");

            if (!string.IsNullOrEmpty(filePath))
            {
                var detachedCurrentUser = _authenticationService.CurrentUser;
                if (detachedCurrentUser == null) return;

                // --- Create a unique file path ---
                var extension = Path.GetExtension(filePath);
                var fileName = $"avatar_{detachedCurrentUser.Id}_{DateTime.Now.Ticks}{extension}";
                
                var projectRoot = AppDomain.CurrentDomain.BaseDirectory;
                var avatarsDir = Path.GetFullPath(Path.Combine(projectRoot, "..\\..\\..\\Images\\Avatars"));
                
                Directory.CreateDirectory(avatarsDir);
                var destPath = Path.Combine(avatarsDir, fileName);

                File.Copy(filePath, destPath, true);
                
                // --- Update database using the correct pattern ---
                var userToUpdate = await _unitOfWork.Users.GetByIdAsync(detachedCurrentUser.Id);
                if (userToUpdate == null) return; // Should not happen if user is logged in

                userToUpdate.AvatarPath = destPath;
                await _unitOfWork.CompleteAsync();
                
                // --- Update UI and session state ---
                detachedCurrentUser.AvatarPath = destPath; // Update the user object in the auth service
                UserAvatarPath = destPath; // Update the property bound to the UI
                _messenger.Send(new UserAvatarChangedMessage(destPath));
            }
        }

        [RelayCommand]
        private async Task ExportGarden()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;

            var defaultFileName = $"plantify_garden_{currentUser.Login}_{DateTime.Now:yyyyMMdd}.json";
            var filePath = _dialogService.ShowSaveFileDialog("JSON files (*.json)|*.json", defaultFileName);

            if (string.IsNullOrEmpty(filePath)) return;

            var userPlants = await _unitOfWork.UserPlants.FindAsync(up => up.UserId == currentUser.Id);
            
            var gardenDto = userPlants.Select(up => new UserPlantDto
            {
                PlantId = up.PlantId,
                CustomName = up.CustomName,
                Location = up.Location,
                Description = up.Description,
                LastUserWateringDate = up.LastUserWateringDate,
                LastFertilizedDate = up.LastFertilizedDate
            }).ToList();

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(gardenDto, options);
                await File.WriteAllTextAsync(filePath, json);
                
                _messenger.Send(new NewNotificationMessage(new Notification { Message = "Сад успешно экспортирован!", Type = Models.Enums.NotificationType.ActionSuccess }));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _messenger.Send(new NewNotificationMessage(new Notification { Message = $"Ошибка экспорта: {ex.Message}", Type = Models.Enums.NotificationType.Warning }));
            }
        }

        [RelayCommand]
        private async Task ImportGarden()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;

            var filePath = _dialogService.ShowOpenFileDialog("JSON files (*.json)|*.json");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var gardenDto = JsonSerializer.Deserialize<List<UserPlantDto>>(json);

                if (gardenDto == null || !gardenDto.Any())
                {
                    _messenger.Send(new NewNotificationMessage(new Notification { Message = "Файл импорта пуст или некорректен.", Type = Models.Enums.NotificationType.Warning }));
                    return;
                }

                // Удаляем старые растения
                var oldUserPlants = await _unitOfWork.UserPlants.FindAsync(up => up.UserId == currentUser.Id);
                _unitOfWork.UserPlants.RemoveRange(oldUserPlants);

                // Добавляем новые
                var newUserPlants = gardenDto.Select(dto => new UserPlant
                {
                    UserId = currentUser.Id,
                    PlantId = dto.PlantId,
                    CustomName = dto.CustomName,
                    Location = dto.Location,
                    Description = dto.Description,
                    LastUserWateringDate = dto.LastUserWateringDate,
                    LastFertilizedDate = dto.LastFertilizedDate
                }).ToList();

                await _unitOfWork.UserPlants.AddRangeAsync(newUserPlants);
                await _unitOfWork.CompleteAsync();

                _messenger.Send(new GardenStateChangedMessage());
                _messenger.Send(new NewNotificationMessage(new Notification { Message = "Сад успешно импортирован!", Type = Models.Enums.NotificationType.ActionSuccess }));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _messenger.Send(new NewNotificationMessage(new Notification { Message = $"Ошибка импорта: {ex.Message}", Type = Models.Enums.NotificationType.Warning }));
            }
        }
    }
}

