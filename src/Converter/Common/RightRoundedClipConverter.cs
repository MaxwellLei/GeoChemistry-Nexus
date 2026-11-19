using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 裁剪几何：左边为直角，右边圆角与外框一致（半径减去边框厚度，贴着描边内侧）。
    /// 参数依次为宽度、高度、CornerRadius、BorderThickness。
    /// </summary>
    public class RightRoundedClipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 3
                && values[0] is double width
                && values[1] is double height
                && values[2] is CornerRadius radius)
            {
                if (width < double.Epsilon || height < double.Epsilon)
                    return Geometry.Empty;

                double topRight = radius.TopRight;
                double bottomRight = radius.BottomRight;
                if (values.Length >= 4 && values[3] is Thickness thickness)
                {
                    topRight = Math.Max(0, topRight - Math.Max(thickness.Right, thickness.Top));
                    bottomRight = Math.Max(0, bottomRight - Math.Max(thickness.Right, thickness.Bottom));
                }

                topRight = Math.Min(topRight, width);
                bottomRight = Math.Min(bottomRight, width);
                if (topRight + bottomRight > height && height > 0)
                {
                    double scale = height / (topRight + bottomRight);
                    topRight *= scale;
                    bottomRight *= scale;
                }

                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(0, 0), true, true);
                    ctx.LineTo(new Point(width - topRight, 0), true, false);
                    if (topRight > 0)
                        ctx.ArcTo(new Point(width, topRight), new Size(topRight, topRight),
                            0, false, SweepDirection.Clockwise, true, false);
                    else
                        ctx.LineTo(new Point(width, 0), true, false);

                    ctx.LineTo(new Point(width, height - bottomRight), true, false);
                    if (bottomRight > 0)
                        ctx.ArcTo(new Point(width - bottomRight, height), new Size(bottomRight, bottomRight),
                            0, false, SweepDirection.Clockwise, true, false);
                    else
                        ctx.LineTo(new Point(width, height), true, false);

                    ctx.LineTo(new Point(0, height), true, false);
                    ctx.LineTo(new Point(0, 0), true, false);
                }

                geometry.Freeze();
                return geometry;
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
