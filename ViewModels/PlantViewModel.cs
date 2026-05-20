using Plantify.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Plantify.ViewModels
{
    public class PlantViewModel : BaseViewModel
    {
        public Plant PlantModel { get; }
        public BitmapImage? DisplayImageSource { get; }

        public string Name => PlantModel.Name;
        public string Variety => PlantModel.Variety.Name;
        public string LightRequirement => PlantModel.LightRequirement.Name;
        public int WateringInterval => PlantModel.WateringInterval;
        public int FertilizingInterval => PlantModel.FertilizingInterval;
        public ICollection<PlantSection> Sections { get; }
        public string ShortDescription => Sections.FirstOrDefault()?.Content ?? "";
        public string ImagePath => PlantModel.ImagePath ?? "";

        public PlantViewModel(Plant plant)
        {
            PlantModel = plant;

            // Ensure sections are distinct before assigning them.
            // This is a safeguard against EF materialization issues with JOINs.
            if (plant.Sections != null)
            {
                Sections = plant.Sections.GroupBy(s => s.Id).Select(g => g.First()).ToList();
            }
            else
            {
                Sections = new List<PlantSection>();
            }

            DisplayImageSource = LoadImage(plant.ImagePath);
        }

        private BitmapImage? LoadImage(string? imagePath)
        {
            string imageToLoad = "pack://application:,,,/Images/placeholder.png";

            if (!string.IsNullOrEmpty(imagePath))
            {
                if (Path.IsPathRooted(imagePath) && File.Exists(imagePath))
                {
                    imageToLoad = imagePath;
                }
                else
                {
                    try
                    {
                        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                        while (dir != null && (!dir.GetDirectories("Images").Any() || !dir.GetDirectories("Views").Any()))
                        {
                            dir = dir.Parent;
                        }

                        if (dir != null)
                        {
                            string fullPath = Path.Combine(dir.FullName, "Images", "Plants", imagePath);
                            if (File.Exists(fullPath))
                            {
                                imageToLoad = fullPath;
                            }
                        }
                    }
                    catch { /* Игнорируем ошибки поиска пути */ }
                }
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
            catch
            {
                return null;
            }
        }
    }
}
