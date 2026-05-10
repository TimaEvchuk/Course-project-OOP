using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace Plantify.Converters
{
    public class NextCareActionToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string careText)
            {
                return Brushes.Black; // Default or fallback color
            }

            var terracotta = (SolidColorBrush)Application.Current.FindResource("BrushTerracotta");
            var forestGreen = (SolidColorBrush)Application.Current.FindResource("BrushForestGreen");
            var baseGray = (SolidColorBrush)Application.Current.FindResource("BrushBaseGray");

            if (careText.Equals("Сегодня", StringComparison.OrdinalIgnoreCase))
            {
                return terracotta;
            }

            if (careText.Equals("Завтра", StringComparison.OrdinalIgnoreCase))
            {
                return forestGreen;
            }
            
            if (careText.EndsWith("дней") && int.TryParse(careText.Split(' ')[0], out int days))
            {
                if (days >= 2 && days <= 5)
                {
                    return forestGreen;
                }
            }

            return baseGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
