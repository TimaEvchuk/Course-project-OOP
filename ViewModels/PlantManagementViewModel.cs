using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Plantify.ViewModels
{
    public partial class PlantManagementViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private ObservableCollection<Plant> _plants;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdatePlantCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeletePlantCommand))]
        private Plant? _selectedPlant;

        // Properties for adding/editing a plant
        [ObservableProperty]
        private string _plantName = "";
        [ObservableProperty]
        private int _plantWateringInterval;
        [ObservableProperty]
        private string _plantLightRequirement = "";
        [ObservableProperty]
        private string _plantVariety = "";
        [ObservableProperty]
        private string? _plantImagePath;
        [ObservableProperty]
        private BitmapImage? _displayImageSource;

        public ObservableCollection<string> Varieties { get; }
        public ObservableCollection<string> LightRequirements { get; }

        public PlantManagementViewModel(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _plants = new ObservableCollection<Plant>();
            Varieties = new ObservableCollection<string> { "Лиственные", "Суккуленты", "Лианы" };
            LightRequirements = new ObservableCollection<string> { "Тенелюбивые", "Светолюбивые", "Теневыносливые" };
            LoadPlantsCommand.Execute(null);
        }

        partial void OnSelectedPlantChanged(Plant? value)
        {
            if (value != null)
            {
                PlantName = value.Name;
                PlantWateringInterval = value.WateringInterval;
                PlantLightRequirement = value.LightRequirement;
                PlantVariety = value.Variety;
                PlantImagePath = value.ImagePath;
            }
            else
            {
                PlantName = "";
                PlantWateringInterval = 0;
                PlantLightRequirement = LightRequirements.FirstOrDefault() ?? "";
                PlantVariety = Varieties.FirstOrDefault() ?? "";
                PlantImagePath = null;
            }
            DisplayImageSource = LoadImage(PlantImagePath);
        }
        
        partial void OnPlantImagePathChanged(string? value)
        {
            DisplayImageSource = LoadImage(value);
        }

        [RelayCommand]
        private async Task LoadPlants()
        {
            var plantList = await _unitOfWork.Plants.GetAllWithSectionsAsync();
            Plants.Clear();
            foreach (var plant in plantList)
            {
                Plants.Add(plant);
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image from {imageToLoad}: {ex.Message}");
                return null;
            }
        }

        public void ProcessImageFile(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                MessageBox.Show("Invalid image file selected or file does not exist.");
                return;
            }
            var fileName = Guid.NewGuid() + Path.GetExtension(sourcePath);
            var targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Plants");
            try
            {
                Directory.CreateDirectory(targetDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating directory {targetDirectory}: {ex.Message}");
                return;
            }
            var destinationPath = Path.Combine(targetDirectory, fileName);
            try
            {
                File.Copy(sourcePath, destinationPath, true);
                PlantImagePath = Path.Combine("Images/Plants", fileName).Replace('\\', '/');
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying image from {sourcePath} to {destinationPath}: {ex.Message}");
            }
        }

        [RelayCommand]
        private void SelectImage()
        {
            var dialog = new OpenFileDialog { Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true)
            {
                ProcessImageFile(dialog.FileName);
            }
        }

        [RelayCommand]
        private async Task AddPlant()
        {
            var newPlant = new Plant
            {
                Name = PlantName,
                WateringInterval = PlantWateringInterval,
                LightRequirement = PlantLightRequirement,
                Variety = PlantVariety,
                ImagePath = PlantImagePath
            };
            await _unitOfWork.Plants.AddAsync(newPlant);
            try
            {
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding plant: {ex.Message}");
            }
            await LoadPlants();
        }

        private bool CanUpdateOrDelete() => SelectedPlant != null;

        [RelayCommand(CanExecute = nameof(CanUpdateOrDelete))]
        private async Task UpdatePlant()
        {
            if (SelectedPlant == null) return;
            SelectedPlant.Name = PlantName;
            SelectedPlant.WateringInterval = PlantWateringInterval;
            SelectedPlant.LightRequirement = PlantLightRequirement;
            SelectedPlant.Variety = PlantVariety;
            SelectedPlant.ImagePath = PlantImagePath;
            _unitOfWork.Plants.Update(SelectedPlant);
            try
            {
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating plant: {ex.Message}");
            }
            await LoadPlants();
        }

        [RelayCommand(CanExecute = nameof(CanUpdateOrDelete))]
        private async Task DeletePlant()
        {
            if (SelectedPlant == null) return;
            _unitOfWork.Plants.Delete(SelectedPlant);
            await _unitOfWork.CompleteAsync();
            await LoadPlants();
        }
    }
}
