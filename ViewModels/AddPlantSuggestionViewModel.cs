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
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Plantify.Models.Enums;

namespace Plantify.ViewModels
{
    public partial class AddPlantSuggestionViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;
        private readonly IConfiguration _configuration;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string? _plantName;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private LightRequirement? _selectedLightRequirement;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private Variety? _selectedVariety;

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

        public ObservableCollection<Variety> Varieties { get; }
        public ObservableCollection<LightRequirement> LightRequirements { get; }

        public override string Title => "Предложить новое растение";
        public AddPlantSuggestionViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;
            _configuration = configuration;

            WateringInterval = 7;
            FertilizingInterval = 30;
            Varieties = new ObservableCollection<Variety>();
            LightRequirements = new ObservableCollection<LightRequirement>();
            Sections = new ObservableCollection<PlantSectionViewModel>
            {
                new PlantSectionViewModel { Title = "Описание", Content = "" }
            };
            DisplayImageSource = LoadImage(null);
            _ = LoadCategoriesCommand.ExecuteAsync(null);
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
                   SelectedLightRequirement != null &&
                   _authenticationService.CurrentUser != null;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            ErrorMessage = null;
            
            var sectionsAsJson = JsonSerializer.Serialize(Sections.Select(s => new { s.Title, s.Content }).ToList());

            var newSubmission = new PlantSubmission
            {
                Name = PlantName,
                LightRequirementId = SelectedLightRequirement!.Id,
                VarietyId = SelectedVariety!.Id,
                WateringInterval = WateringInterval,
                FertilizingInterval = FertilizingInterval,
                ImagePath = ImagePath,
                Description = sectionsAsJson,
                SubmittedByUserId = _authenticationService.CurrentUser!.Id,
            };

            await _unitOfWork.PlantSubmissions.AddAsync(newSubmission);
            await _unitOfWork.CompleteAsync();
            
            var enableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
            if (enableSuccessNotifications)
            {
                var notification = new Notification
                {
                    Message = "Заявка на добавление растения успешно отправлена!",
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
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }

        [RelayCommand]
        private void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                Title = "Выберите изображение растения"
            };

            if (dialog.ShowDialog() == true)
            {
                string sourceFilePath = dialog.FileName;

                try
                {
                    string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                    string plantifyImagesPath = Path.Combine(appDataPath, "Plantify", "Images");

                    Directory.CreateDirectory(plantifyImagesPath);

                    string newFileName = $"{Guid.NewGuid()}{Path.GetExtension(sourceFilePath)}";
                    string destinationPath = Path.Combine(plantifyImagesPath, newFileName);
                    
                    File.Copy(sourceFilePath, destinationPath);

                    ImagePath = destinationPath;
                }
                catch (Exception)
                {
                    // Optional: Show an error message to the user
                }
            }
        }
        
        private BitmapImage? LoadImage(string? imagePath)
        {
            string imageToLoad = "pack://application:,,,/Images/placeholder.png";

            if (!string.IsNullOrEmpty(imagePath) && Path.IsPathRooted(imagePath) && File.Exists(imagePath))
            {
                imageToLoad = imagePath;
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
