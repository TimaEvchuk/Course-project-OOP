using Plantify.Dialogs;

namespace Plantify.Services
{
    public interface IDialogService
    {
        ConfirmationDialogResult ShowConfirmationDialog(string message);
    }
}
