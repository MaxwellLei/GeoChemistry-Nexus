using GeoChemistryNexus.Helpers;
using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    public class EnumToDisplayStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            Type type = value.GetType();
            if (!type.IsEnum) return value.ToString();

            FieldInfo fi = type.GetField(value.ToString());
            if (fi != null)
            {
                var attributes = (LocalizedDescriptionAttribute[])fi.GetCustomAttributes(typeof(LocalizedDescriptionAttribute), false);
                if (attributes.Length > 0 && !string.IsNullOrEmpty(attributes[0].Description))
                {
                    return attributes[0].Description;
                }
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
