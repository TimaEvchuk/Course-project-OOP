using Plantify.Models;
using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Plantify.ViewModels
{
    public partial class UserPlantViewModel : ObservableObject
    {
        private readonly UserPlant _userPlant;

        public UserPlantViewModel(UserPlant userPlant)
        {
            _userPlant = userPlant;
        }

        public string Name => string.IsNullOrEmpty(_userPlant.CustomName) ? _userPlant.Plant.Name : _userPlant.CustomName;
        public string Species => _userPlant.Plant.Name;
        public string Location => _userPlant.Location ?? "Не указано";
        public PlantViewModel PlantViewModel => new PlantViewModel(_userPlant.Plant);

        public string LightRequirement => _userPlant.Plant.LightRequirement;
        public int WateringInterval => _userPlant.Plant.WateringInterval;

        public string NextWateringDue
        {
            get
            {
                var nextWateringDate = _userPlant.LastWatered.AddDays(WateringInterval);
                var daysUntilWatering = (nextWateringDate - DateTime.Today).Days;

                if (daysUntilWatering <= 0) return "Сегодня";
                if (daysUntilWatering == 1) return "Завтра";
                return $"Через {daysUntilWatering} дней";
            }
        }
        
        public string WateringStatus
        {
            get
            {
                var nextWateringDate = _userPlant.LastWatered.AddDays(WateringInterval);
                var daysUntilWatering = (nextWateringDate - DateTime.Today).Days;

                if (daysUntilWatering < 0) return $"Пропущено {-daysUntilWatering} дн.";
                return "OK";
            }
        }
    }
}
