using Plantify.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Plantify.Views
{
    public partial class AddUserPlantView : UserControl
    {
        public AddUserPlantView()
        {
            InitializeComponent();
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any())
            {
                var viewModel = DataContext as AddUserPlantViewModel;
                if (viewModel != null)
                {
                    viewModel.ProcessImageFile(files[0]);
                }
            }
        }
    }
}
