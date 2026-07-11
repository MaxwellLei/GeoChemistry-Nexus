using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 比较两个字符串是否相等（忽略大小写），用于列表选中高亮
    /// </summary>
    public class StringEqualsMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            string? left = values[0]?.ToString();
            string? right = values[1]?.ToString();

            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return false;

            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
