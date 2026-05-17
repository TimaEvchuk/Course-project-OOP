using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Plantify.ViewModels
{
    public enum TaskPeriod { Day, Week, Month }

    public partial class DashboardViewModel : BaseViewModel, 
        IRecipient<GardenStateChangedMessage>, 
        IRecipient<UserLoggedInMessage>,
        IRecipient<PremiumStatusChangedMessage>
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
        private bool _isNotPremiumUser;

        public bool IsPremiumUser => !IsNotPremiumUser;

        // Properties for Day View
        [ObservableProperty]
        private ObservableCollection<CareTaskViewModel> _todaysTasks = new();

        [ObservableProperty]
        private bool _showNoTasksMessage;
        
        // Properties for Week View
        [ObservableProperty]
        private ObservableCollection<DayTasksViewModel> _weekTasks = new();
        
        // Properties for Month View
        [ObservableProperty]
        private ObservableCollection<MonthDayViewModel> _monthTasks = new();

        private DateTime _weekStartDate;
        private DateTime _weekEndDate;
        private DateTime _monthStartDate;
        private DateTime _monthEndDate;

        // View management properties
        [ObservableProperty]
        private TaskPeriod _selectedPeriod;

        public string SummaryTitle
        {
            get
            {
                var culture = new CultureInfo("ru-RU");
                switch (SelectedPeriod)
                {
                    case TaskPeriod.Week:
                        return $"Сводка задач на неделю ({_weekStartDate:d MMM} - {_weekEndDate:d MMM})";
                    case TaskPeriod.Month:
                        return $"Сводка задач на месяц ({_monthStartDate:d MMMM} - {_monthEndDate:d MMMM})";
                    case TaskPeriod.Day:
                    default:
                        return $"Сводка задач на сегодня ({DateTime.Today.ToString("d MMMM", culture)})";
                }
            }
        }

        partial void OnSelectedPeriodChanged(TaskPeriod value)
        {
            OnPropertyChanged(nameof(SummaryTitle));
        }

        [ObservableProperty]
        private bool _isDayViewVisible;

        [ObservableProperty]
        private bool _isWeekViewVisible;
        
        [ObservableProperty]
        private bool _isMonthViewVisible;

        public DashboardViewModel(IUnitOfWork unitOfWork, AuthenticationService authenticationService, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _authenticationService = authenticationService;
            _messenger = messenger;

            _messenger.Register<GardenStateChangedMessage>(this);
            _messenger.Register<UserLoggedInMessage>(this);
            _messenger.Register<PremiumStatusChangedMessage>(this);

            // Set initial view state
            SelectedPeriod = TaskPeriod.Day;
            IsDayViewVisible = true;
            
            LoadedCommand.Execute(null);
        }

        [RelayCommand(CanExecute = nameof(IsNotPremiumUser))]
        private void ShowPremiumPurchase()
        {
            _messenger.Send(new ShowPremiumPurchaseOverlayMessage());
        }

        [RelayCommand]
        private async Task SetPeriod(TaskPeriod period)
        {
            Debug.WriteLine($"[DEBUG] SetPeriod called with: {period}");
            SelectedPeriod = period;
            IsDayViewVisible = period == TaskPeriod.Day;
            IsWeekViewVisible = period == TaskPeriod.Week;
            IsMonthViewVisible = period == TaskPeriod.Month;
            Debug.WriteLine($"[DEBUG] IsDayViewVisible: {IsDayViewVisible}, IsWeekViewVisible: {IsWeekViewVisible}, IsMonthViewVisible: {IsMonthViewVisible}");

            // Load data for the new period
            await LoadTasksAsync();
        }

        public async void Receive(GardenStateChangedMessage message)
        {
            await UpdateDashboard();
        }
        
        public void Receive(UserLoggedInMessage message)
        {
            // This message is sent on login
            UpdatePremiumStatus();
        }

        public void Receive(PremiumStatusChangedMessage message)
        {
            // This message is sent after a premium purchase, so we also need to update the user object
            // in the authentication service before re-evaluating the status.
            if (_authenticationService.CurrentUser != null && _authenticationService.CurrentUser.Id == message.UpdatedUser.Id)
            {
                // This is a simple update, for more complex scenarios, a full re-fetch or AutoMapper would be better.
                _authenticationService.CurrentUser.IsPremium = message.UpdatedUser.IsPremium;
                _authenticationService.CurrentUser.PremiumStartDate = message.UpdatedUser.PremiumStartDate;
                _authenticationService.CurrentUser.PremiumEndDate = message.UpdatedUser.PremiumEndDate;
            }
            UpdatePremiumStatus();
        }

        [RelayCommand]
        private async Task Loaded()
        {
            await UpdateDashboard();
        }

        private async Task UpdateDashboard()
        {
            UpdatePremiumStatus();
            await CalculateGardenHealth();
            await LoadTasksAsync();
        }

        private void UpdatePremiumStatus()
        {
            IsNotPremiumUser = !_authenticationService.IsCurrentUserPremium();
            OnPropertyChanged(nameof(IsPremiumUser));
            ShowPremiumPurchaseCommand.NotifyCanExecuteChanged();

            // If user is not premium and is viewing the premium-only month tab, switch them back to the day view.
            if (IsNotPremiumUser && SelectedPeriod == TaskPeriod.Month)
            {
                SetPeriodCommand.Execute(TaskPeriod.Day);
            }
        }

        private async Task LoadTasksAsync()
        {
            switch (SelectedPeriod)
            {
                case TaskPeriod.Day:
                    await LoadDayTasksAsync();
                    break;
                case TaskPeriod.Week:
                    await LoadWeekTasksAsync();
                    break;
                case TaskPeriod.Month:
                    await LoadMonthTasksAsync();
                    break;
            }
        }

        private async Task LoadDayTasksAsync()
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

        private async Task LoadWeekTasksAsync()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;

            WeekTasks.Clear();
            var culture = new CultureInfo("ru-RU");
            var today = DateTime.Today;
            var weekAhead = Enumerable.Range(0, 7).Select(i => today.AddDays(i)).ToList();

            _weekStartDate = today;
            _weekEndDate = weekAhead.Last();
            OnPropertyChanged(nameof(SummaryTitle));

            // Initialize the 7 day view models
            foreach (var day in weekAhead)
            {
                var dayName = culture.DateTimeFormat.GetDayName(day.DayOfWeek);
                WeekTasks.Add(new DayTasksViewModel
                {
                    DayName = char.ToUpper(dayName[0]) + dayName.Substring(1),
                    Date = day.ToString("dd MMM", culture)
                });
            }
            
            var userPlants = await _unitOfWork.UserPlants.GetUserPlantsWithPlantDetailsAsync(currentUser.Id);

            foreach (var userPlant in userPlants)
            {
                // Check for watering tasks
                var nextWateringDate = userPlant.LastUserWateringDate.AddDays(userPlant.Plant.WateringInterval);
                if (nextWateringDate >= today && nextWateringDate < today.AddDays(7))
                {
                    var dayIndex = (nextWateringDate.Date - today).Days;
                    if (dayIndex >= 0 && dayIndex < 7)
                    {
                        WeekTasks[dayIndex].Tasks.Add(new CareTaskViewModel(userPlant, "Полив"));
                    }
                }

                // Check for fertilizing tasks
                var nextFertilizingDate = userPlant.LastFertilizedDate.AddDays(userPlant.Plant.FertilizingInterval);
                if (nextFertilizingDate >= today && nextFertilizingDate < today.AddDays(7))
                {
                    var dayIndex = (nextFertilizingDate.Date - today).Days;
                    if (dayIndex >= 0 && dayIndex < 7)
                    {
                        WeekTasks[dayIndex].Tasks.Add(new CareTaskViewModel(userPlant, "Удобрение"));
                    }
                }
            }
        }
        
        private async Task LoadMonthTasksAsync()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;

            MonthTasks.Clear();
            var today = DateTime.Today;
            _monthStartDate = today;
            _monthEndDate = today.AddDays(30); // 31 days total
            OnPropertyChanged(nameof(SummaryTitle));

            var userPlants = await _unitOfWork.UserPlants.GetUserPlantsWithPlantDetailsAsync(currentUser.Id);
            
            var allTasks = new List<(DateTime Date, string Type)>();

            foreach (var plant in userPlants)
            {
                // Get all watering dates in the next 31 days
                var nextWatering = plant.LastUserWateringDate.AddDays(plant.Plant.WateringInterval);
                while (nextWatering <= _monthEndDate)
                {
                    if (nextWatering >= _monthStartDate)
                    {
                        allTasks.Add((nextWatering, "Полив"));
                    }
                    if (plant.Plant.WateringInterval == 0) break;
                    nextWatering = nextWatering.AddDays(plant.Plant.WateringInterval);
                }

                // Get all fertilizing dates in the next 31 days
                var nextFertilizing = plant.LastFertilizedDate.AddDays(plant.Plant.FertilizingInterval);
                while (nextFertilizing <= _monthEndDate)
                {
                    if (nextFertilizing >= _monthStartDate)
                    {
                        allTasks.Add((nextFertilizing, "Удобрение"));
                    }
                    if (plant.Plant.FertilizingInterval == 0) break;
                    nextFertilizing = nextFertilizing.AddDays(plant.Plant.FertilizingInterval);
                }
            }

            var tasksByDay = allTasks.GroupBy(t => t.Date)
                                     .ToDictionary(g => g.Key, g => g.ToList());

            for (int i = 0; i < 31; i++) // 7 columns * 5 rows
            {
                var day = today.AddDays(i);
                var vm = new MonthDayViewModel { FullDate = day };

                if (tasksByDay.TryGetValue(day, out var tasksForDay))
                {
                    vm.WateringTaskCount = tasksForDay.Count(t => t.Type == "Полив");
                    vm.FertilizingTaskCount = tasksForDay.Count(t => t.Type == "Удобрение");
                }
                
                MonthTasks.Add(vm);
            }
        }

        private async Task CalculateGardenHealth()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;

            var userPlants = await _unitOfWork.UserPlants.GetUserPlantsWithPlantDetailsAsync(currentUser.Id);
            
            var totalPlants = userPlants.Count();
            if (totalPlants == 0)
            {
                GardenHealthPercentage = 0;
                GardenHealthBrush = (SolidColorBrush)Application.Current.FindResource("BrushGray");
                GardenHealthStatusText = "У вас сейчас нету растений";
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
