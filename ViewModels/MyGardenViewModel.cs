using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plantify.Data;
using Plantify.Models;
using Microsoft.EntityFrameworkCore;

namespace Plantify.ViewModels
{
    public partial class MyGardenViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;

        [ObservableProperty]
        private ObservableCollection<UserPlantViewModel> _userPlants = new();

        [ObservableProperty]
        private string _tasksSummary = "Задачи на сегодня: 0 растений ждут полива";

        [ObservableProperty]
        private bool _showEmptyState;

        public MyGardenViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            LoadUserPlantsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadUserPlants()
        {
            // Assuming a logged-in user with Id = 1 for now
            var userId = 1; 

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
