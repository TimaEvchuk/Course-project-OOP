using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
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

namespace Plantify.ViewModels
{
    public partial class AddUserPlantViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;

        [ObservableProperty]
        private ObservableCollection<Plant> _allPlants = new();

        [ObservableProperty]
        private Plant? _selectedPlant;
        
        [ObservableProperty]
        private string? _customName;
        
        [ObservableProperty]
        private string? _location;

        [ObservableProperty]
        private UserPlant? _originalUserPlant;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string? _customImagePath;

        [ObservableProperty]
        private BitmapImage? _displayImageSource;

        public string Title => IsEditMode ? "Редактировать растение" : "Добавить растение в сад";

        public AddUserPlantViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;
        }

        public void Initialize(UserPlant? userPlant)
        {
            OriginalUserPlant = userPlant;
            IsEditMode = userPlant != null;

            if (IsEditMode && OriginalUserPlant != null)
            {
                CustomName = OriginalUserPlant.CustomName;
                Location = OriginalUserPlant.Location;
                CustomImagePath = OriginalUserPlant.CustomImagePath;
            }
            else
            {
                CustomName = "";
                Location = "";
                CustomImagePath = null;
                SelectedPlant = null;
            }
            // Load image initially
            DisplayImageSource = LoadImage(CustomImagePath ?? OriginalUserPlant?.Plant?.ImagePath);
        }

        partial void OnCustomImagePathChanged(string? value)
        {
            DisplayImageSource = LoadImage(value);
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
            if (SelectedPlant == null) { /*...*/ return; }
            if (_authenticationService.CurrentUser == null) { /*...*/ return; }

            if (IsEditMode && OriginalUserPlant != null)
            {
                OriginalUserPlant.PlantId = SelectedPlant.Id;
                OriginalUserPlant.CustomName = CustomName;
                OriginalUserPlant.Location = Location;
                OriginalUserPlant.CustomImagePath = CustomImagePath;
                
                _unitOfWork.UserPlants.Update(OriginalUserPlant);
            }
            else
            {
                var newUserPlant = new UserPlant
                {
                    PlantId = SelectedPlant.Id,
                    UserId = _authenticationService.CurrentUser.Id,
                    CustomName = CustomName,
                    Location = Location,
                    LastUserWateringDate = DateTime.Today,
                    CustomImagePath = CustomImagePath
                };
                await _unitOfWork.UserPlants.AddAsync(newUserPlant);
            }
            
            await _unitOfWork.CompleteAsync();

            _messenger.Send(new CloseOverlayMessage());
            _messenger.Send(new NavigateMessage(typeof(MyGardenViewModel)));
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }

        public void ProcessImageFile(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return;
            
            var fileName = Guid.NewGuid() + Path.GetExtension(sourcePath);
            var targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Plants");
            
            Directory.CreateDirectory(targetDirectory);
            
            var destinationPath = Path.Combine(targetDirectory, fileName);
            
            File.Copy(sourcePath, destinationPath, true);
            CustomImagePath = Path.Combine("Images/Plants", fileName).Replace('\\', '/');
        }

        [RelayCommand]
        private void SelectImage()
        {
            var dialog = new OpenFileDialog { Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg" };
            if (dialog.ShowDialog() == true)
            {
                ProcessImageFile(dialog.FileName);
            }
        }

        private BitmapImage? LoadImage(string? imagePath)
        {
            string? imageToLoad = null;
            if (!string.IsNullOrEmpty(imagePath))
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(basePath, imagePath);
                if (File.Exists(fullPath))
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
