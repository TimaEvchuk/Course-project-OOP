using Plantify.Dialogs;
using Plantify.ViewModels;
using Plantify.Views;

namespace Plantify.Services
{
    public class DialogService : IDialogService
    {
        public ConfirmationDialogResult ShowConfirmationDialog(string message, bool showApplyToAll = false)
        {
            var viewModel = new ConfirmationDialogViewModel(message, showApplyToAll);
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
