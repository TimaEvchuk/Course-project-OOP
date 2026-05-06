using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Plantify.Data;
using Plantify.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Plantify.ViewModels
{
    public partial class PlantManagementViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;

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


        public PlantManagementViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
                var sourcePath = dialog.FileName;
                var fileName = Guid.NewGuid() + Path.GetExtension(sourcePath);
                var destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Plants", fileName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(sourcePath, destinationPath, true);

                PlantImagePath = Path.Combine("/Images/Plants", fileName).Replace('\\', '/');
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
            await _unitOfWork.CompleteAsync();
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
            await _unitOfWork.CompleteAsync();
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
