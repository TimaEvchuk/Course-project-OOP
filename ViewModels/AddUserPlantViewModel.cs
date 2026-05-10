using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using Microsoft.EntityFrameworkCore; // For .Include()

namespace Plantify.ViewModels
{
    public partial class AddUserPlantViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private ObservableCollection<Plant> _allPlants = new();

        [ObservableProperty]
        private Plant? _selectedPlant;
        
        [ObservableProperty]
        private string? _customName;
        
        [ObservableProperty]
        private string? _location;
        
        public AddUserPlantViewModel(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            LoadAllPlantsCommand.Execute(null);
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
        }
        
        [RelayCommand]
        private async Task Save()
        {
            if (SelectedPlant == null)
            {
                MessageBox.Show("Please select a plant.");
                return;
            }

            var newUserPlant = new UserPlant
            {
                PlantId = SelectedPlant.Id,
                UserId = 1, // Hardcoded for now
                CustomName = CustomName,
                Location = Location,
                LastUserWateringDate = DateTime.Today // Corrected property name
            };

            await _unitOfWork.UserPlants.AddAsync(newUserPlant);
            await _unitOfWork.CompleteAsync();

            _messenger.Send(new CloseOverlayMessage());
            _messenger.Send(new NavigateMessage(typeof(MyGardenViewModel)));
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }
    }
}
