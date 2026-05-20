using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Plantify.Converters
{
    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string imagePath = value as string;
            string imageToLoad = "pack://application:,,,/Images/placeholder.png";

            if (!string.IsNullOrEmpty(imagePath))
            {
                // First, check if it's a full, rooted path to an existing file (like in AppData)
                if (Path.IsPathRooted(imagePath) && File.Exists(imagePath))
                {
                    imageToLoad = imagePath;
                }
                else
                {
                    // Fallback for old logic: try to find it as a relative path in the project Images/Plants folder
                    try
                    {
                        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                        // Traverse up to find the project root
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
                    catch { /* Ignore path search errors */ }
                }
            }
            
            return CreateBitmapFromPath(imageToLoad);
        }

        private BitmapImage CreateBitmapFromPath(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                // In case the final path is invalid or the placeholder is missing, return an empty image.
                return new BitmapImage();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
