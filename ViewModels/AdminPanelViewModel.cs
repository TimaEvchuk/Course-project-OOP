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
using Microsoft.EntityFrameworkCore;

namespace Plantify.ViewModels
{
    public partial class AdminPanelViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuthenticationService _authenticationService;
        private readonly IDialogService _dialogService;
        private List<User> _allUsers = new();

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

                    BlockUserCommand.NotifyCanExecuteChanged();
                    ChangeUserRoleCommand.NotifyCanExecuteChanged();
                    DeleteUserCommand.NotifyCanExecuteChanged();
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

        public bool IsUserSelected => SelectedUser != null;
        public bool CanChangeRole => SelectedUser != null && SelectedRoleForChange != null;
        public bool CanModifySelectedUser => SelectedUser != null && SelectedUser.Id != _authenticationService.CurrentUser?.Id;
        public string BlockButtonText => SelectedUser?.IsBlocked == true ? "Разблокировать" : "Заблокировать";

        public ObservableCollection<User> Users { get; } = new();
        public ObservableCollection<Role> AllRoles { get; } = new();

        public AdminPanelViewModel(IUnitOfWork unitOfWork, AuthenticationService authenticationService, IDialogService dialogService)
        {
            _unitOfWork = unitOfWork;
            _authenticationService = authenticationService;
            _dialogService = dialogService;
            _ = Initialize();
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
                "Удаление пользователя",
                $"Вы уверены, что хотите безвозвратно удалить пользователя '{SelectedUser.Login}'?");

            if (result == ConfirmationDialogResult.Yes)
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
    }
}
