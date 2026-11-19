using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// 公告浅色背景：与 <see cref="AnnouncementThemeToBrushConverter"/> 同色系，但整体偏浅，配深色文字使用。
    /// </summary>
    public class AnnouncementLightThemeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HomeAnnouncementDisplayViewModel announcement)
                return AnnouncementBrushes.CreateLight(announcement.ThemeKey, announcement.BackgroundColor);

            return AnnouncementBrushes.CreateLight(value as string, null);
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

        /// <summary>
        /// 预设主题。顺序即发布器下拉顺序。左侧偏深，保证白字可读。
        /// </summary>
        private static readonly (string Key, string Start, string Mid, string End)[] Palette =
        {
            ("blue", "#0A5CE0", "#1687FF", "#6AB8FF"),
            ("purple", "#5B2BD0", "#8552E8", "#C79BFF"),
            ("teal", "#067A68", "#0AA88E", "#6FD9C3"),
            ("orange", "#C45A08", "#F79523", "#FFC56A"),
            ("red", "#C62828", "#EF5350", "#FF9A8B"),
            ("green", "#1B8A42", "#2EBE5A", "#8BE0A4"),
            ("indigo", "#3F51C5", "#6674EE", "#B0BAFF"),
            ("rose", "#D12A72", "#F25C98", "#FFB0D0"),
            ("cyan", "#0C86B5", "#22B4DE", "#8ADCF3"),
            ("gold", "#8A5E06", "#D4A017", "#F0CC62"),
            ("slate", "#3D4F61", "#6A7E92", "#B4C2D0"),
            ("magenta", "#A81CC0", "#D44AE8", "#F2B0FA"),
            ("navy", "#163E8C", "#3A6FD4", "#8FB4F0"),
            ("copper", "#A85A28", "#D4843C", "#F0C090"),
        };

        public static IReadOnlyList<string> ThemeKeys { get; } = Palette.Select(item => item.Key).ToArray();

        private static readonly Dictionary<string, Brush> Themes = Palette.ToDictionary(
            item => item.Key,
            item => CreateGradient(item.Start, item.Mid, item.End),
            StringComparer.OrdinalIgnoreCase);

        public static Brush Create(string? theme, string? colorHex)
        {
            if (TryParseColor(colorHex, out Color color))
                return CreateGradient(Darken(color, 0.30), color, Lighten(color, 0.42));

            // 未指定预设主题：跟随软件当前主题色
            if (string.IsNullOrWhiteSpace(theme))
            {
                Color mid = AppThemeHelper.LightAccentColor;
                return CreateGradient(AppThemeHelper.AccentColor, mid, Lighten(mid, 0.45));
            }

            return Themes.TryGetValue(theme, out Brush? brush) && brush != null ? brush : Themes["blue"];
        }

        /// <summary>
        /// 浅色版渐变：取预设主题（或自定义色）的中间色大幅提亮，左上略深、右下更浅。
        /// </summary>
        public static Brush CreateLight(string? theme, string? colorHex)
        {
            Color baseColor;
            if (!TryParseColor(colorHex, out baseColor))
            {
                if (string.IsNullOrWhiteSpace(theme))
                {
                    // 未指定预设主题：跟随软件当前主题色
                    baseColor = AppThemeHelper.LightAccentColor;
                }
                else
                {
                    var entry = Palette.FirstOrDefault(p => string.Equals(p.Key, theme, StringComparison.OrdinalIgnoreCase));
                    if (entry.Key == null)
                        entry = Palette[0];
                    baseColor = (Color)ColorConverter.ConvertFromString(entry.Mid);
                }
            }

            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Lighten(baseColor, 0.90), 0),
                    new GradientStop(Lighten(baseColor, 0.76), 1),
                }
            };
            brush.Freeze();
            return brush;
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
