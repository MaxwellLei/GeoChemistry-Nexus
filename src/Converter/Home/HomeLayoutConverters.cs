using System;
using System.Globalization;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 用可视区域高度减去固定边距，得到“刚好撑满一屏”的最小高度。
    /// ConverterParameter 形如 "30" 或 "30,320"：第一个数是要减去的边距，第二个数（可选）是最小值下限。
    /// </summary>
    public class ViewportFillMinHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double viewport = value is double v && !double.IsNaN(v) && !double.IsInfinity(v) ? v : 0d;

            double margin = 0d;
            double floor = 0d;
            if (parameter is string text)
            {
                var parts = text.Split(',');
                if (parts.Length > 0)
                    double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out margin);
                if (parts.Length > 1)
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out floor);
            }

            return Math.Max(viewport - margin, floor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 按宽度换算固定比例的高度：高度 = 宽度 × ratio + offset。
    /// ConverterParameter 形如 "0.56" 或 "0.56,6"：第一个数是高宽比，第二个数（可选）是额外固定偏移。
    /// 用于让网格类布局（如周期表）随宽度等比缩放，而不是被拉伸填满可用高度。
    /// </summary>
    public class WidthToAspectHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = value is double v && !double.IsNaN(v) && !double.IsInfinity(v) ? v : 0d;

            double ratio = 1d;
            double offset = 0d;
            if (parameter is string text)
            {
                var parts = text.Split(',');
                if (parts.Length > 0)
                    double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out ratio);
                if (parts.Length > 1)
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out offset);
            }

            return width * ratio + offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
