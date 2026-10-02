using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    public class DoublePrecisionConverter : IValueConverter
    {
        public int Precision { get; set; } = 4;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return Math.Round(d, Precision);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return d;
            }
            if (value is string s && double.TryParse(s, out double result))
            {
                return result;
            }
            return value;
        }
    }
}
