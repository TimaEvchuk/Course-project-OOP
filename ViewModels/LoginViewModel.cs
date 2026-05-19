using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Plantify.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessenger _messenger;
        private readonly AuthenticationService _authenticationService;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Login is required.")]
        private string _login = "";

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Password is required.")]
        private string _password = "";

        [ObservableProperty]
        private string _errorMessage = "";

        public LoginViewModel(IUnitOfWork unitOfWork, IMessenger messenger, AuthenticationService authenticationService)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _authenticationService = authenticationService;
        }

        [RelayCommand]
        private async Task SignIn()
        {
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Неверный пароль или логин";
                return;
            }
            
            ErrorMessage = "";

            bool success = await _authenticationService.SignIn(Login, Password);

            if (!success)
            {
                ErrorMessage = "Неверный пароль или логин";
                return;
            }
            
            if (_authenticationService.CurrentUser != null && _authenticationService.CurrentUser.IsBlocked)
            {
                ErrorMessage = "Данный аккаунт заблокирован.";
                _authenticationService.Logout(); // Sign out the blocked user
                return;
            }

            // Successful login: Send a message to update the main view
            _messenger.Send(new UserLoggedInMessage(_authenticationService.CurrentUser!));
        }

        [RelayCommand]
        private void GoToRegister()
        {
            _messenger.Send(new NavigateMessage(typeof(RegisterViewModel)));
        }
    }
}
