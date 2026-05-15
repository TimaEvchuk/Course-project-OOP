using System;
using System.Globalization;
using System.Windows.Data;

namespace Plantify.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() is not { } enumValue || parameter?.ToString() is not { } parameterValue)
            {
                return false;
            }

            return enumValue.Equals(parameterValue, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return Binding.DoNothing;

            if (value is true)
            {
                return Enum.Parse(targetType, parameter.ToString() ?? string.Empty);
            }

            return Binding.DoNothing;
        }
    }
}
