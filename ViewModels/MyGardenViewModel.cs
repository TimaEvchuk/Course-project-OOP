using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using Plantify.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class MyGardenViewModel : BaseViewModel, IRecipient<UserPlantSelectionChangedMessage>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;

        [ObservableProperty]
        private ObservableCollection<UserPlantViewModel> _userPlants = new();

        [ObservableProperty]
        private string _tasksSummary = "Задачи на сегодня: 0 растений ждут полива";

        [ObservableProperty]
        private bool _showEmptyState;

        [ObservableProperty]
        private bool _isInMassSelectionMode;

        public MyGardenViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;
            _messenger.Register(this);
            LoadUserPlantsCommand.Execute(null);
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
            UpdateTasksSummary();
        }

        [RelayCommand]
        private async Task ConfirmCareAction()
        {
            var selectedPlantVMs = UserPlants.Where(p => p.IsTaskCompletedToday).ToList();
            if (!selectedPlantVMs.Any()) return;

            var notDuePlants = selectedPlantVMs
                .Where(p => p.DaysToNextWatering > 0 && p.DaysToNextFertilizing > 0)
                .ToList();

            if (notDuePlants.Any())
            {
                var plantNames = string.Join(", ", notDuePlants.Select(p => p.Name).Take(3));
                if (notDuePlants.Count > 3) plantNames += ", ...";
                
                var result = System.Windows.MessageBox.Show(
                    $"Растениям ({plantNames}) сегодня не требуется уход. Вы действительно хотите отметить их?",
                    "Предупреждение",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.No)
                {
                    return; // User cancelled
                }
            }

            var plantIdsToUpdate = selectedPlantVMs.Select(p => p.UserPlantId).ToList();
            var plantsToUpdate = await _unitOfWork.UserPlants.GetAllAsync(filter: p => plantIdsToUpdate.Contains(p.Id));

            foreach (var plantVM in selectedPlantVMs)
            {
                var plantToUpdate = plantsToUpdate.FirstOrDefault(p => p.Id == plantVM.UserPlantId);
                if (plantToUpdate != null)
                {
                    // Update if the task was due OR if the user confirmed the early action
                    if (plantVM.DaysToNextWatering <= 0 || notDuePlants.Contains(plantVM))
                    {
                        plantToUpdate.LastUserWateringDate = DateTime.Today;
                    }
                    if (plantVM.DaysToNextFertilizing <= 0 || notDuePlants.Contains(plantVM))
                    {
                        plantToUpdate.LastFertilizedDate = DateTime.Today;
                    }
                    _unitOfWork.UserPlants.Update(plantToUpdate);
                }
            }

            await _unitOfWork.CompleteAsync();

            // Reset UI
            CancelMassSelection();
            
            // Reload plants to show updated dates
            await LoadUserPlants();
        }

        [RelayCommand]
        private async Task DeletePlant(UserPlantViewModel? plantVM)
        {
            if (plantVM == null) return;

            var plantToDelete = await _unitOfWork.UserPlants.GetByIdAsync(plantVM.UserPlantId);
            if (plantToDelete != null)
            {
                _unitOfWork.UserPlants.Delete(plantToDelete);
                await _unitOfWork.CompleteAsync();

                UserPlants.Remove(plantVM);
                UpdateTasksSummary();
            }
        }

        [RelayCommand]
        private async Task EditPlant(UserPlantViewModel? plantVM)
        {
            if (plantVM == null) return;
            
            var userPlantToEdit = await _unitOfWork.UserPlants.GetAllAsync(
                filter: up => up.Id == plantVM.UserPlantId,
                include: i => i.Include(up => up.Plant).ThenInclude(p => p.Sections)
            );
            
            _messenger.Send(new ShowAddUserPlantOverlayMessage(userPlantToEdit.FirstOrDefault()));
        }

        [RelayCommand]
        private async Task LoadUserPlants()
        {
            if (_authenticationService.CurrentUser == null)
            {
                ShowEmptyState = true;
                UserPlants.Clear();
                UpdateTasksSummary();
                return;
            }
            
            var userId = _authenticationService.CurrentUser.Id; 

            var plants = await _unitOfWork.UserPlants.GetAllAsync(
                filter: up => up.UserId == userId,
                include: i => i.Include(up => up.Plant).ThenInclude(p => p.Sections)
            );

            UserPlants.Clear();
            foreach (var userPlant in plants.OrderBy(p => p.LastUserWateringDate))
            {
                UserPlants.Add(new UserPlantViewModel(userPlant, _messenger)); // Pass messenger
            }

            UpdateTasksSummary();
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
            ShowEmptyState = !UserPlants.Any();
        }

        public void Receive(UserPlantSelectionChangedMessage message)
        {
            // Recalculate properties that depend on selection
            OnPropertyChanged(nameof(SelectedPlantsCount));
            OnPropertyChanged(nameof(IsAnyPlantSelected));
            UpdateTasksSummary();
            IsInMassSelectionMode = SelectedPlantsCount > 0;
        }
    }
}
