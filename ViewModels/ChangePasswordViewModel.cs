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
            var detachedCurrentUser = _authenticationService.CurrentUser;
            if (detachedCurrentUser == null)
            {
                ErrorMessage = "Ошибка: пользователь не авторизован.";
                return;
            }

            // It's better to re-fetch the user to get the most current password hash for verification
            var userToUpdate = await _unitOfWork.Users.GetByIdAsync(detachedCurrentUser.Id);
            if (userToUpdate == null)
            {
                ErrorMessage = "Ошибка: не удалось найти пользователя в базе данных.";
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, userToUpdate.PasswordHash))
            {
                ErrorMessage = "Текущий пароль введен неверно.";
                return;
            }

            userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            
            // No need to call UpdateAsync, just complete the unit of work
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
