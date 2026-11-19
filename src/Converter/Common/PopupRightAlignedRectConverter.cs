using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 生成 Popup 的 PlacementRectangle，使弹窗与触发控件“右缘对齐、向左加宽”。
    /// values: [触发控件 ActualWidth, 触发控件 ActualHeight]；
    /// parameter: "extra,margin"，extra 为弹窗比触发控件多出的宽度比例（如 0.3 表示宽 30%），
    /// margin 为弹窗外层阴影留白（左右各一份）。
    /// 矩形位于触发控件的局部坐标系，由 WPF 随 UI 缩放（LayoutTransform）一并换算，
    /// 因此比直接使用 HorizontalOffset 更不易在缩放下错位。
    /// Bottom 放置时：弹窗左缘 = 矩形左缘，弹窗顶缘 = 矩形底缘。
    /// </summary>
    public class PopupRightAlignedRectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not double width || values[1] is not double height
                || double.IsNaN(width) || double.IsNaN(height))
            {
                return Rect.Empty;
            }

            double extra = 0.3;
            double margin = 0;
            if (parameter is string text && !string.IsNullOrWhiteSpace(text))
            {
                var parts = text.Split(',');
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out extra);
                if (parts.Length > 1)
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out margin);
            }

            double left = -(width * extra + margin);
            double rectWidth = width * (1 + extra) + margin * 2;
            return new Rect(left, 0, rectWidth, height);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
