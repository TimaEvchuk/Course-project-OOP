using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Models.Enums;
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
        private readonly IConfiguration _configuration;
        private string _previousHealthStatusText = "";

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
                        return $"График ухода на неделю ({_weekStartDate:d MMM} - {_weekEndDate:d MMM})";
                    case TaskPeriod.Month:
                        return $"График ухода на месяц ({_monthStartDate:d MMMM} - {_monthEndDate:d MMMM})";
                    case TaskPeriod.Day:
                    default:
                        return $"График ухода на сегодня ({DateTime.Today.ToString("d MMMM", culture)})";
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

        public DashboardViewModel(IUnitOfWork unitOfWork, AuthenticationService authenticationService, IMessenger messenger, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _authenticationService = authenticationService;
            _messenger = messenger;
            _configuration = configuration;

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
            
            if (_authenticationService.CurrentUser != null && _authenticationService.CurrentUser.Id == message.UpdatedUser.Id)
            {
                
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
            IsNotPremiumUser = !_authenticationService.IsPremiumActive();
            OnPropertyChanged(nameof(IsPremiumUser));
            ShowPremiumPurchaseCommand.NotifyCanExecuteChanged();

            
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
            _weekStartDate = today;
            _weekEndDate = today.AddDays(6);
            OnPropertyChanged(nameof(SummaryTitle));

            var weekDays = new Dictionary<DateTime, DayTasksViewModel>();
            for (int i = 0; i < 7; i++)
            {
                var day = today.AddDays(i);
                var dayName = culture.DateTimeFormat.GetDayName(day.DayOfWeek);
                var dayViewModel = new DayTasksViewModel
                {
                    DayName = char.ToUpper(dayName[0]) + dayName.Substring(1),
                    Date = day.ToString("dd MMM", culture)
                };
                weekDays[day.Date] = dayViewModel;
            }

            var userPlants = await _unitOfWork.UserPlants.GetUserPlantsWithPlantDetailsAsync(currentUser.Id);

            foreach (var userPlant in userPlants)
            {
                // --- Watering Tasks ---
                if (userPlant.Plant.WateringInterval > 0)
                {
                    var nextDue = userPlant.LastUserWateringDate.AddDays(userPlant.Plant.WateringInterval);
                    if (nextDue < today)
                    {
                        weekDays[today].Tasks.Add(new CareTaskViewModel(userPlant, "Полив"));
                        while (nextDue < today)
                        {
                            nextDue = nextDue.AddDays(userPlant.Plant.WateringInterval);
                        }
                        if (nextDue == today)
                        {
                           nextDue = nextDue.AddDays(userPlant.Plant.WateringInterval);
                        }
                    }
                    
                    while (nextDue <= _weekEndDate)
                    {
                        if (weekDays.TryGetValue(nextDue.Date, out var dayVM))
                        {
                            dayVM.Tasks.Add(new CareTaskViewModel(userPlant, "Полив"));
                        }
                        nextDue = nextDue.AddDays(userPlant.Plant.WateringInterval);
                    }
                }

                // --- Fertilizing Tasks ---
                if (userPlant.Plant.FertilizingInterval > 0)
                {
                    var nextDue = userPlant.LastFertilizedDate.AddDays(userPlant.Plant.FertilizingInterval);
                    if (nextDue < today)
                    {
                        weekDays[today].Tasks.Add(new CareTaskViewModel(userPlant, "Удобрение"));
                        while (nextDue < today)
                        {
                            nextDue = nextDue.AddDays(userPlant.Plant.FertilizingInterval);
                        }
                        if (nextDue == today)
                        {
                            nextDue = nextDue.AddDays(userPlant.Plant.FertilizingInterval);
                        }
                    }

                    while (nextDue <= _weekEndDate)
                    {
                        if (weekDays.TryGetValue(nextDue.Date, out var dayVM))
                        {
                            dayVM.Tasks.Add(new CareTaskViewModel(userPlant, "Удобрение"));
                        }
                        nextDue = nextDue.AddDays(userPlant.Plant.FertilizingInterval);
                    }
                }
            }
            
            foreach (var day in weekDays.Values.OrderBy(d => d.Date))
            {
                WeekTasks.Add(day);
            }
        }
        
        private async Task LoadMonthTasksAsync()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return;

            MonthTasks.Clear();
            var today = DateTime.Today;
            _monthStartDate = today;
            _monthEndDate = today.AddDays(30);
            OnPropertyChanged(nameof(SummaryTitle));

            var tasksByDay = new Dictionary<DateTime, (int watering, int fertilizing)>();
            for (int i = 0; i <= 30; i++)
            {
                tasksByDay[today.AddDays(i).Date] = (0, 0);
            }

            var userPlants = await _unitOfWork.UserPlants.GetUserPlantsWithPlantDetailsAsync(currentUser.Id);

            foreach (var plant in userPlants)
            {
                // --- Watering Tasks ---
                if (plant.Plant.WateringInterval > 0)
                {
                    var nextDue = plant.LastUserWateringDate.AddDays(plant.Plant.WateringInterval);
                    if (nextDue < today)
                    {
                        var current = tasksByDay[today];
                        tasksByDay[today] = (current.watering + 1, current.fertilizing);
                        
                        while (nextDue < today)
                        {
                            nextDue = nextDue.AddDays(plant.Plant.WateringInterval);
                        }
                        if (nextDue == today)
                        {
                            nextDue = nextDue.AddDays(plant.Plant.WateringInterval);
                        }
                    }
                    
                    while (nextDue <= _monthEndDate)
                    {
                        if (tasksByDay.ContainsKey(nextDue.Date))
                        {
                            var current = tasksByDay[nextDue.Date];
                            tasksByDay[nextDue.Date] = (current.watering + 1, current.fertilizing);
                        }
                        nextDue = nextDue.AddDays(plant.Plant.WateringInterval);
                    }
                }

                // --- Fertilizing Tasks ---
                if (plant.Plant.FertilizingInterval > 0)
                {
                    var nextDue = plant.LastFertilizedDate.AddDays(plant.Plant.FertilizingInterval);
                    if (nextDue < today)
                    {
                        var current = tasksByDay[today];
                        tasksByDay[today] = (current.watering, current.fertilizing + 1);
                        
                        while (nextDue < today)
                        {
                            nextDue = nextDue.AddDays(plant.Plant.FertilizingInterval);
                        }
                        if (nextDue == today)
                        {
                            nextDue = nextDue.AddDays(plant.Plant.FertilizingInterval);
                        }
                    }

                    while (nextDue <= _monthEndDate)
                    {
                        if (tasksByDay.ContainsKey(nextDue.Date))
                        {
                            var current = tasksByDay[nextDue.Date];
                            tasksByDay[nextDue.Date] = (current.watering, current.fertilizing + 1);
                        }
                        nextDue = nextDue.AddDays(plant.Plant.FertilizingInterval);
                    }
                }
            }

            for (int i = 0; i <= 30; i++)
            {
                var day = today.AddDays(i);
                var vm = new MonthDayViewModel { FullDate = day };
                if (tasksByDay.TryGetValue(day.Date, out var tasksForDay))
                {
                    vm.WateringTaskCount = tasksForDay.watering;
                    vm.FertilizingTaskCount = tasksForDay.fertilizing;
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
            
            if (GardenHealthStatusText == "Требует внимания" && _previousHealthStatusText != "Требует внимания")
            {
                var enableCareNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableCareNotifications");
                if (enableCareNotifications)
                {
                    var notification = new Notification
                    {
                        Message = "Уровень здоровья сада стал плохим!",
                        Timestamp = DateTime.Now,
                        Type = NotificationType.Warning,
                        UserId = currentUser.Id,
                        IsDismissed = false
                    };
                    await _unitOfWork.Notifications.AddAsync(notification);
                    await _unitOfWork.CompleteAsync();
                    _messenger.Send(new NewNotificationMessage(notification));
                }
            }
            _previousHealthStatusText = GardenHealthStatusText;
        }
    }
}
