using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
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
        private readonly IMessenger _messenger;
        private List<PlantViewModel> _allPlants = new List<PlantViewModel>();

        [ObservableProperty]
        private ObservableCollection<PlantViewModel> _plants;
        
        [ObservableProperty]
        private ObservableCollection<string> _allCategories;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private string _selectedFilter = "Все";

        partial void OnSearchTextChanged(string value) => PerformFilter();
        partial void OnSelectedFilterChanged(string value) => PerformFilter();

        public EncyclopediaViewModel(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _plants = new ObservableCollection<PlantViewModel>();
            _allCategories = new ObservableCollection<string>();
            
            LoadDataCommand.Execute(null); // Call the combined loading command
        }
        
        [RelayCommand]
        private async Task LoadData()
        {
            await LoadPlants();
            await LoadAllCategories();
        }

        [RelayCommand]
        private async Task LoadAllCategories()
        {
            AllCategories.Clear();
            AllCategories.Add("Все");

            var varieties = await _unitOfWork.Varieties.GetAllAsync();
            foreach (var variety in varieties.OrderBy(v => v.Name))
            {
                AllCategories.Add(variety.Name);
            }

            var lightRequirements = await _unitOfWork.LightRequirements.GetAllAsync();
            foreach (var light in lightRequirements.OrderBy(l => l.Name))
            {
                AllCategories.Add(light.Name);
            }
        }

        [RelayCommand]
        private void ShowPlantDetail(PlantViewModel plant)
        {
            if (plant == null) return;
            _messenger.Send(new ShowPlantDetailMessage(plant));
        }
        
        [RelayCommand]
        private async Task LoadPlants()
        {
            var plantList = await _unitOfWork.Plants.GetAllWithSectionsAsync();
            _allPlants = plantList.Select(p => new PlantViewModel(p)).ToList();
            PerformFilter();
        }

        private void PerformFilter()
        {
            IEnumerable<PlantViewModel> filteredPlants = _allPlants;

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filteredPlants = filteredPlants.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
            
            if (SelectedFilter != "Все")
            {
                filteredPlants = filteredPlants.Where(p => p.PlantModel.Variety.Name == SelectedFilter || p.PlantModel.LightRequirement.Name == SelectedFilter);
            }

            Plants.Clear();
            foreach (var plantVM in filteredPlants)
            {
                Plants.Add(plantVM);
            }
        }
        
        [RelayCommand]
        private void SelectFilter(string filter)
        {
            SelectedFilter = filter;
        }
        
        [RelayCommand]
        private void GoToPlantManagement()
        {
            _messenger.Send(new NavigateMessage(typeof(PlantManagementViewModel)));
        }

        [RelayCommand]
        private void SuggestNewPlant()
        {
            _messenger.Send(new ShowAddPlantSuggestionOverlayMessage());
        }
    }
}
