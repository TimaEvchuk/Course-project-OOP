using System.Windows;
using System.Windows.Controls;
using Plantify.ViewModels;

namespace Plantify.Views
{
    public partial class ChangePasswordView : UserControl
    {
        public ChangePasswordView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordViewModel viewModel)
            {
                if (sender is PasswordBox box)
                {
                    if (box.Name == "NewPasswordBox")
                        viewModel.NewPassword = box.Password;
                    else if (box.Name == "ConfirmNewPasswordBox")
                        viewModel.ConfirmNewPassword = box.Password;

                    // Manually trigger CanExecute re-evaluation
                    viewModel.ChangePasswordCommand.NotifyCanExecuteChanged();
                }
            }
        }
    }
}
