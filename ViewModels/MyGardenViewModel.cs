using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Plantify.Data;
using Plantify.Dialogs;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Models.Enums;
using Plantify.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class MyGardenViewModel : BaseViewModel, IRecipient<UserPlantSelectionChangedMessage>, IRecipient<GardenStateChangedMessage>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;
        private readonly IDialogService _dialogService;
        private readonly IConfiguration _configuration;
        private bool _isLoading = false;
        private List<UserPlantViewModel> _allUserPlants = new();

        [ObservableProperty]
        private ObservableCollection<UserPlantViewModel> _userPlants = new();

        [ObservableProperty]
        private string _tasksSummary = "Задачи на сегодня: 0 растений ждут полива";
        
        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private string _selectedCategory = "Все";

        partial void OnSearchTextChanged(string value) => PerformFilter();
        partial void OnSelectedCategoryChanged(string value) => PerformFilter();

        [ObservableProperty]
        private bool _showEmptyState;

        [ObservableProperty]
        private bool _showEmptyFilterState;

        [ObservableProperty]
        private bool _isInMassSelectionMode;

        private List<UserPlantViewModel> _plantsSelectedByFilterToggle = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectAllFilteredButtonText))]
        private bool _isSelectAllFilteredActive;

        public string SelectAllFilteredButtonText => IsSelectAllFilteredActive ? "Отменить" : "Отметить все";
        
        private enum CareActionType { Water, Fertilize }

        public MyGardenViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService, IDialogService dialogService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;
            _dialogService = dialogService;
            _configuration = configuration;

            _messenger.Register<UserPlantSelectionChangedMessage>(this);
            _messenger.Register<GardenStateChangedMessage>(this);
            LoadDataCommand.Execute(null);
        }        
        [RelayCommand]
        private async Task LoadData()
        {
            await LoadCategories();
            await LoadUserPlants(true);
        }

        [RelayCommand]
        private async Task LoadCategories()
        {
            Categories.Clear();
            Categories.Add("Все");

            var varieties = await _unitOfWork.Varieties.GetAllAsync();
            foreach (var variety in varieties.OrderBy(v => v.Name))
            {
                Categories.Add(variety.Name);
            }

            var lightRequirements = await _unitOfWork.LightRequirements.GetAllAsync();
            foreach (var light in lightRequirements.OrderBy(l => l.Name))
            {
                Categories.Add(light.Name);
            }
        }

        partial void OnIsInMassSelectionModeChanged(bool value)
        {
            UpdateTasksSummary();
            OnPropertyChanged(nameof(SelectedPlantsCount));
            OnPropertyChanged(nameof(IsAnyPlantSelected));
        }

        public int SelectedPlantsCount => UserPlants.Count(p => p.IsTaskCompletedToday);
        public bool IsAnyPlantSelected => SelectedPlantsCount > 0;

        [RelayCommand]
        private void AddPlant()
        {
            _messenger.Send(new ShowAddUserPlantOverlayMessage((Plant?)null));
        }

        [RelayCommand]
        private void MarkAllTasksAsCompleted()
        {
            IsInMassSelectionMode = true;
            foreach (var plantVM in UserPlants)
            {
                if (plantVM.DaysToNextWatering <= 0 || plantVM.DaysToNextFertilizing <= 0)
                {
                    plantVM.IsTaskCompletedToday = true;
                }
            }
            UpdateTasksSummary();
        }

        [RelayCommand]
        private void CancelMassSelection()
        {
            IsInMassSelectionMode = false;
            foreach (var plantVM in UserPlants)
            {
                plantVM.IsTaskCompletedToday = false;
            }

            _plantsSelectedByFilterToggle.Clear();
            IsSelectAllFilteredActive = false;

            UpdateTasksSummary();
        }

        [RelayCommand]
        private void ToggleSelectAllFiltered()
        {
            IsSelectAllFilteredActive = !IsSelectAllFilteredActive;

            if (IsSelectAllFilteredActive)
            {
                // Action: Select All
                _plantsSelectedByFilterToggle.Clear();
                foreach (var plantVM in UserPlants)
                {
                    if (!plantVM.IsTaskCompletedToday)
                    {
                        plantVM.IsTaskCompletedToday = true;
                        _plantsSelectedByFilterToggle.Add(plantVM);
                    }
                }
            }
            else
            {
                // Action: Cancel
                foreach (var plantVM in _plantsSelectedByFilterToggle)
                {
                    plantVM.IsTaskCompletedToday = false;
                }
                _plantsSelectedByFilterToggle.Clear();
            }
        }

        [RelayCommand]
        private async Task ConfirmWaterAction()
        {
            await ConfirmCareActionAsync(CareActionType.Water);
        }

        [RelayCommand]
        private async Task ConfirmFertilizeAction()
        {
            await ConfirmCareActionAsync(CareActionType.Fertilize);
        }

        [RelayCommand(CanExecute = nameof(IsAnyPlantSelected))]
        private async Task DeleteSelectedPlants()
        {
            var selectedPlantVMs = UserPlants.Where(p => p.IsTaskCompletedToday).ToList();
            if (!selectedPlantVMs.Any()) return;

            var result = _dialogService.ShowConfirmationDialog($"Вы уверены, что хотите удалить {selectedPlantVMs.Count} растений?");

            if (result.Confirmed)
            {
                var plantIdsToDelete = selectedPlantVMs.Select(vm => vm.UserPlantId).ToList();
                var plantsInDb = await _unitOfWork.UserPlants.FindAsync(p => plantIdsToDelete.Contains(p.Id));
                Notification? notification = null;

                _unitOfWork.UserPlants.RemoveRange(plantsInDb);
                _allUserPlants.RemoveAll(p => plantIdsToDelete.Contains(p.UserPlantId));
                
                var enableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
                if (enableSuccessNotifications)
                {
                    notification = new Notification
                    {
                        Message = $"🗑️ Успешно удалено {plantIdsToDelete.Count} растений!",
                        Timestamp = DateTime.Now,
                        Type = NotificationType.ActionSuccess,
                        UserId = _authenticationService.CurrentUser.Id,
                        IsDismissed = false
                    };
                    await _unitOfWork.Notifications.AddAsync(notification);
                }
                
                await _unitOfWork.CompleteAsync();
                _unitOfWork.DetachAllEntities();

                if(notification != null)
                {
                    _messenger.Send(new NewNotificationMessage(notification));
                }
                
                CancelMassSelection();
                PerformFilter();
            }
        }

        private async Task ConfirmCareActionAsync(CareActionType actionType)
        {
            if (_authenticationService.CurrentUser == null) return;
            var selectedPlantVMs = UserPlants.Where(p => p.IsTaskCompletedToday).ToList();
            if (!selectedPlantVMs.Any()) return;

            Func<UserPlantViewModel, bool> isDueTodayPredicate = vm => actionType == CareActionType.Water ? vm.DaysToNextWatering <= 0 : vm.DaysToNextFertilizing <= 0;
            var dueTodayVMs = selectedPlantVMs.Where(isDueTodayPredicate).ToList();
            var notDueVMs = selectedPlantVMs.Except(dueTodayVMs).ToList();
            var confirmedForUpdateVMs = new List<UserPlantViewModel>(dueTodayVMs);
            bool? applyToAllDecision = null;

            foreach (var plantVM in notDueVMs)
            {
                bool confirm = false;
                if (applyToAllDecision.HasValue) { confirm = applyToAllDecision.Value; }
                else
                {
                    var actionName = actionType == CareActionType.Water ? "полив" : "удобрение";
                    var message = $"Растению '{plantVM.Name}' сегодня не требуется {actionName}. Вы действительно хотите отметить его?";
                    var result = _dialogService.ShowConfirmationDialog(message, true);
                    confirm = result.Confirmed;
                    if (result.ApplyToAll) { applyToAllDecision = result.Confirmed; }
                }
                if (confirm) { confirmedForUpdateVMs.Add(plantVM); }
            }

            if (confirmedForUpdateVMs.Any())
            {
                var plantIdsToUpdate = confirmedForUpdateVMs.Select(p => p.UserPlantId).ToList();
                var plantsToUpdate = await _unitOfWork.UserPlants.GetAllAsync(filter: p => plantIdsToUpdate.Contains(p.Id));
                Notification? notification = null;

                foreach (var plantVM in confirmedForUpdateVMs)
                {
                    var plantToUpdate = plantsToUpdate.FirstOrDefault(p => p.Id == plantVM.UserPlantId);
                    if (plantToUpdate != null)
                    {
                        if (actionType == CareActionType.Water)
                        {
                            plantToUpdate.LastUserWateringDate = DateTime.Today;
                        }
                        else
                        {
                            plantToUpdate.LastFertilizedDate = DateTime.Today;
                        }
                    }
                }

                var enableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
                if (enableSuccessNotifications)
                {
                    var icon = actionType == CareActionType.Water ? "💧" : "🌱";
                    var actionText = actionType == CareActionType.Water ? "полито" : "удобрено";
                    var successMessage = $"{icon} Успешно {actionText} {confirmedForUpdateVMs.Count} растений!";
                    notification = new Notification { Message = successMessage, Timestamp = DateTime.Now, Type = NotificationType.ActionSuccess, UserId = _authenticationService.CurrentUser.Id, IsDismissed = false };
                    await _unitOfWork.Notifications.AddAsync(notification);
                }
                
                await _unitOfWork.CompleteAsync();
                _unitOfWork.DetachAllEntities();
                
                if (notification != null) { _messenger.Send(new NewNotificationMessage(notification)); }
                _messenger.Send(new GardenStateChangedMessage());
            }
            CancelMassSelection();
        }

        [RelayCommand]
        private async Task DeletePlant(UserPlantViewModel? plantVM)
        {
            if (plantVM == null || _authenticationService.CurrentUser == null) return;

            var plantToDelete = await _unitOfWork.UserPlants.GetByIdAsync(plantVM.UserPlantId);
            if (plantToDelete != null)
            {
                var plantName = plantVM.Name;
                _unitOfWork.UserPlants.Delete(plantToDelete);
                
                Notification? notification = null;
                var enableSuccessNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableSuccessNotifications");
                if (enableSuccessNotifications)
                {
                    notification = new Notification { Message = $"Растение '{plantName}' успешно удалено!", Timestamp = DateTime.Now, Type = NotificationType.ActionSuccess, UserId = _authenticationService.CurrentUser.Id, IsDismissed = false };
                    await _unitOfWork.Notifications.AddAsync(notification);
                }
                
                await _unitOfWork.CompleteAsync();
                _unitOfWork.DetachAllEntities();

                if (notification != null) { _messenger.Send(new NewNotificationMessage(notification)); }
                _messenger.Send(new GardenStateChangedMessage());
            }
        }

        [RelayCommand]
        private async Task EditPlant(UserPlantViewModel? plantVM)
        {
            if (plantVM == null) return;
            var userPlantToEdit = await _unitOfWork.UserPlants.GetAllAsync(
                filter: up => up.Id == plantVM.UserPlantId,
                include: i => i.Include(up => up.Plant).ThenInclude(p => p.Sections),
                withTracking: false
            );
            _messenger.Send(new ShowAddUserPlantOverlayMessage(userPlantToEdit.FirstOrDefault()));
        }

        [RelayCommand]
        private async Task LoadUserPlants(bool generateNotifications = true)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                if (_authenticationService.CurrentUser == null)
                {
                    _allUserPlants.Clear();
                    PerformFilter(); 
                    return;
                }

                var userId = _authenticationService.CurrentUser.Id;
                var plants = await _unitOfWork.UserPlants.GetAllAsync(
                    filter: up => up.UserId == userId,
                    include: i => i.Include(up => up.Plant).ThenInclude(p => p.Variety).Include(up => up.Plant).ThenInclude(p => p.LightRequirement),
                    withTracking: false
                );

                _allUserPlants = plants.OrderBy(p => p.LastUserWateringDate)
                                       .Select(up => new UserPlantViewModel(up, _messenger))
                                       .ToList();
                PerformFilter();

                if (generateNotifications)
                {
                    await GenerateNeedsCareNotifications();
                }
            }
            finally
            {
                _isLoading = false;
            }
        }
        
        private void PerformFilter()
        {
            _plantsSelectedByFilterToggle.Clear();
            IsSelectAllFilteredActive = false;

            IEnumerable<UserPlantViewModel> filteredPlants = _allUserPlants;

            bool isFilterActive = !string.IsNullOrWhiteSpace(SearchText) || SelectedCategory != "Все";

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filteredPlants = filteredPlants.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
            
            if (SelectedCategory != "Все")
            {
                filteredPlants = filteredPlants.Where(p => p.Species == SelectedCategory || p.LightRequirement == SelectedCategory);
            }

            UserPlants.Clear();
            foreach (var plantVM in filteredPlants)
            {
                UserPlants.Add(plantVM);
            }
            
            ShowEmptyState = !_allUserPlants.Any();
            ShowEmptyFilterState = _allUserPlants.Any() && !UserPlants.Any() && isFilterActive;

            FilteredPlantCountSummary = GetPlantCountString(UserPlants.Count);
            UpdateTasksSummary();
        }

        public override string Title => $"Мой сад - {DateTime.Now.ToString("d MMMM", new System.Globalization.CultureInfo("ru-RU"))}";

        [ObservableProperty]
        private string _filteredPlantCountSummary = "";

        private string GetPlantCountString(int count)
        {
            if (count % 10 == 1 && count % 100 != 11)
            {
                return $"{count} растение";
            }
            if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20))
            {
                return $"{count} растения";
            }
            return $"{count} растений";
        }

        private async Task GenerateNeedsCareNotifications()
        {
            if (AppState.NeedsCareNotifiedToday) return;

            var enableCareNotifications = _configuration.GetValue<bool>("NotificationSettings:EnableCareNotifications");
            if (!enableCareNotifications) return;
            
            if (_authenticationService.CurrentUser == null) return;
            var userId = _authenticationService.CurrentUser.Id;
            var todayStart = DateTime.Today;
            var existingTodayNotifications = await _unitOfWork.Notifications.GetAllAsync(
                filter: n => n.UserId == userId && n.Type == NotificationType.NeedsCare && n.Timestamp >= todayStart,
                withTracking: false);

            if (existingTodayNotifications.Any())
            {
                AppState.NeedsCareNotifiedToday = true;
                return;
            }

            bool notificationAdded = false;
            int wateringCount = UserPlants.Count(p => p.DaysToNextWatering <= 0);
            if (wateringCount > 0)
            {
                var notification = new Notification
                {
                    Message = $"💧 Требуется полить {wateringCount} растений!",
                    Timestamp = DateTime.Now,
                    Type = NotificationType.NeedsCare,
                    UserId = userId,
                    IsDismissed = false
                };
                await _unitOfWork.Notifications.AddAsync(notification);
                _messenger.Send(new NewNotificationMessage(notification));
                notificationAdded = true;
            }

            int fertilizingCount = UserPlants.Count(p => p.DaysToNextFertilizing <= 0);
            if (fertilizingCount > 0)
            {
                var notification = new Notification
                {
                    Message = $"🌱 Требуется удобрить {fertilizingCount} растений!",
                    Timestamp = DateTime.Now,
                    Type = NotificationType.NeedsCare,
                    UserId = userId,
                    IsDismissed = false
                };
                await _unitOfWork.Notifications.AddAsync(notification);
                _messenger.Send(new NewNotificationMessage(notification));
                notificationAdded = true;
            }

            if (notificationAdded)
            {
                await _unitOfWork.CompleteAsync();
                _unitOfWork.DetachAllEntities();
                AppState.NeedsCareNotifiedToday = true;
            }
        }

        private void UpdateTasksSummary()
        {
            if (IsInMassSelectionMode)
            {
                TasksSummary = $"Выбрано растений: {SelectedPlantsCount}";
            }
            else
            {
                int plantsToCareToday = UserPlants.Count(p => p.DaysToNextWatering <= 0 || p.DaysToNextFertilizing <= 0);
                TasksSummary = $"Задачи на сегодня: {plantsToCareToday} растений ждут ухода";
            }
        }

        public void Receive(UserPlantSelectionChangedMessage message)
        {
            OnPropertyChanged(nameof(SelectedPlantsCount));
            OnPropertyChanged(nameof(IsAnyPlantSelected));
            DeleteSelectedPlantsCommand.NotifyCanExecuteChanged();
            UpdateTasksSummary();
            IsInMassSelectionMode = SelectedPlantsCount > 0;
        }

        public async void Receive(GardenStateChangedMessage message)
        {
            await LoadUserPlants(false);
        }
    }
}
