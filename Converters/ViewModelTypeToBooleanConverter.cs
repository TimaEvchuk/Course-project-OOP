using System;
using System.Globalization;
using System.Windows.Data;
using Plantify.ViewModels;

namespace Plantify.Converters
{
    public class ViewModelTypeToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || !(parameter is Type))
            {
                return false;
            }

            var viewModelType = value.GetType();
            var targetViewModelType = (Type)parameter;

            return viewModelType == targetViewModelType;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
