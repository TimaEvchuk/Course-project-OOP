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
            _messenger.Send(new UserPlantSelectionChangedMessage(0)); // Value is not used, just triggers re-calculation
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
