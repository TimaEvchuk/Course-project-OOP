using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plantify.Data;
using Plantify.Dialogs;
using Plantify.Models;
using Plantify.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Plantify.Messages;

namespace Plantify.ViewModels
{
    public partial class AdminPanelViewModel : BaseViewModel, IRecipient<AdminUserListChangedMessage>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuthenticationService _authenticationService;
        private readonly IDialogService _dialogService;
        private readonly IMessenger _messenger;
        private List<User> _allUsers = new();

        // Tab Management
        [ObservableProperty]
        private bool _isUsersViewSelected = true;

        [ObservableProperty]
        private bool _isStatisticsViewSelected = false;

        // For dirty checking premium status
        private bool _originalIsPremium;
        private DateTime? _originalPremiumEndDate;

        [ObservableProperty]
        private string _searchText = "";

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    OnPropertyChanged(nameof(IsUserSelected));
                    OnPropertyChanged(nameof(CanModifySelectedUser));
                    OnPropertyChanged(nameof(BlockButtonText));
                    OnPropertyChanged(nameof(CanChangeRole));

                    if (SelectedUser != null)
                    {
                        IsPremiumSelectedUser = SelectedUser.IsPremium;
                        PremiumStartDateSelectedUser = SelectedUser.PremiumStartDate;
                        PremiumEndDateSelectedUser = SelectedUser.PremiumEndDate;
                        _originalIsPremium = SelectedUser.IsPremium;
                        _originalPremiumEndDate = SelectedUser.PremiumEndDate;
                    }

