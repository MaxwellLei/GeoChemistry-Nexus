using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 多个布尔值按逻辑或合并；任一为 true 则返回 true。
    /// 用于卡片悬停样式在右键菜单打开后仍保持（IsMouseOver || ContextMenu.IsOpen）。
    /// </summary>
    public class BoolOrMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            foreach (var value in values)
            {
                if (value is bool boolValue && boolValue)
                    return true;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
