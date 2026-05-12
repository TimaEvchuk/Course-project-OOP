using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Plantify.ViewModels
{
    public partial class ConfirmationDialogViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _message;

        [ObservableProperty]
        private bool _applyToAll;

        public bool? DialogResult { get; private set; }

        public ConfirmationDialogViewModel(string message)
        {
            _message = message;
        }

        [RelayCommand]
        private void Yes(Window window)
        {
            DialogResult = true;
            window.DialogResult = true;
            window.Close();
        }

        [RelayCommand]
        private void No(Window window)
        {
            DialogResult = false;
            window.DialogResult = false;
            window.Close();
        }
    }
}