                    OnPropertyChanged(nameof(CanSaveChanges));
                    BlockUserCommand.NotifyCanExecuteChanged();
                    ChangeUserRoleCommand.NotifyCanExecuteChanged();
                    DeleteUserCommand.NotifyCanExecuteChanged();
                    SavePremiumCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private Role? _selectedRoleForChange;
        public Role? SelectedRoleForChange
        {
            get => _selectedRoleForChange;
            set
            {
                if (SetProperty(ref _selectedRoleForChange, value))
                {
                    OnPropertyChanged(nameof(CanChangeRole));
                    ChangeUserRoleCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private bool _isPremiumSelectedUser;
        public bool IsPremiumSelectedUser
        {
            get => _isPremiumSelectedUser;
            set
            {
                if (SetProperty(ref _isPremiumSelectedUser, value))
                {
                    if (value == true && _originalIsPremium == false)
                    {
                        PremiumStartDateSelectedUser = DateTime.Today;
                        PremiumEndDateSelectedUser = DateTime.Today.AddDays(31);
                    }
                    
                    OnPropertyChanged(nameof(CanSaveChanges));
                    SavePremiumCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private DateTime? _premiumStartDateSelectedUser;
        public DateTime? PremiumStartDateSelectedUser
        {
            get => _premiumStartDateSelectedUser;
            set
            {
                if (SetProperty(ref _premiumStartDateSelectedUser, value))
                {
                    OnPropertyChanged(nameof(CanSaveChanges));
                    SavePremiumCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private DateTime? _premiumEndDateSelectedUser;
        public DateTime? PremiumEndDateSelectedUser
        {
            get => _premiumEndDateSelectedUser;
            set
            {
                if (SetProperty(ref _premiumEndDateSelectedUser, value))
                {
                    OnPropertyChanged(nameof(CanSaveChanges));
                    SavePremiumCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsUserSelected => SelectedUser != null;
        public bool CanChangeRole => SelectedUser != null && SelectedRoleForChange != null;
        public bool CanModifySelectedUser => SelectedUser != null && SelectedUser.Id != _authenticationService.CurrentUser?.Id;
        public bool CanSaveChanges => SelectedUser != null && (IsPremiumSelectedUser != _originalIsPremium || PremiumEndDateSelectedUser != _originalPremiumEndDate);
        public string BlockButtonText => SelectedUser?.IsBlocked == true ? "Разблокировать" : "Заблокировать";
        public DateTime Today => DateTime.Today;

        public ObservableCollection<User> Users { get; } = new();
        public ObservableCollection<Role> AllRoles { get; } = new();
        
        public StatisticsViewModel StatisticsVM { get; }

        public AdminPanelViewModel(IUnitOfWork unitOfWork, AuthenticationService authenticationService, IDialogService dialogService, IMessenger messenger, StatisticsViewModel statisticsViewModel)
        {
            _unitOfWork = unitOfWork;
            _authenticationService = authenticationService;
            _dialogService = dialogService;
            _messenger = messenger;
            StatisticsVM = statisticsViewModel;
            StatisticsVM.ParentVM = this;
            _messenger.Register<AdminUserListChangedMessage>(this);
            _ = Initialize();
        }

        public void Receive(AdminUserListChangedMessage message)
        {
            _ = LoadUsers();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterUsers();
        }

        private void FilterUsers()
        {
            Users.Clear();
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allUsers
                : _allUsers.Where(u => u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            
            foreach (var user in filtered)
            {
                Users.Add(user);
            }
        }

        private async Task Initialize()
        {
            await LoadRoles();
            await LoadUsers();
            await StatisticsVM.LoadDataAsync();
        }
        
        [RelayCommand]
        private void GoToUsersView()
        {
            IsUsersViewSelected = true;
            IsStatisticsViewSelected = false;
        }
        
        [RelayCommand]
        private void GoToStatisticsView()
        {
            IsUsersViewSelected = false;
            IsStatisticsViewSelected = true;
        }

        [RelayCommand]
        private async Task LoadUsers()
        {
            var usersFromDb = await _unitOfWork.Users.GetAllAsync(
                include: u => u.Include(user => user.Roles)
            );
            
            _allUsers = usersFromDb.OrderBy(u => u.Id).ToList();
            FilterUsers();
        }

        [RelayCommand]
        private async Task LoadRoles()
        {
            AllRoles.Clear();
            var rolesFromDb = await _unitOfWork.Roles.GetAllAsync();
            foreach (var role in rolesFromDb.OrderBy(r => r.Id))
            {
                AllRoles.Add(role);
            }
        }

        [RelayCommand]
        private void ShowAddUserOverlay()
        {
            _messenger.Send(new ShowAddUserAdminOverlayMessage());
        }

        [RelayCommand(CanExecute = nameof(CanChangeRole))]
        private async Task ChangeUserRole()
        {
            if (SelectedUser is null || SelectedRoleForChange is null) return;

            var userToUpdate = (await _unitOfWork.Users.GetAllAsync(
                filter: u => u.Id == SelectedUser.Id,
                include: q => q.Include(u => u.Roles)
            )).FirstOrDefault();

            if (userToUpdate is null) return;

            if (userToUpdate.Roles.Any(r => r.Id == SelectedRoleForChange.Id))
            {
                return; 
            }

            userToUpdate.Roles.Clear();
            userToUpdate.Roles.Add(SelectedRoleForChange);
            await _unitOfWork.CompleteAsync();

            await LoadUsers();
        }

        [RelayCommand(CanExecute = nameof(CanModifySelectedUser))]
        private async Task BlockUser()
        {
            if (SelectedUser is null) return;

            var userToUpdate = await _unitOfWork.Users.GetByIdAsync(SelectedUser.Id);
            if (userToUpdate is null) return;

            userToUpdate.IsBlocked = !userToUpdate.IsBlocked;

            _unitOfWork.Users.Update(userToUpdate);
            await _unitOfWork.CompleteAsync();
            
            await LoadUsers();
        }

        [RelayCommand(CanExecute = nameof(CanModifySelectedUser))]
        private async Task DeleteUser()
        {
            if (SelectedUser is null) return;

            var result = _dialogService.ShowConfirmationDialog(
                $"Вы уверены, что хотите безвозвратно удалить пользователя '{SelectedUser.Login}'?");

            if (result.Confirmed)
            {
                var userToDelete = await _unitOfWork.Users.GetByIdAsync(SelectedUser.Id);
                if (userToDelete != null)
                {
                    _unitOfWork.Users.Delete(userToDelete);
                    await _unitOfWork.CompleteAsync();
                    await LoadUsers();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanSaveChanges))]
        private async Task SavePremium()
        {
            if (SelectedUser is null) return;

            var userToUpdate = await _unitOfWork.Users.GetByIdAsync(SelectedUser.Id);
            if (userToUpdate is null) return;

            userToUpdate.IsPremium = IsPremiumSelectedUser;

            if (IsPremiumSelectedUser)
            {
                userToUpdate.PremiumStartDate = PremiumStartDateSelectedUser ?? DateTime.Today;
                userToUpdate.PremiumEndDate = PremiumEndDateSelectedUser ?? DateTime.Today.AddMonths(1);
            }
            else
            {
                userToUpdate.PremiumStartDate = null;
                userToUpdate.PremiumEndDate = null;
            }
            
            _unitOfWork.Users.Update(userToUpdate);
            await _unitOfWork.CompleteAsync();

            var selectedUserId = SelectedUser.Id;
            await LoadUsers();
            SelectedUser = Users.FirstOrDefault(u => u.Id == selectedUserId);
        }
    }
}
