using System; // Added for Uri
using System.IO; // Added for FileStream
using System.Windows.Media.Imaging; // Added for BitmapImage

namespace Plantify.Models
{
    public class Plant
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string LightRequirement { get; set; } = null!;
        public string Variety { get; set; } = null!;
        public int WateringInterval { get; set; }
        public string? ImagePath { get; set; }
        public virtual List<PlantSection> Sections { get; set; } = new();

        // New property for displaying image in UI
        public BitmapImage? DisplayImageSource
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath)) return null;

                try
                {
                    var appDomainBasePath = AppDomain.CurrentDomain.BaseDirectory;
                    var fullPath = Path.Combine(appDomainBasePath, ImagePath);

                    if (!File.Exists(fullPath))
                    {
                        // Fallback to project root for development scenario
                        var projectRootPath = Path.GetFullPath(Path.Combine(appDomainBasePath, "..", "..", ".."));
                        fullPath = Path.Combine(projectRootPath, ImagePath);
                        
                        if (!File.Exists(fullPath))
                        {
                            // Could show a "not found" image here
                            return null;
                        }
                    }
                    
                    // Using a FileStream to avoid file locking issues
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Keep image in memory after loading
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Freeze the BitmapImage to make it thread-safe and optimize memory
                    }
                    return bitmap;
                }
                catch (Exception ex)
                {
                    // Log error or show message box
                    // MessageBox.Show($"Error loading image from {ImagePath}: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
