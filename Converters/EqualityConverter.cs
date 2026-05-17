using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Plantify.Converters
{
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            for (int i = 1; i < values.Length; i++)
            {
                if (!values[0].Equals(values[i]))
                    return false;
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[0]; // Not needed for one-way binding
        }
    }
}
