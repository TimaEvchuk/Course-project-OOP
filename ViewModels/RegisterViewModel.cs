using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Plantify.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;

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

        public RegisterViewModel(IMessenger messenger, AuthenticationService authenticationService)
        {
            _messenger = messenger;
            _authenticationService = authenticationService;
        }

        [RelayCommand]
        private async Task Register()
        {
            // Basic client-side check
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || Password != ConfirmPassword)
            {
                ErrorMessage = "Неверный email или пароль";
                return;
            }
            
            ErrorMessage = "";

            bool success = await _authenticationService.Register(Login, Email, Password);

            if (!success)
            {
                ErrorMessage = "Неверный email или пароль";
                return;
            }
            
            // Successful registration and auto-login
            _messenger.Send(new UserLoggedInMessage(_authenticationService.CurrentUser!));
        }

        [RelayCommand]
        private void GoToLogin()
        {
            _messenger.Send(new NavigateMessage(typeof(LoginViewModel)));
        }
    }
}
