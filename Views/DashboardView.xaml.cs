using System.Windows.Controls;

namespace Plantify.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            WeekScrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (e.Handled)
                {
                    return;
                }

                WeekScrollViewer.ScrollToHorizontalOffset(WeekScrollViewer.HorizontalOffset - e.Delta);
                e.Handled = true;
            };
        }
    }
}
