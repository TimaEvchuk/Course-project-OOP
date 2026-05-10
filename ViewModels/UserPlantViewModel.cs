using CommunityToolkit.Mvvm.ComponentModel;
using Plantify.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Plantify.ViewModels
{
    public partial class UserPlantViewModel : BaseViewModel
    {
        private readonly UserPlant _userPlant;

        public int UserPlantId => _userPlant.Id;
        
        // Properties directly from UserPlant or its Plant
        public string Name => _userPlant.CustomName ?? _userPlant.Plant.Name;
        public string Species => _userPlant.Plant.Variety;
        public string Location => _userPlant.Location ?? "Не указано";
        public string LightRequirement => _userPlant.Plant.LightRequirement;

        public BitmapImage? DisplayImageSource { get; private set; }

        public string NextWateringDue
        {
            get
            {
                var daysSinceWatered = (DateTime.Today - _userPlant.LastUserWateringDate).Days;
                var wateringInterval = _userPlant.Plant.WateringInterval;
                var daysLeft = wateringInterval - daysSinceWatered;

                if (daysLeft <= 0) return "Сегодня";
                if (daysLeft == 1) return "Завтра";
                return $"{daysLeft} дней";
            }
        }

        public string NextFertilizingDue
        {
            get
            {
                if (_userPlant.Plant.FertilizingInterval <= 0) return "-"; // Don't show if interval is 0

                var daysSinceFertilized = (DateTime.Today - _userPlant.LastFertilizedDate).Days;
                var fertilizingInterval = _userPlant.Plant.FertilizingInterval;
                var daysLeft = fertilizingInterval - daysSinceFertilized;

                if (daysLeft <= 0) return "Сегодня";
                if (daysLeft == 1) return "Завтра";
                return $"{daysLeft} дней";
            }
        }

        [ObservableProperty]
        private bool _isTaskCompletedToday;

        public UserPlantViewModel(UserPlant userPlant)
        {
            _userPlant = userPlant ?? throw new ArgumentNullException(nameof(userPlant));
            if (userPlant.Plant == null) throw new ArgumentNullException(nameof(userPlant.Plant));

            var imagePath = _userPlant.CustomImagePath ?? _userPlant.Plant.ImagePath;
            DisplayImageSource = LoadImage(imagePath);
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
            
            if (imageToLoad == null)
            {
                // Fallback to a placeholder if no image is found
                imageToLoad = "pack://application:,,,/Images/placeholder.png";
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageToLoad, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Important for use in data templates and on different threads
                return bitmap;
            }
            catch (Exception)
            {
                // In case of any error, return null or a default placeholder
                return null; 
            }
        }
    }
}
