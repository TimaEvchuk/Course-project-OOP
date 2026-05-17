using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using Plantify.ViewModels;

namespace Plantify.Converters
{
    public class JsonToSectionsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var json = value as string;
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                // The JSON was created from an anonymous type, so we deserialize to a compatible shape.
                var sections = JsonSerializer.Deserialize<List<PlantSectionViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return sections;
            }
            catch
            {
                // If deserialization fails, return a single section with the raw text
                // to ensure something is still displayed.
                return new List<PlantSectionViewModel>
                {
                    new PlantSectionViewModel { Title = "Описание (ошибка)", Content = json }
                };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
