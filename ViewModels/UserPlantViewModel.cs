using CommunityToolkit.Mvvm.ComponentModel;
using Plantify.Models;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Plantify.ViewModels
{
    public partial class UserPlantViewModel : BaseViewModel
    {
        private readonly UserPlant _userPlant;

        public int UserPlantId => _userPlant.Id;
        
        public string Name => _userPlant.CustomName ?? _userPlant.Plant.Name;
        public string Species => _userPlant.Plant.Variety;
        public string Location => _userPlant.Location ?? "Не указано";
        public string LightRequirement => _userPlant.Plant.LightRequirement;

        public BitmapImage? DisplayImageSource { get; private set; }

        public int DaysToNextWatering
        {
            get
            {
                var nextWateringDate = _userPlant.LastUserWateringDate.AddDays(_userPlant.Plant.WateringInterval);
                return (nextWateringDate - DateTime.Today).Days;
            }
        }
        
        public int DaysToNextFertilizing
        {
            get
            {
                if (_userPlant.Plant.FertilizingInterval <= 0) return int.MaxValue;
                var nextFertilizingDate = _userPlant.LastFertilizedDate.AddDays(_userPlant.Plant.FertilizingInterval);
                return (nextFertilizingDate - DateTime.Today).Days;
            }
        }

        public string NextWateringDue => GetFormattedDueDate(DaysToNextWatering, _userPlant.LastUserWateringDate.AddDays(_userPlant.Plant.WateringInterval));
        public string NextFertilizingDue => _userPlant.Plant.FertilizingInterval <= 0 ? "-" : GetFormattedDueDate(DaysToNextFertilizing, _userPlant.LastFertilizedDate.AddDays(_userPlant.Plant.FertilizingInterval));
        
        private string GetFormattedDueDate(int daysLeft, DateTime nextDate)
        {
            if (daysLeft <= 0) return "Сегодня";
            if (daysLeft == 1) return "Завтра";
            return nextDate.ToString("dd MMMM");
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
                return null; 
            }
        }
    }
}
