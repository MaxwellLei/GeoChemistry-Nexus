using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 将 double.NaN 与空字符串互转，用于“留空表示不限制”的可选数值绑定。
    /// </summary>
    public class OptionalDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d))
                    return string.Empty;

                return Math.Round(d, 4).ToString(culture);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return double.NaN;

            if (value is string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return double.NaN;

                if (double.TryParse(text, NumberStyles.Float, culture, out double result))
                    return result;

                return Binding.DoNothing;
            }

            if (value is double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d))
                    return double.NaN;
                return d;
            }

            return Binding.DoNothing;
        }
    }
}
