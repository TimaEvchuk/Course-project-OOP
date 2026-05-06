using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plantify.Data;
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


        public EncyclopediaViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
    }
}
