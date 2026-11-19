using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 数值线性变换：result = value * factor + offset。
    /// ConverterParameter 形如 "1.25" 或 "-0.25,-12"（factor[,offset]），按不变文化解析。
    /// 用于让弹窗宽度/位置按触发控件宽度的比例计算。
    /// </summary>
    public class ScaleOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double number || double.IsNaN(number))
                return 0d;

            double factor = 1d;
            double offset = 0d;

            if (parameter is string text && !string.IsNullOrWhiteSpace(text))
            {
                var parts = text.Split(',');
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out factor);
                if (parts.Length > 1)
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out offset);
            }

            return number * factor + offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
