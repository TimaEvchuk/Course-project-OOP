using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using System;
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
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string? _plantName;
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private Variety? _selectedVariety;
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private LightRequirement? _selectedLightRequirement;

        [ObservableProperty]
        private int _wateringInterval;
        [ObservableProperty]
        private int _fertilizingInterval;
        [ObservableProperty]
        private string? _imagePath;
        
        [ObservableProperty]
        private string? _errorMessage;
        
        public ObservableCollection<PlantSectionViewModel> Sections { get; set; }

        public string Title => IsEditMode ? "Редактировать растение" : "Добавить новое растение";
        public string SaveButtonText => IsEditMode ? "Сохранить" : "Добавить";
        
        public ObservableCollection<Variety> Varieties { get; }
        public ObservableCollection<LightRequirement> LightRequirements { get; }

        public AddEditPlantViewModel(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            
            Varieties = new ObservableCollection<Variety>();
            LightRequirements = new ObservableCollection<LightRequirement>();
            Sections = new ObservableCollection<PlantSectionViewModel>();
        }

        [RelayCommand]
        private async Task LoadCategories()
        {
            var varieties = await _unitOfWork.Varieties.GetAllAsync();
            Varieties.Clear();
            foreach (var variety in varieties.OrderBy(v => v.Name))
            {
                Varieties.Add(variety);
            }

            var lightRequirements = await _unitOfWork.LightRequirements.GetAllAsync();
            LightRequirements.Clear();
            foreach (var light in lightRequirements.OrderBy(l => l.Name))
            {
                LightRequirements.Add(light);
            }
        }

        public async Task InitializeAsync(Plant? plant = null)
        {
            await LoadCategoriesCommand.ExecuteAsync(null);
            
            Sections.Clear();
            
            if (plant != null)
            {
                IsEditMode = true;
                _plantToEdit = plant;

                PlantName = plant.Name;
                SelectedVariety = Varieties.FirstOrDefault(v => v.Id == plant.VarietyId);
                SelectedLightRequirement = LightRequirements.FirstOrDefault(l => l.Id == plant.LightRequirementId);
                WateringInterval = plant.WateringInterval;
                FertilizingInterval = plant.FertilizingInterval;
                ImagePath = plant.ImagePath;

                if (plant.Sections.Any())
                {
                    foreach (var section in plant.Sections)
                    {
                        Sections.Add(new PlantSectionViewModel { Title = section.Title, Content = section.Content });
                    }
                }
                else
                {
                    Sections.Add(new PlantSectionViewModel { Title = "Описание", Content = "" });
                }
            }
            else
            {
                IsEditMode = false;
                _plantToEdit = null;
                
                PlantName = "";
                SelectedVariety = null;
                SelectedLightRequirement = null;
                WateringInterval = 7;
                FertilizingInterval = 30;
                ImagePath = null;
                Sections.Add(new PlantSectionViewModel { Title = "Описание", Content = "" });
            }
            // Manually trigger re-evaluation after initialization
            SaveCommand.NotifyCanExecuteChanged();
        }
        
        [RelayCommand]
        private void AddSection()
        {
            Sections.Add(new PlantSectionViewModel { Title = "", Content = "" });
        }

        [RelayCommand]
        private void RemoveSection(PlantSectionViewModel section)
        {
            if (section != null)
            {
                Sections.Remove(section);
            }
        }

        private bool CanSave()
        {
            var canSaveResult = !string.IsNullOrWhiteSpace(PlantName) &&
                                PlantName.Length >= 2 &&
                                SelectedVariety != null &&
                                SelectedLightRequirement != null;
            
            MessageBox.Show($"CanSave evaluated: {canSaveResult}\n" +
                            $"PlantName: '{PlantName}' ({(PlantName?.Length ?? 0)} chars)\n" +
                            $"SelectedVariety: {SelectedVariety?.Name ?? "null"}\n" +
                            $"SelectedLightRequirement: {SelectedLightRequirement?.Name ?? "null"}");

            return canSaveResult;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            ErrorMessage = null;
            
            ProcessImageFile(ImagePath);

            Plant plantToSave;

            if (IsEditMode && _plantToEdit != null)
            {
                plantToSave = _plantToEdit;
            }
            else
            {
                plantToSave = new Plant();
                await _unitOfWork.Plants.AddAsync(plantToSave);
            }

            plantToSave.Name = PlantName;
            plantToSave.VarietyId = SelectedVariety.Id;
            plantToSave.LightRequirementId = SelectedLightRequirement.Id;
            plantToSave.WateringInterval = WateringInterval;
            plantToSave.FertilizingInterval = FertilizingInterval;
            plantToSave.ImagePath = ImagePath;

            plantToSave.Sections.Clear();
            foreach (var sectionVm in Sections)
            {
                if (!string.IsNullOrWhiteSpace(sectionVm.Title) || !string.IsNullOrWhiteSpace(sectionVm.Content))
                {
                    plantToSave.Sections.Add(new PlantSection
                    {
                        Title = sectionVm.Title,
                        Content = sectionVm.Content
                    });
                }
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
                return;
            }
            
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
