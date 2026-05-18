using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Plantify.Messages;
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
        
        public string Name => _userPlant.CustomName ?? _userPlant.Plant?.Name ?? "Неизвестное растение";
        public string Species => _userPlant.Plant?.Variety?.Name ?? "Неизвестно";
        public string Location => _userPlant.Location ?? "Не указано";
        public string LightRequirement => _userPlant.Plant?.LightRequirement?.Name ?? "Неизвестно";

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

        private readonly IMessenger _messenger;

        public UserPlantViewModel(UserPlant userPlant, IMessenger messenger)
        {
            _userPlant = userPlant ?? throw new ArgumentNullException(nameof(userPlant));
            _messenger = messenger;
            if (userPlant.Plant == null) throw new ArgumentNullException(nameof(userPlant.Plant));

            var imagePath = _userPlant.Plant.ImagePath;
            DisplayImageSource = LoadImage(imagePath);
        }

        partial void OnIsTaskCompletedTodayChanged(bool value)
        {
            _messenger.Send(new UserPlantSelectionChangedMessage(value ? 1 : -1)); // Send message for change
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
            
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageToLoad ?? "pack://application:,,,/Images/placeholder.png", UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); 
                return bitmap;
            }
            catch (Exception)
            {
                // If the primary image fails, try the placeholder as a fallback.
                try
                {
                    var fallback = new BitmapImage();
                    fallback.BeginInit();
                    fallback.UriSource = new Uri("pack://application:,,,/Images/placeholder.png", UriKind.RelativeOrAbsolute);
                    fallback.CacheOption = BitmapCacheOption.OnLoad;
                    fallback.EndInit();
                    fallback.Freeze();
                    return fallback;
                }
                catch
                {
                    // If even the placeholder fails, return an empty image to avoid a null crash.
                    return new BitmapImage();
                }
            }
        }
    }
}
