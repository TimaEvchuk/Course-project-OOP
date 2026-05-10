using Plantify.Models;
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Plantify.ViewModels
{
    public partial class UserPlantViewModel : BaseViewModel
    {
        private readonly UserPlant _userPlant;

        public PlantViewModel PlantViewModel { get; }

        public string Name => _userPlant.CustomName ?? PlantViewModel.Name;
        public string Species => PlantViewModel.Variety;
        public string Location => _userPlant.Location ?? "Не указано";
        public string LightRequirement => PlantViewModel.Plant.LightRequirement;

        public string NextWateringDue
        {
            get
            {
                var daysSinceWatered = (DateTime.Today - _userPlant.LastUserWateringDate).Days;
                var wateringInterval = PlantViewModel.Plant.WateringInterval;
                var daysLeft = wateringInterval - daysSinceWatered;

                if (daysLeft <= 0) return "Сегодня";
                if (daysLeft == 1) return "Завтра";
                return $"{daysLeft} дней";
            }
        }

        public UserPlantViewModel(UserPlant userPlant)
        {
            _userPlant = userPlant;
            PlantViewModel = new PlantViewModel(userPlant.Plant);
               
        }
    }
}
