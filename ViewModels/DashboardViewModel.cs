using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plantify.Data;
using Plantify.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Plantify.Messages; // Added for GardenStateChangedMessage

using CommunityToolkit.Mvvm.Messaging;

namespace Plantify.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel, IRecipient<GardenStateChangedMessage>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuthenticationService _authenticationService;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private int _gardenHealthPercentage;

        [ObservableProperty]
        private SolidColorBrush _gardenHealthBrush = new(Colors.Transparent);

        [ObservableProperty]
        private string _gardenHealthStatusText = "Нет данных";

        [ObservableProperty]
        private ObservableCollection<CareTaskViewModel> _todaysTasks = new();

        [ObservableProperty]
        private bool _showNoTasksMessage;

        public DashboardViewModel(IUnitOfWork unitOfWork, AuthenticationService authenticationService, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _authenticationService = authenticationService;
            _messenger = messenger;

            _messenger.Register<GardenStateChangedMessage>(this);
            
            LoadedCommand.Execute(null);
        }

        public async void Receive(GardenStateChangedMessage message)
        {
            await CalculateGardenHealth();
            await LoadTodaysTasks();
        }

        [RelayCommand]
        private async Task Loaded()
        {
            await CalculateGardenHealth();
            await LoadTodaysTasks();
        }

        private async Task LoadTodaysTasks()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;
            
            var userPlants = await _unitOfWork.UserPlants.GetUserPlantsWithPlantDetailsAsync(currentUser.Id);
            
            TodaysTasks.Clear();

            foreach (var userPlant in userPlants)
            {
                bool needsWatering = DateTime.Today >= userPlant.LastUserWateringDate.AddDays(userPlant.Plant.WateringInterval);
                bool needsFertilizing = DateTime.Today >= userPlant.LastFertilizedDate.AddDays(userPlant.Plant.FertilizingInterval);

                if (needsWatering)
                {
                    TodaysTasks.Add(new CareTaskViewModel(userPlant, "Полив"));
                }
                if (needsFertilizing)
                {
                    TodaysTasks.Add(new CareTaskViewModel(userPlant, "Удобрение"));
                }
            }
            
            ShowNoTasksMessage = TodaysTasks.Count == 0;
        }

        private async Task CalculateGardenHealth()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;

            var userPlants = await _unitOfWork.UserPlants.GetUserPlantsWithPlantDetailsAsync(currentUser.Id);
            
            var totalPlants = userPlants.Count();
            if (totalPlants == 0)
            {
                GardenHealthPercentage = 100;
                GardenHealthBrush = new SolidColorBrush(Color.FromRgb(60, 179, 113)); // MediumSeaGreen
                GardenHealthStatusText = "Сад в порядке";
                return;
            }

            int onTimePlants = 0;
            foreach (var userPlant in userPlants)
            {
                bool isWaterOverdue = DateTime.Today >= userPlant.LastUserWateringDate.AddDays(userPlant.Plant.WateringInterval);
                bool isFertilizerOverdue = DateTime.Today >= userPlant.LastFertilizedDate.AddDays(userPlant.Plant.FertilizingInterval);

                if (!isWaterOverdue && !isFertilizerOverdue)
                {
                    onTimePlants++;
                }
            }
            
            var percentage = (int)Math.Round((double)onTimePlants / totalPlants * 100);
            GardenHealthPercentage = percentage;

            if (percentage <= 40)
            {
                GardenHealthBrush = new SolidColorBrush(Colors.IndianRed);
                GardenHealthStatusText = "Требует внимания";
            }
            else if (percentage <= 80)
            {
                GardenHealthBrush = new SolidColorBrush(Colors.Gold);
                GardenHealthStatusText = "Хорошее состояние";
            }
            else
            {
                GardenHealthBrush = new SolidColorBrush(Color.FromRgb(60, 179, 113)); // MediumSeaGreen
                GardenHealthStatusText = "Отличное состояние";
            }
        }
    }
}
