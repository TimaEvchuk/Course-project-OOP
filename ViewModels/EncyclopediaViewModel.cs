using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging; // Added for IMessenger
using Plantify.Data;
using Plantify.Messages; // Added for NavigateMessage
using Plantify.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class EncyclopediaViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger; // Injected messenger
        private List<Plant> _allPlants = new List<Plant>();

        [ObservableProperty]
        private ObservableCollection<Plant> _plants;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private ObservableCollection<string> _difficulties;

        [ObservableProperty]
        private string _selectedDifficulty = "All";

        [ObservableProperty]
        private ObservableCollection<string> _lightRequirements;

        [ObservableProperty]
        private string _selectedLightRequirement = "All";


        partial void OnSearchTextChanged(string value) => PerformFilter();
        partial void OnSelectedDifficultyChanged(string value) => PerformFilter();
        partial void OnSelectedLightRequirementChanged(string value) => PerformFilter();


        public EncyclopediaViewModel(IUnitOfWork unitOfWork, IMessenger messenger) // Messenger injected
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger; // Assign messenger
            _plants = new ObservableCollection<Plant>();
            _difficulties = new ObservableCollection<string> { "All", "Easy", "Medium", "Hard" };
            _lightRequirements = new ObservableCollection<string> { "All", "Low to Bright Indirect", "Bright Indirect" };
            
            LoadPlantsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadPlants()
        {
            var plantList = await _unitOfWork.Plants.GetAllAsync();
            _allPlants = new List<Plant>(plantList);
            PerformFilter();
        }

        private void PerformFilter()
        {
            IEnumerable<Plant> filteredPlants = _allPlants;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filteredPlants = filteredPlants.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedDifficulty != "All")
            {
                filteredPlants = filteredPlants.Where(p => p.Difficulty == SelectedDifficulty);
            }

            if (SelectedLightRequirement != "All")
            {
                filteredPlants = filteredPlants.Where(p => p.LightRequirement == SelectedLightRequirement);
            }

            Plants.Clear();
            foreach (var plant in filteredPlants)
            {
                Plants.Add(plant);
            }
        }

        [RelayCommand]
        private void GoToPlantManagement()
        {
            _messenger.Send(new NavigateMessage(typeof(PlantManagementViewModel)));
        }
    }
}
