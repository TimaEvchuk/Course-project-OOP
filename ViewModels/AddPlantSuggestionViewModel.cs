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
using System.Text.Json;
using System.Threading.Tasks;
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

        private int _wateringInterval;
        public int WateringInterval
        {
            get => _wateringInterval;
            set
            {
                if (value < 0) value = 0;
                if (value > 999) value = 999;
                SetProperty(ref _wateringInterval, value);
            }
        }

        private int _fertilizingInterval;
        public int FertilizingInterval
        {
            get => _fertilizingInterval;
            set
            {
                if (value < 0) value = 0;
                if (value > 999) value = 999;
                SetProperty(ref _fertilizingInterval, value);
            }
        }

        [ObservableProperty]
        private string? _imagePath;

        [ObservableProperty]
        private string? _errorMessage;
        
        public ObservableCollection<PlantSectionViewModel> Sections { get; set; }

        public ObservableCollection<Variety> Varieties { get; }
        public ObservableCollection<LightRequirement> LightRequirements { get; }

        public string Title => "Предложить новое растение";

        public AddPlantSuggestionViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;
            _configuration = configuration;

            Varieties = new ObservableCollection<Variety>();
            LightRequirements = new ObservableCollection<LightRequirement>();
            Sections = new ObservableCollection<PlantSectionViewModel>
            {
                new PlantSectionViewModel { Title = "Описание", Content = "" }
            };
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
                LightRequirementId = SelectedLightRequirement.Id,
                VarietyId = SelectedVariety.Id,
                WateringInterval = WateringInterval,
                FertilizingInterval = FertilizingInterval,
                ImagePath = ImagePath,
                Description = sectionsAsJson,
                SubmittedByUserId = _authenticationService.CurrentUser.Id,
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
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
            }
        }
    }
}
