using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 将从 0 开始的序号（如 ItemsControl.AlternationIndex）转换为从 1 开始的显示序号。
    /// </summary>
    public class OneBasedIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int index ? index + 1 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
