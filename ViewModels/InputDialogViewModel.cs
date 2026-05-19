using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plantify.Dialogs;
using System.Windows;

namespace Plantify.ViewModels
{
    public partial class InputDialogViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _message;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OkCommand))]
        private string _inputText;

        public InputDialogResult DialogResult { get; private set; }

        public InputDialogViewModel(string title, string message, string defaultText = "")
        {
            _title = title;
            _message = message;
            _inputText = defaultText;
            DialogResult = new InputDialogResult();
        }

        private bool CanConfirm()
        {
            return !string.IsNullOrWhiteSpace(InputText) && InputText.Trim().Length >= 2;
        }

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void Ok(Window window)
        {
            DialogResult.Confirmed = true;
            DialogResult.Text = InputText;
            window?.Close();
        }

        [RelayCommand]
        private void Cancel(Window window)
        {
            DialogResult.Confirmed = false;
            window?.Close();
        }
    }
}
