using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Plantify.Converters
{
    public class NextCareActionToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int daysLeft)
            {
                return Brushes.Black; // Default or fallback color
            }

            var terracotta = (SolidColorBrush)Application.Current.FindResource("BrushTerracotta");
            var forestGreen = (SolidColorBrush)Application.Current.FindResource("BrushForestGreen");
            var baseGray = (SolidColorBrush)Application.Current.FindResource("BrushBaseGray");

            if (daysLeft <= 0)
            {
                return terracotta;
            }
            if (daysLeft >= 1 && daysLeft <= 5)
            {
                return forestGreen;
            }
            
            return baseGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
