using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 公告背景：自定义颜色或预设主题 → 同色系渐变。服务端不下发完整渐变参数。
    /// </summary>
    public class AnnouncementThemeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HomeAnnouncementDisplayViewModel announcement)
                return AnnouncementBrushes.Create(announcement.ThemeKey, announcement.BackgroundColor);

            return AnnouncementBrushes.Create(value as string, null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 公告装饰图形缩放：按卡片短边相对原型高度（236）同比缩小。
    /// </summary>
    public class AnnouncementDecorScaleConverter : IValueConverter
    {
        public const double ReferenceHeight = 236d;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double height = value is double actual && actual > 1 ? actual : 150d;
            return height / ReferenceHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 只显示与 ConverterParameter 相同的已知多边形样式。未知样式不会到达这里。
    /// </summary>
    public class AnnouncementPolygonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string current = value as string ?? string.Empty;
            string expected = parameter as string ?? string.Empty;
            return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 公告渐变画刷。方向接近原型里的 118°：左上偏深，右下偏浅。
    /// </summary>
    public static class AnnouncementBrushes
    {
        private static readonly Point GradientStart = new(0, 0.08);
        private static readonly Point GradientEnd = new(1, 0.62);

        private static readonly Dictionary<string, Brush> Themes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["blue"] = CreateGradient("#0A5CE0", "#1687FF", "#6AB8FF"),
            ["purple"] = CreateGradient("#5B2BD0", "#8552E8", "#C79BFF"),
            ["teal"] = CreateGradient("#067A68", "#0AA88E", "#6FD9C3"),
            ["orange"] = CreateGradient("#C45A08", "#F79523", "#FFC56A"),
        };

        public static Brush Create(string? theme, string? colorHex)
        {
            if (TryParseColor(colorHex, out Color color))
                return CreateGradient(Darken(color, 0.30), color, Lighten(color, 0.42));

            string key = string.IsNullOrWhiteSpace(theme) ? "blue" : theme;
            return Themes.TryGetValue(key, out Brush? brush) && brush != null ? brush : Themes["blue"];
        }

        private static bool TryParseColor(string? value, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                object? converted = ColorConverter.ConvertFromString(value.Trim());
                if (converted is not Color parsed)
                    return false;

                color = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Color Darken(Color color, double amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return Color.FromRgb(
                (byte)(color.R * (1 - amount)),
                (byte)(color.G * (1 - amount)),
                (byte)(color.B * (1 - amount)));
        }

        private static Color Lighten(Color color, double amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return Color.FromRgb(
                (byte)(color.R + (255 - color.R) * amount),
                (byte)(color.G + (255 - color.G) * amount),
                (byte)(color.B + (255 - color.B) * amount));
        }

        private static Brush CreateGradient(string start, string mid, string end)
        {
            return CreateGradient(
                (Color)ColorConverter.ConvertFromString(start),
                (Color)ColorConverter.ConvertFromString(mid),
                (Color)ColorConverter.ConvertFromString(end));
        }

        private static Brush CreateGradient(Color start, Color mid, Color end)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = GradientStart,
                EndPoint = GradientEnd,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(start, 0),
                    new GradientStop(mid, 0.46),
                    new GradientStop(end, 1),
                }
            };
            brush.Freeze();
            return brush;
        }
    }
}
