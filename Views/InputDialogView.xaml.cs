using System.Windows;
using System.Windows.Controls;
using Plantify.ViewModels;

namespace Plantify.Views
{
    public partial class InputDialogView : UserControl
    {
        public InputDialogView()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as InputDialogViewModel;
            if (vm != null)
            {
                vm.DialogResult.Confirmed = true;
                vm.DialogResult.Text = vm.InputText;
            }
            CloseDialog();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as InputDialogViewModel;
            if (vm != null)
            {
                vm.DialogResult.Confirmed = false;
            }
            CloseDialog();
        }

        private void CloseDialog()
        {
            var window = Window.GetWindow(this);
            window?.Close();
        }
    }
}
