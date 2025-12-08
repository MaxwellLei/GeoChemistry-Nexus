using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    public class IndexToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter != null)
            {
                if (int.TryParse(value.ToString(), out int intValue) && int.TryParse(parameter.ToString(), out int intParam))
                {
                    return intValue == intParam;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int intParam))
                {
                    return intParam;
                }
            }
            return Binding.DoNothing;
        }
    }
}
