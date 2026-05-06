using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
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

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        private string _email;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Password is required.")]
        private string _password;

        [ObservableProperty]
        private string _errorMessage;

        public LoginViewModel(IUnitOfWork unitOfWork, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _messenger = messenger;
            _email = string.Empty;
            _password = string.Empty;
            _errorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task Login()
        {
            ValidateAllProperties();
            if (HasErrors)
            {
                ErrorMessage = string.Join("\n", GetErrors().Select(e => e.ErrorMessage));
                return;
            }
            ErrorMessage = string.Empty;

            var users = await _unitOfWork.Users.FindAsync(u => u.Email == Email);
            var user = users.FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                ErrorMessage = "Invalid email or password.";
                return;
            }

            if (user.IsBlocked)
            {
                ErrorMessage = "This account has been blocked.";
                return;
            }

            // Successful login: Navigate to the dashboard.
            _messenger.Send(new NavigateMessage(typeof(DashboardViewModel)));
        }

        [RelayCommand]
        private void GoToRegister()
        {
            _messenger.Send(new NavigateMessage(typeof(RegisterViewModel)));
        }
    }
}
