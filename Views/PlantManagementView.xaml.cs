using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Plantify.Views
{
    /// <summary>
    /// Interaction logic for PlantManagementView.xaml
    /// </summary>
    public partial class PlantManagementView : UserControl
    {
        public PlantManagementView()
        {
            InitializeComponent();
        }

        private void ImageDropTarget_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                (sender as Border)!.BorderBrush = Brushes.Blue; // Visual feedback
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ImageDropTarget_DragLeave(object sender, DragEventArgs e)
        {
            (sender as Border)!.BorderBrush = Brushes.Green; // Restore original color
            e.Handled = true;
        }

        private void ImageDropTarget_Drop(object sender, DragEventArgs e)
        {
            (sender as Border)!.BorderBrush = Brushes.Green; // Restore original color
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null || files.Length == 0) return;
                string? filePath = files.FirstOrDefault();

                if (!string.IsNullOrEmpty(filePath) && DataContext is ViewModels.PlantManagementViewModel viewModel)
                {
                    viewModel.ProcessImageFile(filePath);
                }
            }
            e.Handled = true;
        }
    }
}
