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
using System.Windows.Media.Imaging;

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
        private BitmapImage? _displayImageSource;
        
        [ObservableProperty]
        private string? _errorMessage;

        public ObservableCollection<PlantSectionViewModel> Sections { get; set; }

        public override string Title => IsEditMode ? "Редактировать растение" : "Добавить новое растение";
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

        partial void OnImagePathChanged(string? value)
        {
            DisplayImageSource = LoadImage(value);
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
            }
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
            return !string.IsNullOrWhiteSpace(PlantName) &&
                   PlantName.Length >= 2 &&
                   SelectedVariety != null &&
                   SelectedLightRequirement != null;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            ErrorMessage = null;
            if (PlantName == null || SelectedVariety == null || SelectedLightRequirement == null)
            {
                ErrorMessage = "Все поля со звездочкой должны быть заполнены.";
                return;
            }

            if (IsEditMode && _plantToEdit != null)
            {
                var plantToUpdate = await _unitOfWork.Plants.GetByIdAsync(_plantToEdit.Id);
                if (plantToUpdate == null)
                {
                    ErrorMessage = "Не удалось найти редактируемое растение в базе данных.";
                    return;
                }
                
                plantToUpdate.Name = PlantName;
                plantToUpdate.VarietyId = SelectedVariety.Id;
                plantToUpdate.LightRequirementId = SelectedLightRequirement.Id;
                plantToUpdate.WateringInterval = WateringInterval;
                plantToUpdate.FertilizingInterval = FertilizingInterval;
                plantToUpdate.ImagePath = ImagePath;
                
                plantToUpdate.Sections.Clear();
                foreach (var sectionVm in Sections)
                {
                    if (!string.IsNullOrWhiteSpace(sectionVm.Title) || !string.IsNullOrWhiteSpace(sectionVm.Content))
                    {
                        plantToUpdate.Sections.Add(new PlantSection
                        {
                            Title = sectionVm.Title,
                            Content = sectionVm.Content
                        });
                    }
                }
            }
            else
            {
                var newPlant = new Plant
                {
                    Name = PlantName,
                    VarietyId = SelectedVariety.Id,
                    LightRequirementId = SelectedLightRequirement.Id,
                    WateringInterval = WateringInterval,
                    FertilizingInterval = FertilizingInterval,
                    ImagePath = ImagePath
                };
                
                foreach (var sectionVm in Sections)
                {
                    if (!string.IsNullOrWhiteSpace(sectionVm.Title) || !string.IsNullOrWhiteSpace(sectionVm.Content))
                    {
                        newPlant.Sections.Add(new PlantSection
                        {
                            Title = sectionVm.Title,
                            Content = sectionVm.Content
                        });
                    }
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
                ImagePath = Path.GetFileName(dialog.FileName);
            }
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
            catch 
            {
                return null; 
            }
        }
    }
}
