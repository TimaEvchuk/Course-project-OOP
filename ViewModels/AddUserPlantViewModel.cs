using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Plantify.Models.Enums;

namespace Plantify.ViewModels
{
    public partial class AddUserPlantViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;
        private readonly IConfiguration _configuration;

        [ObservableProperty]
        private ObservableCollection<Plant> _allPlants = new();

        [ObservableProperty]
        private Plant? _selectedPlant;
        
        [ObservableProperty]
        private string? _customName;
        
        [ObservableProperty]
        private string? _location;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private DateTime _lastWateringDate;

        [ObservableProperty]
        private DateTime _lastFertilizingDate;

        [ObservableProperty]
        private UserPlant? _originalUserPlant;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private BitmapImage? _displayImageSource;

        [ObservableProperty]
        private string? _errorMessage;

        public string Title => IsEditMode ? "Редактировать растение" : "Добавить растение в сад";

        public AddUserPlantViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;
            _configuration = configuration;
        }

        public void Initialize(ShowAddUserPlantOverlayMessage message)
        {
            ErrorMessage = null;
            if (message.UserPlantToEdit != null)
            {
                // EDIT MODE
                OriginalUserPlant = message.UserPlantToEdit;
                IsEditMode = true;

                CustomName = OriginalUserPlant.CustomName;
                Location = OriginalUserPlant.Location;
                Description = OriginalUserPlant.Description;
                LastWateringDate = OriginalUserPlant.LastUserWateringDate;
                LastFertilizingDate = OriginalUserPlant.LastFertilizedDate;
                
                DisplayImageSource = LoadImage(OriginalUserPlant.Plant?.ImagePath);
            }
            else
            {
                // ADD MODE
                OriginalUserPlant = null;
                IsEditMode = false;
                
                CustomName = message.PlantToPreFill?.Name ?? "";
                Location = "";
                Description = "";
                SelectedPlant = message.PlantToPreFill;
                LastWateringDate = DateTime.Today;
                LastFertilizingDate = DateTime.Today;
                
                DisplayImageSource = LoadImage(message.PlantToPreFill?.ImagePath);
            }
        }
        
        partial void OnSelectedPlantChanged(Plant? value)
        {
            if (!IsEditMode && value != null)
            {
                DisplayImageSource = LoadImage(value.ImagePath);
            }
        }

        [RelayCommand]
        private async Task LoadAllPlants()
        {
            var plants = await _unitOfWork.Plants.GetAllAsync();
            AllPlants.Clear();
            foreach (var plant in plants)
            {
                AllPlants.Add(plant);
            }

            if (IsEditMode && OriginalUserPlant != null)
            {
                SelectedPlant = AllPlants.FirstOrDefault(p => p.Id == OriginalUserPlant.PlantId);
            }
        }
        
        [RelayCommand]
        private async Task Save()
        {
            if (SelectedPlant == null) { MessageBox.Show("Пожалуйста, выберите растение."); return; }
            if (_authenticationService.CurrentUser == null) { MessageBox.Show("Ошибка: пользователь не авторизован."); return; }
            
            ErrorMessage = null;
            string successMessage;

            if (IsEditMode && OriginalUserPlant != null)
            {
                var plantToUpdate = await _unitOfWork.UserPlants.GetByIdAsync(OriginalUserPlant.Id);
                if (plantToUpdate == null)
                {
                    ErrorMessage = "Не удалось найти редактируемое растение в базе данных.";
                    return;
                }

                plantToUpdate.PlantId = SelectedPlant.Id;
                plantToUpdate.CustomName = string.IsNullOrWhiteSpace(CustomName) ? SelectedPlant.Name : CustomName;
                plantToUpdate.Location = Location;
                plantToUpdate.Description = Description;
                plantToUpdate.LastUserWateringDate = LastWateringDate;
                plantToUpdate.LastFertilizedDate = LastFertilizingDate;

                successMessage = $"Данные о растении '{plantToUpdate.CustomName}' успешно обновлены!";
            }
            else
            {
                // Plant limit check for non-premium users
                if (!_authenticationService.IsPremiumActive())
                {
                    var plantCount = await _unitOfWork.UserPlants.CountAsync(p => p.UserId == _authenticationService.CurrentUser.Id);
                    if (plantCount >= 5)
                    {
                        ErrorMessage = "Достигнут лимит в 5 растений. Оформите премиум-подписку для снятия ограничений.";
                        return;
                    }
                }

                var newUserPlant = new UserPlant
                {
                    PlantId = SelectedPlant.Id,
                    UserId = _authenticationService.CurrentUser.Id,
                    CustomName = string.IsNullOrWhiteSpace(CustomName) ? SelectedPlant.Name : CustomName,
                    Location = Location,
                    Description = Description,
                    LastUserWateringDate = LastWateringDate,
                    LastFertilizedDate = LastFertilizingDate,
                };
                await _unitOfWork.UserPlants.AddAsync(newUserPlant);
                successMessage = $"Растение '{newUserPlant.CustomName}' успешно добавлено в ваш сад!";
            }
            
            await _unitOfWork.CompleteAsync();

            var enableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
            if (enableSuccessNotifications)
            {
                var notification = new Notification
                {
                    Message = successMessage,
                    Timestamp = DateTime.Now,
                    Type = NotificationType.ActionSuccess,
                    UserId = _authenticationService.CurrentUser.Id,
                    IsDismissed = false
                };
                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.CompleteAsync();
                _messenger.Send(new NewNotificationMessage(notification));
            }

            _messenger.Send(new CloseOverlayMessage());
            _messenger.Send(new GardenStateChangedMessage());
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }

        private BitmapImage? LoadImage(string? imagePath)
        {
            string? imageToLoad = null;
            if (!string.IsNullOrEmpty(imagePath))
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = System.IO.Path.Combine(basePath, imagePath);
                if (System.IO.File.Exists(fullPath))
                {
                    imageToLoad = fullPath;
                }
            }
            
            if (imageToLoad == null)
            {
                imageToLoad = "pack://application:,,,/Images/placeholder.png";
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageToLoad, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }
    }
}
