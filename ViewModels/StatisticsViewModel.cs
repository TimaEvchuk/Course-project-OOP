using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plantify.Data;
using Plantify.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Plantify.ViewModels
{
    public record UserStatItem(int Rank, string UserName, int PlantCount);

    public partial class StatisticsViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;

        [ObservableProperty]
        private int _totalUsersCount;
        
        [ObservableProperty]
        private int _premiumUsersCount;
        
        [ObservableProperty]
        private int _totalPlantsInGardensCount;

        [ObservableProperty]
        private int _blockedUsersCount;
        
        public AdminPanelViewModel? ParentVM { get; set; }
        
        public ObservableCollection<UserStatItem> TopUsers { get; } = new();

        public StatisticsViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            // Data will be loaded by the parent AdminPanelViewModel
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            TotalUsersCount = await _unitOfWork.Users.CountAsync();
            PremiumUsersCount = await _unitOfWork.Users.CountAsync(u => u.IsPremium);
            BlockedUsersCount = await _unitOfWork.Users.CountAsync(u => u.IsBlocked);
            TotalPlantsInGardensCount = await _unitOfWork.UserPlants.CountAsync();

            var allUsers = await _unitOfWork.Users.GetAllAsync(include: u => u.Include(u => u.UserPlants));
            var top5 = allUsers
                .OrderByDescending(u => u.UserPlants.Count)
                .Take(5)
                .Select((u, index) => new UserStatItem(index + 1, u.Login, u.UserPlants.Count));
            
            TopUsers.Clear();
            foreach (var userStat in top5)
            {
                TopUsers.Add(userStat);
            }
        }
    }
}
