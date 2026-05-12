using Plantify.Dialogs;
using Plantify.ViewModels;
using Plantify.Views;

namespace Plantify.Services
{
    public class DialogService : IDialogService
    {
        public ConfirmationDialogResult ShowConfirmationDialog(string message)
        {
            var viewModel = new ConfirmationDialogViewModel(message);
            var dialog = new ConfirmationDialogView
            {
                DataContext = viewModel
            };

            dialog.ShowDialog();

            return new ConfirmationDialogResult
            {
                Confirmed = viewModel.DialogResult ?? false,
                ApplyToAll = viewModel.ApplyToAll
            };
        }
    }
}
