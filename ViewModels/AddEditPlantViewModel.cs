using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Plantify.ViewModels
{
    public partial class AddEditPlantViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private bool _isEditMode;

        private Plant? _plantToEdit;

        [ObservableProperty]
        private string? _plantName;
        [ObservableProperty]
        private string? _variety;
        [ObservableProperty]
        private string? _lightRequirement;
        [ObservableProperty]
        private int _wateringInterval;
        [ObservableProperty]
        private int _fertilizingInterval;
        [ObservableProperty]
        private string? _description;
        [ObservableProperty]
        private string? _imagePath;
        
        [ObservableProperty]
        private string? _errorMessage;

        public string Title => IsEditMode ? "Редактировать растение" : "Добавить новое растение";
        public string SaveButtonText => IsEditMode ? "Сохранить" : "Добавить";
        
        public ObservableCollection<string> Varieties { get; }
        public ObservableCollection<string> LightRequirements { get; }

        public AddEditPlantViewModel(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            
            Varieties = new ObservableCollection<string>();
            LightRequirements = new ObservableCollection<string>();
        }

        [RelayCommand]
        private async Task LoadDistinctProperties()
        {
            var plants = await _unitOfWork.Plants.GetAllAsync();
            
            var distinctVarieties = plants.Select(p => p.Variety).Distinct().OrderBy(v => v);
            Varieties.Clear();
            foreach (var variety in distinctVarieties)
            {
                Varieties.Add(variety);
            }

            var distinctLight = plants.Select(p => p.LightRequirement).Distinct().OrderBy(l => l);
            LightRequirements.Clear();
            foreach (var light in distinctLight)
            {
                LightRequirements.Add(light);
            }
        }

        public void Initialize(Plant? plant = null)
        {
            if (plant != null)
            {
                IsEditMode = true;
                _plantToEdit = plant;

                PlantName = plant.Name;
                Variety = plant.Variety;
                LightRequirement = plant.LightRequirement;
                WateringInterval = plant.WateringInterval;
                FertilizingInterval = plant.FertilizingInterval;
                ImagePath = plant.ImagePath;
                Description = plant.Sections.FirstOrDefault(s => s.Title == "Описание")?.Content ?? "";
            }
            else
            {
                IsEditMode = false;
                _plantToEdit = null;
                
                PlantName = "";
                Variety = "";
                LightRequirement = "";
                WateringInterval = 0;
                FertilizingInterval = 0;
                ImagePath = null;
                Description = "";
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(PlantName) || string.IsNullOrWhiteSpace(Variety) || string.IsNullOrWhiteSpace(LightRequirement))
            {
                ErrorMessage = "Пожалуйста, заполните все обязательные поля.";
                return;
            }

            ErrorMessage = null;
            
            ProcessImageFile(ImagePath);

            if (IsEditMode && _plantToEdit != null)
            {
                // Update existing plant
                _plantToEdit.Name = PlantName;
                _plantToEdit.Variety = Variety;
                _plantToEdit.LightRequirement = LightRequirement;
                _plantToEdit.WateringInterval = WateringInterval;
                _plantToEdit.FertilizingInterval = FertilizingInterval;
                _plantToEdit.ImagePath = ImagePath;

                var descriptionSection = _plantToEdit.Sections.FirstOrDefault(s => s.Title == "Описание");
                if (descriptionSection != null)
                {
                    descriptionSection.Content = Description ?? "";
                }
                else if (!string.IsNullOrWhiteSpace(Description))
                {
                    _plantToEdit.Sections.Add(new PlantSection { Title = "Описание", Content = Description });
                }
                
                _unitOfWork.Plants.Update(_plantToEdit);
            }
            else
            {
                // Add new plant
                var newPlant = new Plant
                {
                    Name = PlantName,
                    Variety = Variety,
                    LightRequirement = LightRequirement,
                    WateringInterval = WateringInterval,
                    FertilizingInterval = FertilizingInterval,
                    ImagePath = ImagePath,
                };
                if (!string.IsNullOrWhiteSpace(Description))
                {
                    newPlant.Sections.Add(new PlantSection { Title = "Описание", Content = Description });
                }
                
                await _unitOfWork.Plants.AddAsync(newPlant);
            }

            await _unitOfWork.CompleteAsync();

            _messenger.Send(new AdminUserListChangedMessage());
            _messenger.Send(new CloseOverlayMessage());
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }
        
        [RelayCommand]
        private void SelectImage()
        {
            var dialog = new OpenFileDialog { Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true)
            {
                ImagePath = dialog.FileName;
            }
        }

        private void ProcessImageFile(string? sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                // No image to process, or path is already a project-relative path
                return;
            }
            
            // Avoid re-processing if it's already a relative path
            if (sourcePath.StartsWith("Images/"))
            {
                return;
            }

            var fileName = Guid.NewGuid() + Path.GetExtension(sourcePath);
            var targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Plants");
            
            try
            {
                Directory.CreateDirectory(targetDirectory);
                var destinationPath = Path.Combine(targetDirectory, fileName);
                File.Copy(sourcePath, destinationPath, true);
                ImagePath = Path.Combine("Images/Plants", fileName).Replace('\\', '/');
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying image from {sourcePath}: {ex.Message}");
            }
        }
    }
}
