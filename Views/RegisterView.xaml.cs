using System.Windows.Controls;
using Plantify.ViewModels; // Add this using statement

namespace Plantify.Views
{
    /// <summary>
    /// Interaction logic for RegisterView.xaml
    /// </summary>
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is RegisterViewModel viewModel)
            {
                viewModel.SetPassword(((PasswordBox)sender).Password);
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is RegisterViewModel viewModel)
            {
                viewModel.SetConfirmPassword(((PasswordBox)sender).Password);
            }
        }
    }
}
