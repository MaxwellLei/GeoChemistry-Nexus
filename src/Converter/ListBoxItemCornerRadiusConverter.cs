using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    public class ListBoxItemCornerRadiusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is ListBoxItem item && values[1] is ListBox listBox)
            {
                var index = listBox.ItemContainerGenerator.IndexFromContainer(item);
                var count = listBox.Items.Count;
                double radius = 6.0;
                
                if (parameter is double paramRadius)
                {
                    radius = paramRadius;
                }
                else if (parameter is string paramString && double.TryParse(paramString, out double parsedRadius))
                {
                    radius = parsedRadius;
                }

                if (count == 1)
                {
                    return new CornerRadius(radius);
                }

                if (index == 0)
                {
                    return new CornerRadius(radius, 0, 0, radius);
                }
                
                if (index == count - 1)
                {
                    return new CornerRadius(0, radius, radius, 0);
                }
            }

            return new CornerRadius(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
