using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 判断温压计列表项是否为当前选中项（SelectedPlugin 为 null 时一律为 false）
    /// </summary>
    public class GeoTPluginSelectedMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            if (values[1] == null || values[1] == DependencyProperty.UnsetValue)
                return false;

            string? itemId = values[0] switch
            {
                Geothermometer plugin => plugin.Id,
                _ => values[0]?.ToString()
            };

            if (string.IsNullOrEmpty(itemId))
                return false;

            string? selectedId = values[1] switch
            {
                Geothermometer selected => selected.Id,
                _ => values[1]?.ToString()
            };

            if (string.IsNullOrEmpty(selectedId))
                return false;

            return string.Equals(itemId, selectedId, StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
