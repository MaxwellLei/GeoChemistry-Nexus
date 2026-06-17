using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    public class ListBoxItemBorderThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is ListBoxItem item && values[1] is ListBox listBox)
            {
                var index = listBox.ItemContainerGenerator.IndexFromContainer(item);
                var count = listBox.Items.Count;

                if (index < count - 1)
                {
                    return new Thickness(0, 0, 1, 0);
                }
            }

            return new Thickness(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
