using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Data;
using Plantify.Messages;
using Plantify.Services;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; // Add this line
using BCrypt.Net;

namespace Plantify.ViewModels
{
    public partial class ChangePasswordViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AuthenticationService _authenticationService;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
        private string? _currentPassword;
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }

        [ObservableProperty]
        private string? _errorMessage;

        public ChangePasswordViewModel(IUnitOfWork unitOfWork, AuthenticationService authenticationService, IMessenger messenger)
        {
            _unitOfWork = unitOfWork;
            _authenticationService = authenticationService;
            _messenger = messenger;
        }

        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmNewPassword) &&
                   NewPassword.Length >= 6 &&
                   NewPassword == ConfirmNewPassword;
        }

        [RelayCommand(CanExecute = nameof(CanChangePassword))]
        private async Task ChangePassword()
        {
            if (_authenticationService.CurrentUser == null)
            {
                ErrorMessage = "Ошибка: пользователь не авторизован.";
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, _authenticationService.CurrentUser.PasswordHash))
            {
                ErrorMessage = "Текущий пароль введен неверно.";
                return;
            }

            _authenticationService.CurrentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _unitOfWork.Users.UpdateAsync(_authenticationService.CurrentUser);
            await _unitOfWork.CompleteAsync();

            ErrorMessage = null;
            MessageBox.Show("Пароль успешно изменен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            _messenger.Send(new CloseOverlayMessage());
        }

        [RelayCommand]
        private void Cancel()
        {
            _messenger.Send(new CloseOverlayMessage());
        }
    }
}
