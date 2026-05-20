using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;

        [ObservableProperty]
        private string _login = "";
        [ObservableProperty]
        private string _email = "";
        
        private string _password = "";
        private string _confirmPassword = "";

        [ObservableProperty]
        private string? _errorMessage;

        public RegisterViewModel(IMessenger messenger, AuthenticationService authenticationService, IUnitOfWork unitOfWork)
        {
            _messenger = messenger;
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;

            // Trigger validation on property changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(ErrorMessage))
                {
                    Validate();
                }
            };
        }

        // These methods will be called from the View's code-behind
        public void SetPassword(string password)
        {
            _password = password;
            Validate();
        }
        public void SetConfirmPassword(string confirmPassword)
        {
            _confirmPassword = confirmPassword;
            Validate();
        }

        private void Validate()
        {
            var errors = new List<string>();

            // Email Validation
            if (!string.IsNullOrWhiteSpace(Email))
            {
                try
                {
                    var mail = new System.Net.Mail.MailAddress(Email);
                }
                catch
                {
                    errors.Add("Неверный email.");
                }
            }

            // Password Validation
            if (!string.IsNullOrWhiteSpace(_password))
            {
                var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,20}$");
                if (!passwordRegex.IsMatch(_password))
                {
                    errors.Add("Пароль должен содержать от 8 до 20 символов, включая заглавные, строчные буквы и цифры.");
                }
            }

            // Confirm Password Validation
            if (!string.IsNullOrWhiteSpace(_password) && !string.IsNullOrWhiteSpace(_confirmPassword))
            {
                if (_password != _confirmPassword)
                {
                    errors.Add("Пароли не совпадают.");
                }
            }

            ErrorMessage = errors.Any() ? string.Join("\n", errors) : null;
            RegisterCommand.NotifyCanExecuteChanged();
        }
        
        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(Login) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(_password) &&
                   !string.IsNullOrWhiteSpace(_confirmPassword) &&
                   string.IsNullOrEmpty(ErrorMessage);
        }

        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task Register()
        {
            // Final check for uniqueness before hitting the service
            if ((await _unitOfWork.Users.FindAsync(u => u.Login == Login)).Any())
            {
                ErrorMessage = "Этот логин уже занят.";
                return;
            }
            if ((await _unitOfWork.Users.FindAsync(u => u.Email == Email)).Any())
            {
                ErrorMessage = "Этот email уже зарегистрирован.";
                return;
            }
            
            bool success = await _authenticationService.Register(Login, Email, _password);

            if (!success)
            {
                ErrorMessage = "Произошла ошибка при регистрации.";
                return;
            }
            
            _messenger.Send(new UserLoggedInMessage(_authenticationService.CurrentUser!));
        }

        [RelayCommand]
        private void GoToLogin()
        {
            _messenger.Send(new NavigateMessage(typeof(LoginViewModel)));
        }
    }
}
