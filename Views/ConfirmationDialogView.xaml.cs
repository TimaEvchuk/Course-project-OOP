using System.Windows;
using System.Windows.Input;

namespace Plantify.Views
{
    public partial class ConfirmationDialogView : Window
    {
        public ConfirmationDialogView()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
