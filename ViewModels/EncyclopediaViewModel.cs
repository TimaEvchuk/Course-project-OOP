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
        private readonly IMessenger _messenger;
        private List<PlantViewModel> _allPlants = new List<PlantViewModel>();

        [ObservableProperty]
        private ObservableCollection<PlantViewModel> _plants;

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
            LoadPlantsCommand.Execute(null);
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

            // Filter by selected category pill
            switch (SelectedFilter)
            {
                case "Лиственные":
                    filteredPlants = filteredPlants.Where(p => p.PlantModel.Variety == "Лиственные");
                    break;
                case "Суккуленты":
                    filteredPlants = filteredPlants.Where(p => p.PlantModel.Variety == "Суккуленты");
                    break;
                case "Лианы":
                    filteredPlants = filteredPlants.Where(p => p.PlantModel.Variety == "Лианы");
                    break;
                case "Тенелюбивые":
                    filteredPlants = filteredPlants.Where(p => p.PlantModel.LightRequirement == "Тенелюбивые");
                    break;
                case "Светолюбивые":
                    filteredPlants = filteredPlants.Where(p => p.PlantModel.LightRequirement == "Светолюбивые");
                    break;
                case "Теневыносливые":
                    filteredPlants = filteredPlants.Where(p => p.PlantModel.LightRequirement == "Теневыносливые");
                    break;
                case "Все":
                default:
                    // No additional filtering needed
                    break;
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
    }
}
