using Plantify.Dialogs;

namespace Plantify.Services
{
    public interface IDialogService
    {
        ConfirmationDialogResult ShowConfirmationDialog(string message, bool showApplyToAll = false);
        InputDialogResult ShowInputDialog(string title, string message, string defaultText = "");
    }
}
