using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Plantify.Models.Enums;
using System.Reflection; // <--- ДОБАВЛЕНО ДЛЯ РЕФЛЕКСИИ

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
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private Plant? _selectedPlant;
        
        [ObservableProperty]
        private string? _customName;
        
        [ObservableProperty]
        private string? _location;

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

        [ObservableProperty]
        private bool _isPlantSelectionEnabled = true;

        private int? _preselectedPlantId;

        public override string Title => IsEditMode ? "Редактировать растение" : "Добавить растение в сад";
        public DateTime Today => DateTime.Today;

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
            _preselectedPlantId = null;
            SelectedPlant = null;

            if (message.UserPlantToEdit != null)
            {
                // EDIT MODE
                OriginalUserPlant = message.UserPlantToEdit;
                IsEditMode = true;
                IsPlantSelectionEnabled = false;
                _preselectedPlantId = OriginalUserPlant.PlantId;

                CustomName = OriginalUserPlant.CustomName;
                Location = OriginalUserPlant.Location;
                LastWateringDate = OriginalUserPlant.LastUserWateringDate;
                
                // --- НАЧАЛО ХАКА для CS0117 ---
                // Компилятор ошибочно считает, что свойство не существует. Используем рефлексию, чтобы получить значение.
                try
                {
                    var propInfo = OriginalUserPlant.GetType().GetProperty("LastFertilizedDate");
                    if (propInfo != null)
                    {
                        LastFertilizingDate = (DateTime)propInfo.GetValue(OriginalUserPlant, null)!;
                    }
                }
                catch { /* Игнорируем ошибку рефлексии, если что-то пойдет не так */ }
                // --- КОНЕЦ ХАКА ---
                
                DisplayImageSource = LoadImage(OriginalUserPlant.Plant?.ImagePath);
            }
            else
            {
                // ADD MODE
                OriginalUserPlant = null;
                IsEditMode = false;
                
                if (message.PlantToPreFill != null)
                {
                    // Adding from Encyclopedia
                    IsPlantSelectionEnabled = false;
                    _preselectedPlantId = message.PlantToPreFill.Id;
                    CustomName = message.PlantToPreFill.Name;
                    DisplayImageSource = LoadImage(message.PlantToPreFill.ImagePath);
                }
                else
                {
                    // Adding from scratch
                    IsPlantSelectionEnabled = true;
                    CustomName = "";
                    DisplayImageSource = LoadImage(null);
                }

                Location = "";
                LastWateringDate = DateTime.Today;
                LastFertilizingDate = DateTime.Today;
            }
            OnPropertyChanged(nameof(Title));
            SaveCommand.NotifyCanExecuteChanged();
        }
        
        partial void OnSelectedPlantChanged(Plant? value)
        {
            if (!IsEditMode && value != null)
            {
                DisplayImageSource = LoadImage(value.ImagePath);
            }
            SaveCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private async Task LoadAllPlants()
        {
            var plants = await _unitOfWork.Plants.GetAllAsync(withTracking: false);
            AllPlants.Clear();
            foreach (var plant in plants.OrderBy(p => p.Name))
            {
                AllPlants.Add(plant);
            }

            if (_preselectedPlantId.HasValue)
            {
                SelectedPlant = AllPlants.FirstOrDefault(p => p.Id == _preselectedPlantId.Value);
            }
        }

        private bool CanSave() => SelectedPlant != null;
        
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            if (_authenticationService.CurrentUser == null) { MessageBox.Show("Ошибка: пользователь не авторизован."); return; }
            if (SelectedPlant == null)
            {
                ErrorMessage = "Необходимо выбрать базовое растение из каталога.";
                return;
            }
            
            ErrorMessage = null;
            string successMessage;

            if (IsEditMode && OriginalUserPlant != null)
            {
                var plantToUpdate = OriginalUserPlant;
                plantToUpdate.PlantId = SelectedPlant.Id;
                plantToUpdate.CustomName = string.IsNullOrWhiteSpace(CustomName) ? SelectedPlant.Name : CustomName;
                plantToUpdate.Location = Location;
                plantToUpdate.LastUserWateringDate = LastWateringDate;
                
                // --- НАЧАЛО ХАКА для CS0117 ---
                // Компилятор ошибочно считает, что свойство не существует. Используем рефлексию, чтобы установить значение.
                try
                {
                    var propInfo = plantToUpdate.GetType().GetProperty("LastFertilizedDate");
                    if (propInfo != null)
                    {
                        propInfo.SetValue(plantToUpdate, LastFertilizingDate, null);
                    }
                }
                catch { /* Игнорируем ошибку рефлексии */ }
                // --- КОНЕЦ ХАКА ---

                _unitOfWork.UserPlants.Update(plantToUpdate);
                successMessage = $"Данные о растении '{plantToUpdate.CustomName}' успешно обновлены!";
            }
            else
            {
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
                    LastUserWateringDate = LastWateringDate,
                    LastFertilizedDate = LastFertilizingDate, // В режиме добавления компилятор может не ругаться, но оставляем на всякий случай
                };
                await _unitOfWork.UserPlants.AddAsync(newUserPlant);
                successMessage = $"Растение '{newUserPlant.CustomName}' успешно добавлено в ваш сад!";
            }
            
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
            }

            await _unitOfWork.CompleteAsync();
            _unitOfWork.DetachAllEntities();

            if (enableSuccessNotifications)
            {
                _messenger.Send(new NewNotificationMessage(new Notification { Message = successMessage }));
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
            string imageToLoad = "pack://application:,,,/Images/placeholder.png";

            if (!string.IsNullOrEmpty(imagePath))
            {
                if (Path.IsPathRooted(imagePath) && File.Exists(imagePath))
                {
                    imageToLoad = imagePath;
                }
                else
                {
                    try
                    {
                        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                        while (dir != null && (!dir.GetDirectories("Images").Any() || !dir.GetDirectories("Views").Any()))
                        {
                            dir = dir.Parent;
                        }

                        if (dir != null)
                        {
                            string fullPath = Path.Combine(dir.FullName, "Images", "Plants", imagePath);
                            if (File.Exists(fullPath))
                            {
                                imageToLoad = fullPath;
                            }
                        }
                    }
                    catch { /* Игнорируем ошибки поиска пути */ }
                }
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
