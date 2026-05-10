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
    public partial class MyGardenViewModel : BaseViewModel
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

        public MyGardenViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;
            LoadUserPlantsCommand.Execute(null);
        }

        [RelayCommand]
        private void AddPlant()
        {
            _messenger.Send(new ShowAddUserPlantOverlayMessage((Plant?)null));
        }

        [RelayCommand]
        private async Task DeletePlant(UserPlantViewModel? plantVM)
        {
            if (plantVM == null) return;

            // Find the entity in the database to delete it.
            // We need to fetch the original entity to remove it from the context.
            var plantToDelete = await _unitOfWork.UserPlants.GetByIdAsync(plantVM.UserPlantId);
            if (plantToDelete != null)
            {
                _unitOfWork.UserPlants.Delete(plantToDelete);
                await _unitOfWork.CompleteAsync();

                // Remove from the collection to update UI
                UserPlants.Remove(plantVM);
                UpdateTasksSummary();
            }
        }

        [RelayCommand]
        private async Task EditPlant(UserPlantViewModel? plantVM)
        {
            if (plantVM == null) return;
            
            // Re-fetch the full UserPlant entity to ensure all navigation properties are loaded
            // This is important because the plantVM might not have the full Plant object loaded
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
                UserPlants.Add(new UserPlantViewModel(userPlant));
            }

            UpdateTasksSummary();
        }

        private void UpdateTasksSummary()
        {
            int plantsToWater = UserPlants.Count(p => p.NextWateringDue == "Сегодня");
            TasksSummary = $"Задачи на сегодня: {plantsToWater} растений ждут полива";
            ShowEmptyState = !UserPlants.Any();
        }
    }
}
