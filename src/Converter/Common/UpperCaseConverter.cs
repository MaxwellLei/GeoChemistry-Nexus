using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 把字符串转为大写显示（如导出格式 png → PNG）。仅用于单向绑定。
    /// </summary>
    public class UpperCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString()?.ToUpperInvariant() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
