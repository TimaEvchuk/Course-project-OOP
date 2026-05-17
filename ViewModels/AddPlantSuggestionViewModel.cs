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
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;

namespace Plantify.ViewModels
{
    public partial class AddPlantSuggestionViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;

        [ObservableProperty]
        private string? _plantName;

        [ObservableProperty]
        private string? _lightRequirement;

        [ObservableProperty]
        private string? _variety;

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
        private string? _description;

        [ObservableProperty]
        private string? _errorMessage;

        public ObservableCollection<string> Varieties { get; }
        public ObservableCollection<string> LightRequirements { get; }

        public string Title => "Предложить новое растение";

        public AddPlantSuggestionViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;

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

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(PlantName) || string.IsNullOrWhiteSpace(LightRequirement) || string.IsNullOrWhiteSpace(Variety))
            {
                ErrorMessage = "Пожалуйста, заполните все обязательные поля.";
                return;
            }
            if (_authenticationService.CurrentUser == null)
            {
                ErrorMessage = "Ошибка: пользователь не авторизован.";
                return;
            }

            ErrorMessage = null;

            var newSubmission = new PlantSubmission
            {
                Name = PlantName,
                LightRequirement = LightRequirement,
                Variety = Variety,
                WateringInterval = WateringInterval,
                FertilizingInterval = FertilizingInterval,
                ImagePath = ImagePath,
                Description = Description,
                SubmittedByUserId = _authenticationService.CurrentUser.Id,
            };

            await _unitOfWork.PlantSubmissions.AddAsync(newSubmission);
            await _unitOfWork.CompleteAsync();

            _messenger.Send(new CloseOverlayMessage());
            // Optionally, send a message to show a 'Thank you' notification.
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
                // Here, we should copy the file to a designated 'uploads' folder within the project
                // and then store the relative path. For simplicity now, we'll just store the full path.
                // This is not ideal for a real application but sufficient for this step.
                ImagePath = openFileDialog.FileName;
            }
        }
    }
}
