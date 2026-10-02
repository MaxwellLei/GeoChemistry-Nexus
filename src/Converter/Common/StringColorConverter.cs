using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GeoChemistryNexus.Converter
{
    public class StringColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                try
                {
                    return (Color)System.Windows.Media.ColorConverter.ConvertFromString(colorString);
                }
                catch
                {
                    return Colors.Transparent;
                }
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.ToString();
            }
            return "#00000000";
        }
    }
}
