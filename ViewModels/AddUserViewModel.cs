using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plantify.Data;
using Plantify.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;

namespace Plantify.ViewModels
{
    public partial class AddUserViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Логин обязателен")]
        [MinLength(3, ErrorMessage = "Логин должен содержать минимум 3 символа")]
        private string _login = "";

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        private string _email = "";

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Пароль обязателен")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,20}$", ErrorMessage = "Пароль должен содержать заглавную и строчную буквы, цифру, и быть длиной 8-20 символов.")]
        private string _password = "";

        private string _confirmPassword = "";

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают.")]
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value, true);
        }
        
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Необходимо выбрать роль")]
        private Role? _selectedRole;
        
        public ObservableCollection<Role> AllRoles { get; } = new();

        public AddUserViewModel(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _ = LoadRoles();
        }

        private async Task LoadRoles()
        {
            var rolesFromDb = await _unitOfWork.Roles.GetAllAsync();
            foreach (var role in rolesFromDb.OrderBy(r => r.Id))
            {
                AllRoles.Add(role);
            }
        }

        [RelayCommand]
        private async Task AddUser()
        {
            ValidateAllProperties();
            if (HasErrors)
            {
                return;
            }

            if ((await _unitOfWork.Users.FindAsync(u => u.Login == Login)).Any())
            {
                // TODO: Show this server-side error in the UI. 
                // For now, just prevent creation.
                return;
            }

            var newUser = new User
            {
                Login = Login,
                Email = Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                IsBlocked = false
            };
            newUser.Roles.Add(SelectedRole!);

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            _messenger.Send(new CloseOverlayMessage());
            _messenger.Send(new AdminUserListChangedMessage());
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }
    }
}
