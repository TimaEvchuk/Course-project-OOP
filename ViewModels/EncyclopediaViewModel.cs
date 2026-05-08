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
        private List<PlantViewModel> _allPlants = new List<PlantViewModel>();

        [ObservableProperty]
        private ObservableCollection<PlantViewModel> _plants;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private string _selectedFilter = "Все";

        partial void OnSearchTextChanged(string value) => PerformFilter();
        partial void OnSelectedFilterChanged(string value) => PerformFilter();

        public EncyclopediaViewModel(IUnitOfWork unitOfWork, IMessenger messenger) // Messenger injected
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger; // Assign messenger
            _plants = new ObservableCollection<PlantViewModel>();
            LoadPlantsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadPlants()
        {
            var plantList = await _unitOfWork.Plants.GetAllAsync();
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
                case "Для новичков":
                    filteredPlants = filteredPlants.Where(p => p.Difficulty == "Easy");
                    break;
                case "Тенелюбивые":
                    filteredPlants = filteredPlants.Where(p => p.Plant.LightRequirement.Contains("Low", StringComparison.OrdinalIgnoreCase));
                    break;
                // "Все" and other unimplemented filters will show all (or all from search)
                default:
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
