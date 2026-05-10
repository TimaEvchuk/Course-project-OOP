using Plantify.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Plantify.ViewModels
{
    public class PlantViewModel : BaseViewModel
    {
        public Plant PlantModel { get; }
        public BitmapImage? DisplayImageSource { get; }

        public string Name => PlantModel.Name;
        public string Variety => PlantModel.Variety;
        public int WateringInterval => PlantModel.WateringInterval;
        public int FertilizingInterval => PlantModel.FertilizingInterval;
        public List<PlantSection> Sections => PlantModel.Sections;
        public string ShortDescription => PlantModel.Sections.FirstOrDefault()?.Content ?? "";
        public string ImagePath => PlantModel.ImagePath ?? "";

        public PlantViewModel(Plant plant)
        {
            PlantModel = plant;
            DisplayImageSource = LoadImage(plant.ImagePath);
        }

        private BitmapImage? LoadImage(string? imagePath)
        {
            string? imageToLoad = null;

            if (!string.IsNullOrEmpty(imagePath))
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(basePath, imagePath);

                if (File.Exists(fullPath))
                {
                    imageToLoad = fullPath;
                }
            }

            // If no specific image is found, use the placeholder
            if (imageToLoad == null)
            {
                // Use Pack URI to load the embedded resource
                imageToLoad = "pack://application:,,,/Images/placeholder.png";
            }
            
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageToLoad, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception)
            {
                // If even the placeholder fails, return null
                return null;
            }
        }
    }
}
