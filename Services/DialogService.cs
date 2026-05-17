using System.Windows;
using System.Windows.Controls;
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
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };
            
            dialog.ShowDialog();

            return new ConfirmationDialogResult
            {
                Confirmed = viewModel.DialogResult ?? false,
                ApplyToAll = viewModel.ApplyToAll
            };
        }

        public InputDialogResult ShowInputDialog(string title, string message, string defaultText = "")
        {
            var viewModel = new InputDialogViewModel(title, message, defaultText);
            var dialogView = new InputDialogView { DataContext = viewModel };

            var dialogWindow = new Window
            {
                Title = title,
                Content = new Border
                {
                    Background = (System.Windows.Media.Brush)Application.Current.FindResource("BrushMilkWhite"),
                    BorderBrush = (System.Windows.Media.Brush)Application.Current.FindResource("BrushDarkGreen"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Child = dialogView
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent
            };

            dialogWindow.ShowDialog();

            return viewModel.DialogResult;
        }
    }
}
