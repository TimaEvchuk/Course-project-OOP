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
using System.Windows;

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
            }
            else
            {
                // Reset fields for 'Add' mode
                CustomName = "";
                Location = "";
                SelectedPlant = null;
            }
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
            if (SelectedPlant == null)
            {
                MessageBox.Show("Пожалуйста, выберите растение.");
                return;
            }

            if (_authenticationService.CurrentUser == null)
            {
                MessageBox.Show("Ошибка: пользователь не авторизован.");
                return;
            }

            if (IsEditMode && OriginalUserPlant != null)
            {
                // Update existing plant
                OriginalUserPlant.PlantId = SelectedPlant.Id;
                OriginalUserPlant.CustomName = CustomName;
                OriginalUserPlant.Location = Location;
                
                _unitOfWork.UserPlants.Update(OriginalUserPlant);
            }
            else
            {
                // Create new plant
                var newUserPlant = new UserPlant
                {
                    PlantId = SelectedPlant.Id,
                    UserId = _authenticationService.CurrentUser.Id,
                    CustomName = CustomName,
                    Location = Location,
                    LastUserWateringDate = DateTime.Today
                };
                await _unitOfWork.UserPlants.AddAsync(newUserPlant);
            }
            
            await _unitOfWork.CompleteAsync();

            _messenger.Send(new CloseOverlayMessage());
            _messenger.Send(new NavigateMessage(typeof(MyGardenViewModel))); // Refresh MyGarden view
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }
    }
}
