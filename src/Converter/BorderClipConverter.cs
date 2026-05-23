using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 将Border的ActualWidth、ActualHeight和CornerRadius转换为裁剪几何形状，
    /// 实现对Border子元素的圆角裁剪。
    /// </summary>
    public class BorderClipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3
                && values[0] is double width
                && values[1] is double height
                && values[2] is CornerRadius radius)
            {
                if (width < double.Epsilon || height < double.Epsilon)
                    return Geometry.Empty;

                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    double topLeft = radius.TopLeft;
                    double topRight = radius.TopRight;
                    double bottomRight = radius.BottomRight;
                    double bottomLeft = radius.BottomLeft;

                    ctx.BeginFigure(new Point(topLeft, 0), true, true);

                    // Top edge → Top-right corner
                    ctx.LineTo(new Point(width - topRight, 0), true, false);
                    if (topRight > 0)
                        ctx.ArcTo(new Point(width, topRight), new Size(topRight, topRight),
                            0, false, SweepDirection.Clockwise, true, false);
                    else
                        ctx.LineTo(new Point(width, 0), true, false);

                    // Right edge → Bottom-right corner
                    ctx.LineTo(new Point(width, height - bottomRight), true, false);
                    if (bottomRight > 0)
                        ctx.ArcTo(new Point(width - bottomRight, height), new Size(bottomRight, bottomRight),
                            0, false, SweepDirection.Clockwise, true, false);
                    else
                        ctx.LineTo(new Point(width, height), true, false);

                    // Bottom edge → Bottom-left corner
                    ctx.LineTo(new Point(bottomLeft, height), true, false);
                    if (bottomLeft > 0)
                        ctx.ArcTo(new Point(0, height - bottomLeft), new Size(bottomLeft, bottomLeft),
                            0, false, SweepDirection.Clockwise, true, false);
                    else
                        ctx.LineTo(new Point(0, height), true, false);

                    // Left edge → Top-left corner
                    ctx.LineTo(new Point(0, topLeft), true, false);
                    if (topLeft > 0)
                        ctx.ArcTo(new Point(topLeft, 0), new Size(topLeft, topLeft),
                            0, false, SweepDirection.Clockwise, true, false);
                    else
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
