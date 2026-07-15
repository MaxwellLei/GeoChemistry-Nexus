using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 将 #RGB / #RRGGBB / #AARRGGBB 字符串转为 SolidColorBrush；无效时返回 Transparent。
    /// </summary>
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string colorString || string.IsNullOrWhiteSpace(colorString))
                return Brushes.Transparent;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorString.Trim());
                return new SolidColorBrush(color);
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
                return brush.Color.ToString();
            return string.Empty;
        }
    }
}
