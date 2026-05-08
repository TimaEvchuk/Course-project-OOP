using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging; // Added for IMessenger
using Microsoft.Win32;
using Plantify.Data;
using Plantify.Messages; // Added for NavigateMessage
using Plantify.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging; // Added for BitmapImage

namespace Plantify.ViewModels
{
    public partial class PlantManagementViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger; // Injected messenger

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
        private string _plantDescription = "";
        [ObservableProperty]
        private int _plantWateringInterval;
        [ObservableProperty]
        private string _plantLightRequirement = "";
        [ObservableProperty]
        private string _plantDifficulty = "";
        [ObservableProperty]
        private string? _plantImagePath;

        [ObservableProperty]
        private BitmapImage? _displayImageSource;


        public PlantManagementViewModel(IUnitOfWork unitOfWork, IMessenger messenger) // Messenger injected
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger; // Assign messenger
            _plants = new ObservableCollection<Plant>();
            LoadPlantsCommand.Execute(null);
        }

        partial void OnSelectedPlantChanged(Plant? value)
        {
            if (value != null)
            {
                PlantName = value.Name;
                PlantDescription = value.Description;
                PlantWateringInterval = value.WateringInterval;
                PlantLightRequirement = value.LightRequirement;
                PlantDifficulty = value.Difficulty;
                PlantImagePath = value.ImagePath;
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
            var plantList = await _unitOfWork.Plants.GetAllAsync();
            Plants.Clear();
            foreach (var plant in plantList)
            {
                Plants.Add(plant);
            }
        }

        // Helper method to load image for display
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

            // If no specific image is found, use the placeholder
            if (imageToLoad == null)
            {
                // Use Pack URI to load the embedded resource
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
                DisplayImageSource = LoadImage(PlantImagePath); // Update display image
                // Temporarily keep the MessageBox for debugging, will remove later
                MessageBox.Show($"Image selected and copied.\nSource: {sourcePath}\nDestination: {destinationPath}\nStored Path: {PlantImagePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying image from {sourcePath} to {destinationPath}: {ex.Message}");
            }
        }

        [RelayCommand]
        private void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

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
                Description = PlantDescription,
                WateringInterval = PlantWateringInterval,
                LightRequirement = PlantLightRequirement,
                Difficulty = PlantDifficulty,
                ImagePath = PlantImagePath
            };
            await _unitOfWork.Plants.AddAsync(newPlant);
            try
            {
                var changes = await _unitOfWork.CompleteAsync();
                if (changes > 0)
                {
                    MessageBox.Show("Plant added successfully!");
                }
                else
                {
                    MessageBox.Show("Plant added, but no changes were saved to the database.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding plant: {ex.Message}");
            }
            await LoadPlants(); // Refresh list
        }

        private bool CanUpdateOrDelete() => SelectedPlant != null;

        [RelayCommand(CanExecute = nameof(CanUpdateOrDelete))]
        private async Task UpdatePlant()
        {
            if (SelectedPlant == null) return;

            SelectedPlant.Name = PlantName;
            SelectedPlant.Description = PlantDescription;
            SelectedPlant.WateringInterval = PlantWateringInterval;
            SelectedPlant.LightRequirement = PlantLightRequirement;
            SelectedPlant.Difficulty = PlantDifficulty;
            SelectedPlant.ImagePath = PlantImagePath;

            _unitOfWork.Plants.Update(SelectedPlant);
            try
            {
                var changes = await _unitOfWork.CompleteAsync();
                if (changes > 0)
                {
                    MessageBox.Show("Plant updated successfully!");
                }
                else
                {
                    MessageBox.Show("Plant updated, but no changes were saved to the database.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating plant: {ex.Message}");
            }
            await LoadPlants(); // Refresh list
        }

        [RelayCommand(CanExecute = nameof(CanUpdateOrDelete))]
        private async Task DeletePlant()
        {
            if (SelectedPlant == null) return;

            _unitOfWork.Plants.Remove(SelectedPlant);
            await _unitOfWork.CompleteAsync();
            await LoadPlants(); // Refresh list
        }
    }
}
