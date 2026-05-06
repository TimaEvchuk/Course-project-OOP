using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required]
        [MinLength(3)]
        private string _login = "";

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required]
        [EmailAddress]
        private string _email = "";

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,20}$", ErrorMessage = "Password must have upper/lower case, a number, and be 8-20 chars long.")]
        private string _password = "";

        private string _confirmPassword = "";

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value, true);
        }
        
        [ObservableProperty]
        private string _errorMessage = "";

        public RegisterViewModel(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
        }

        [RelayCommand]
        private async Task Register()
        {
            ValidateAllProperties();
            if (HasErrors)
            {
                ErrorMessage = string.Join("\n", GetErrors().Select(e => e.ErrorMessage));
                return;
            }
            ErrorMessage = "";

            var existingUserByLogin = (await _unitOfWork.Users.FindAsync(u => u.Login == Login)).FirstOrDefault();
            if (existingUserByLogin != null)
            {
                ErrorMessage = "This login is already taken.";
                return;
            }

            var existingUserByEmail = (await _unitOfWork.Users.FindAsync(u => u.Email == Email)).FirstOrDefault();
            if (existingUserByEmail != null)
            {
                ErrorMessage = "This email is already registered.";
                return;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

            var newUser = new User
            {
                Login = Login,
                Email = Email,
                PasswordHash = passwordHash,
                IsBlocked = false
            };

            var clientRole = (await _unitOfWork.Roles.FindAsync(r => r.Name == "Клиент")).FirstOrDefault();
            if (clientRole != null)
            {
                newUser.Roles.Add(clientRole);
            }
            // else: handle case where default role is not found, maybe log an error.

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.CompleteAsync();

            GoToLogin();
        }

        [RelayCommand]
        private void GoToLogin()
        {
            _messenger.Send(new NavigateMessage(typeof(LoginViewModel)));
        }
    }
}
