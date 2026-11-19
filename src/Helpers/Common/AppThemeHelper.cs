using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 应用主题色：内置若干套主题，默认使用主页的紫色（靛紫）主题。
    /// 所有可换肤的画刷都以固定的资源键写入 Application.Resources 顶层，
    /// XAML 中统一通过 DynamicResource 引用；切换主题只需就地修改画刷颜色，所有窗口即时生效。
    /// </summary>
    /// <summary>数据工作表使用的一组主题色（默认紫色主题下与设计稿一致，其他主题按主色相自动推导）。</summary>
    public sealed class SheetColors
    {
        public Color Accent { get; init; }
        /// <summary>选区覆盖到的行头 / 列头底色。</summary>
        public Color Highlight { get; init; }
        /// <summary>计算结果区（数据区与表头）底色。</summary>
        public Color OutputBack { get; init; }
        /// <summary>结果列分组带底色。</summary>
        public Color OutputBand { get; init; }
        /// <summary>输入列分组带底色。</summary>
        public Color InputBand { get; init; }
        /// <summary>输入区与结果区之间分隔列的底色。</summary>
        public Color Separator { get; init; }
        public Color HeaderText { get; init; }
        public Color RowHeaderBack { get; init; }
        /// <summary>数据区网格线 / 表头内部分隔线。</summary>
        public Color GridLine { get; init; }
        /// <summary>表头与数据区之间较明显的分隔线。</summary>
        public Color HeaderLine { get; init; }
        public Color Mute { get; init; }
        public Color Sub { get; init; }
    }

    public static class AppThemeHelper
    {
        public const string ConfigKey = ConfigKeys.AppTheme;
        public const string DefaultThemeId = "purple";

        private sealed record ThemeDefinition(
            string Id, string LanguageKey, string FallbackName, string AccentHex, double NeutralTint);

        /// <summary>
        /// 内置主题。NeutralTint 控制中性色（文字 / 分割线 / 背景）带上主题色相的程度：1 = 与主页原配色一致。
        /// </summary>
        private static readonly ThemeDefinition[] Definitions =
        {
            new("purple", "app_theme_purple", "紫色", "#3C4A98", 1.00),
            new("blue", "app_theme_blue", "蓝色", "#1769D6", 1.00),
            new("teal", "app_theme_teal", "青色", "#0F8B8D", 0.60),
            new("green", "app_theme_green", "绿色", "#2E8B57", 0.50),
            new("orange", "app_theme_orange", "橙色", "#C75B12", 0.45),
            new("rose", "app_theme_rose", "玫红", "#C8245F", 0.50),
            new("graphite", "app_theme_graphite", "石墨灰", "#475467", 0.12),
        };

        private sealed class Palette
        {
            public Color Accent;
            public Color Hover;
            public Color Pressed;
            public Color Top;
            public Color HoverTop;
            public Color Light;
            public Color Deep;
            public Color Lighter;
            public Color Soft;
            public Color SoftBorder;
            public Color Mist;
            public Color Ink;
            public Color Sub;
            public Color Mute;
            public Color Line;
            public Color Chip;
            public Color ChipHover;
            public Color TimerBar;
            public Color PageBg;
            public Color Sidebar;
            // ----- 数据工作表（ReoGridTableHost）专用 -----
            public Color SheetHighlight;
            public Color SheetOutput;
            public Color SheetOutputBand;
            public Color SheetInputBand;
            public Color SheetSeparator;
            public Color SheetHeaderText;
            public Color SheetRowHeader;
            public Color SheetGridLine;
        }

        private static string _currentId = DefaultThemeId;
        private static Palette _currentPalette = Build(Definitions[0]);
        private static SheetColors _currentSheetColors = ToSheetColors(_currentPalette);

        /// <summary>
        /// 数据工作表（表头、行头、选区、输入 / 结果区底色）使用的主题色，随主题切换整体替换为新实例。
        /// 绘制代码可以用引用比较判断主题是否变化，从而决定是否重建画刷。
        /// </summary>
        public static SheetColors CurrentSheetColors => _currentSheetColors;

        /// <summary>
        /// 当前主题 Id。
        /// </summary>
        public static string CurrentId => _currentId;

        /// <summary>
        /// 当前主题主色。
        /// </summary>
        public static Color AccentColor => _currentPalette.Accent;

        /// <summary>
        /// 当前主题的“提亮主色”，用于公告横幅等需要明亮底色的场景。
        /// </summary>
        public static Color LightAccentColor => _currentPalette.Light;

        /// <summary>
        /// 创建设置页使用的主题选项列表。
        /// </summary>
        public static IReadOnlyList<AppThemeOption> CreateOptions()
        {
            return Definitions
                .Select(def =>
                {
                    var swatch = new SolidColorBrush(ParseColor(def.AccentHex));
                    swatch.Freeze();
                    return new AppThemeOption(def.Id, def.LanguageKey, def.FallbackName, swatch);
                })
                .ToList();
        }

        /// <summary>
        /// 把配置中的主题 Id 规整为已知主题；未知或为空时回退到默认（紫色）。
        /// </summary>
        public static string ResolveThemeId(string? id)
        {
            var match = Definitions.FirstOrDefault(d => string.Equals(d.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase));
            return match?.Id ?? DefaultThemeId;
        }

        /// <summary>
        /// 读取已保存的主题并应用。必须在合并 Styles.xaml 之前或之后、创建任何窗口之前调用。
        /// </summary>
        public static void Initialize()
        {
            string id = ResolveThemeId(ConfigHelper.GetConfig(ConfigKey));
            Apply(id);
        }

        /// <summary>
        /// 切换并保存主题，所有已打开的窗口即时刷新。
        /// </summary>
        public static void SetTheme(string? id)
        {
            string resolved = ResolveThemeId(id);
            if (string.Equals(resolved, _currentId, StringComparison.Ordinal))
                return;

            ConfigHelper.SetConfig(ConfigKey, resolved);
            Apply(resolved);
            WeakReferenceMessenger.Default.Send(new AppThemeChangedMessage(resolved));
        }

        private static void Apply(string id)
        {
            var definition = Definitions.First(d => d.Id == id);
            var palette = Build(definition);
            _currentId = definition.Id;
            _currentPalette = palette;
            _currentSheetColors = ToSheetColors(palette);

            var app = Application.Current;
            if (app == null)
                return;

            var res = app.Resources;
            Color tint = WithAlpha(palette.Accent, 0x1F);
            Color tintStrong = WithAlpha(palette.Accent, 0x38);

            // ===== 颜色资源（用于 DropShadowEffect 等必须是 Color 的属性） =====
            res["ThemeAccentColor"] = palette.Accent;
            res["ThemeAccentDeepColor"] = palette.Deep;
            res["ThemeAccentLightColor"] = palette.Light;
            res["ThemeAccentSoftColor"] = palette.Soft;

            // ===== 通用主题画刷 =====
            Solid(res, "ThemeAccentBrush", palette.Accent);
            Solid(res, "ThemeAccentDarkBrush", palette.Hover);
            Solid(res, "ThemeAccentPressedBrush", palette.Pressed);
            Solid(res, "ThemeAccentLightBrush", palette.Light);
            Solid(res, "ThemeAccentDeepBrush", palette.Deep);
            Solid(res, "ThemeAccentLighterBrush", palette.Lighter);
            Solid(res, "ThemeAccentSoftBrush", palette.Soft);
            Solid(res, "ThemeAccentSoftBorderBrush", palette.SoftBorder);
            Solid(res, "ThemeAccentTintBrush", tint);
            Solid(res, "ThemeAccentTintStrongBrush", tintStrong);
            // 文字链接：放在侧边栏等浅色背景上，使用比主色更浅的一档，悬停时出现浅色底
            Solid(res, "ThemeLinkBrush", palette.Light);
            Solid(res, "ThemeLinkHoverBackgroundBrush", tint);
            Solid(res, "ThemeChipBrush", palette.Chip);
            Solid(res, "ThemeChipHoverBrush", palette.ChipHover);
            Solid(res, "ThemePageBgBrush", palette.PageBg);
            Solid(res, "ThemeSidebarBrush", palette.Sidebar);

            // ===== 数据工作表（XAML 部分：图例、状态栏、检查器） =====
            Solid(res, "ThemeSheetOutputBrush", palette.SheetOutput);
            Solid(res, "ThemeSheetHighlightBrush", palette.SheetHighlight);
            Solid(res, "ThemeSheetRowHeaderBrush", palette.SheetRowHeader);
            Solid(res, "ThemeSheetGridLineBrush", palette.SheetGridLine);

            Gradient(res, "ThemeAccentGradientBrush", new Point(0, 0), new Point(0, 1), palette.Top, palette.Accent);
            Gradient(res, "SidebarSelectedHoverBrush", new Point(0, 0), new Point(0, 1), palette.HoverTop, palette.Hover);
            Gradient(res, "ThemeGlyphBrush", new Point(0, 0), new Point(1, 1), palette.Light, palette.Accent);

            // ===== 主页配色 =====
            Solid(res, "HomeInkBrush", palette.Ink);
            Solid(res, "HomeSubBrush", palette.Sub);
            Solid(res, "HomeMuteBrush", palette.Mute);
            Solid(res, "HomeLineBrush", palette.Line);
            Solid(res, "HomeBrandBrush", palette.Accent);
            Solid(res, "HomeSoftBrush", palette.Soft);

            // ===== 通知系统 =====
            Solid(res, "NotificationTitleBrush", palette.Ink);
            Solid(res, "NotificationMessageBrush", palette.Sub);
            Solid(res, "NotificationMutedBrush", palette.Mute);
            Solid(res, "NotificationDividerBrush", palette.Line);
            Solid(res, "NotificationChipBackgroundBrush", palette.Chip);
            Solid(res, "NotificationChipHoverBrush", palette.ChipHover);
            Solid(res, "NotificationCardBorderBrush", palette.Line);
            Solid(res, "NotificationAccentBrush", palette.Accent);
            Solid(res, "NotificationTimerBarBrush", palette.TimerBar);
            Solid(res, "NotificationUnreadDotBrush", palette.Accent);
            Solid(res, "NotificationBadgeBrush", palette.Accent);
            Solid(res, "NotificationConfirmGradientBrush", palette.Accent);
            Solid(res, "NotificationConfirmSoftBrush", tint);
            Solid(res, "NotificationConfirmTextBrush", palette.Accent);
            Solid(res, "NotificationProgressGradientBrush", palette.Accent);
            Solid(res, "NotificationProgressSoftBrush", palette.Soft);
            Solid(res, "NotificationProgressTextBrush", palette.Accent);
            Solid(res, "NotificationPrimaryButtonBrush", palette.Accent);
            Solid(res, "NotificationPrimaryButtonHoverBrush", palette.Hover);
            Solid(res, "NotificationPrimaryButtonPressedBrush", palette.Pressed);
            Solid(res, "NotificationSecondaryButtonBrush", palette.Chip);
            Solid(res, "NotificationSecondaryButtonHoverBrush", palette.ChipHover);

            // ===== 各模块沿用的强调色键（原先是写死的系统蓝） =====
            Solid(res, "MacSidebarActiveBrush", tint);
            Solid(res, "MacSidebarAccentBrush", palette.Accent);
            Solid(res, "TemplateCardHoverBrush", palette.Accent);
            Solid(res, "LayerRowSelectedBrush", tint);
            Solid(res, "LayerRowSelectedBorderBrush", tintStrong);
            Solid(res, "WidgetAccentBrush", palette.Accent);
            Solid(res, "EditorAccentBrush", palette.Accent);
            Solid(res, "EditorPrimaryBrush", palette.Accent);
            Solid(res, "EditorPrimarySoftBrush", palette.Soft);
            Solid(res, "EpmaAccent", palette.Accent);
            Solid(res, "ModernPrimaryBrush", palette.Accent);
            Solid(res, "ModernPrimarySoftBrush", palette.Soft);
            Solid(res, "ModernSelectedCardBorderBrush", palette.Accent);

            ApplyIllustrationPalette(res, palette);
        }

        /// <summary>
        /// 空状态插画使用的主题色：ThemeArt{角色}Brush / ThemeArt{角色}Color，
        /// 以及带透明度的 ThemeArt{角色}A{AA}Brush / Color（AA 为十六进制透明度，例如 2E）。
        /// 各角色对应原插画里的蓝色层次：Strong 主体线条与填充、Dark 与 Deep 深色强调、Lighter 次级高光、
        /// Mist 淡雾色点缀、SoftBorder 与 Soft 浅色底。插画里的灰色中性色保持不变。
        /// 新增插画若用到新的透明度，把它加进 IllustrationAlphas 即可。
        /// </summary>
        private static readonly (string Role, string Alphas)[] IllustrationAlphas =
        {
            ("Strong", "00 08 14 1F 24 26 2E 33 38 47 A6 CC"),
            ("Dark", "14 85 B8 C7"),
            ("Deep", "94"),
            ("Lighter", "0A 4D 52 73 8C BF D1 E0"),
            ("Mist", "4D EB"),
            ("Soft", "52 8C A6 CC E0"),
            ("SoftBorder", ""),
        };

        private static void ApplyIllustrationPalette(ResourceDictionary res, Palette palette)
        {
            var roles = new Dictionary<string, Color>
            {
                ["Strong"] = palette.Accent,
                ["Dark"] = palette.Hover,
                ["Deep"] = palette.Deep,
                ["Lighter"] = palette.Lighter,
                ["Mist"] = palette.Mist,
                ["Soft"] = palette.Soft,
                ["SoftBorder"] = palette.SoftBorder,
            };

            foreach (var (role, alphas) in IllustrationAlphas)
            {
                Color color = roles[role];
                res[$"ThemeArt{role}Color"] = color;
                Solid(res, $"ThemeArt{role}Brush", color);

                foreach (string alphaText in alphas.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    byte alpha = Convert.ToByte(alphaText, 16);
                    Color withAlpha = WithAlpha(color, alpha);
                    res[$"ThemeArt{role}A{alphaText.ToUpperInvariant()}Color"] = withAlpha;
                    Solid(res, $"ThemeArt{role}A{alphaText.ToUpperInvariant()}Brush", withAlpha);
                }
            }
        }

        /// <summary>
        /// 就地修改画刷颜色（所有 DynamicResource 引用同一实例，立即刷新）；若画刷已被冻结则换成新实例。
        /// </summary>
        private static void Solid(ResourceDictionary res, string key, Color color)
        {
            if (res.Contains(key) && res[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = color;
                return;
            }

            res[key] = new SolidColorBrush(color);
        }

        private static void Gradient(ResourceDictionary res, string key, Point start, Point end, Color from, Color to)
        {
            if (res.Contains(key)
                && res[key] is LinearGradientBrush existing
                && !existing.IsFrozen
                && existing.GradientStops.Count == 2)
            {
                existing.GradientStops[0].Color = from;
                existing.GradientStops[1].Color = to;
                return;
            }

            res[key] = new LinearGradientBrush
            {
                StartPoint = start,
                EndPoint = end,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(from, 0),
                    new GradientStop(to, 1),
                }
            };
        }

        private static Palette Build(ThemeDefinition def)
        {
            Color accent = ParseColor(def.AccentHex);
            var (h, s, l) = ToHsl(accent);
            double t = def.NeutralTint;
            double soft = Math.Max(t, 0.5);

            var palette = new Palette
            {
                Accent = accent,
                Hover = FromHsl(h, s, l - 0.045),
                Pressed = FromHsl(h, s, l - 0.105),
                Top = FromHsl(h, s, l + 0.06),
                HoverTop = FromHsl(h, s, l + 0.10),
                Light = FromHsl(h, s + 0.10, l + 0.14),
                Deep = FromHsl(h, s, l - 0.114),
                Lighter = FromHsl(h, s + 0.23, l + 0.24),
                Soft = FromHsl(h, 0.69 * soft, 0.96),
                SoftBorder = FromHsl(h, 0.62 * soft, 0.87),
                Mist = FromHsl(h, Math.Min(0.85, s + 0.05), 0.80),
                Ink = FromHsl(h, 0.54 * t, 0.19),
                Sub = FromHsl(h, 0.15 * t, 0.50),
                Mute = FromHsl(h, 0.19 * t, 0.67),
                Line = FromHsl(h, 0.36 * t, 0.94),
                Chip = FromHsl(h, 0.46 * t, 0.962),
                ChipHover = FromHsl(h, 0.50 * t, 0.929),
                TimerBar = FromHsl(h, 0.42 * t, 0.846),
                PageBg = FromHsl(h, 0.35 * t, 0.972),
                Sidebar = FromHsl(h, 0.25 * t, 0.953),
                SheetHighlight = FromHsl(h, 0.75 * soft, 0.937),
                SheetOutput = FromHsl(h, 1.00 * soft, 0.980),
                SheetOutputBand = FromHsl(h, 1.00 * soft, 0.9725),
                SheetInputBand = FromHsl(h, 0.59 * t, 0.990),
                SheetSeparator = FromHsl(h, 0.36 * t, 0.945),
                SheetHeaderText = FromHsl(h, 0.25 * t, 0.320),
                SheetRowHeader = FromHsl(h, 0.42 * t, 0.986),
                SheetGridLine = FromHsl(h, 0.34 * t, 0.959),
            };

            if (def.Id == DefaultThemeId)
            {
                // 默认主题与主页原有配色逐值一致
                palette.Hover = ParseColor("#34418A");
                palette.Pressed = ParseColor("#2B3675");
                palette.Light = ParseColor("#5262C8");
                palette.Deep = ParseColor("#283372");
                palette.Lighter = ParseColor("#6E80E2");
                palette.Soft = ParseColor("#EEF1FB");
                palette.SoftBorder = ParseColor("#C9D1F0");
                palette.Ink = ParseColor("#16204A");
                palette.Sub = ParseColor("#6B7392");
                palette.Mute = ParseColor("#9AA1BB");
                palette.Line = ParseColor("#E9ECF4");
                palette.Chip = ParseColor("#F1F3FA");
                palette.ChipHover = ParseColor("#E4E8F6");
                palette.TimerBar = ParseColor("#C7CDE8");
                palette.PageBg = ParseColor("#F6F7FB");
                palette.Sidebar = ParseColor("#F0F2F6");
                // 数据工作表：与设计稿（GeoChemistry_Nexus_CIPW 工作表）逐值一致
                palette.SheetHighlight = ParseColor("#E3E8FB");
                palette.SheetOutput = ParseColor("#F5F7FF");
                palette.SheetOutputBand = ParseColor("#F1F3FF");
                palette.SheetInputBand = ParseColor("#FBFCFE");
                palette.SheetSeparator = ParseColor("#ECEFF6");
                palette.SheetHeaderText = ParseColor("#3D4566");
                palette.SheetRowHeader = ParseColor("#FAFBFD");
                palette.SheetGridLine = ParseColor("#F1F3F8");
            }

            else if (def.Id == "teal")
            {
                // 自动提亮后的青色饱和度过高、文字链接刺眼，改为降饱和的柔和青
                palette.Light = ParseColor("#2B8486");
                palette.Lighter = ParseColor("#59A5A6");
            }
            else if (def.Id == "green")
            {
                // 同上：降低绿色文字链接的饱和度，更接近自然的墨绿
                palette.Light = ParseColor("#3A8660");
                palette.Lighter = ParseColor("#62A885");
            }

            return palette;
        }

        private static SheetColors ToSheetColors(Palette p) => new()
        {
            Accent = p.Accent,
            Highlight = p.SheetHighlight,
            OutputBack = p.SheetOutput,
            OutputBand = p.SheetOutputBand,
            InputBand = p.SheetInputBand,
            Separator = p.SheetSeparator,
            HeaderText = p.SheetHeaderText,
            RowHeaderBack = p.SheetRowHeader,
            GridLine = p.SheetGridLine,
            HeaderLine = p.Line,
            Mute = p.Mute,
            Sub = p.Sub,
        };

        private static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

        private static (double H, double S, double L) ToHsl(Color color)
        {
            double r = color.R / 255d, g = color.G / 255d, b = color.B / 255d;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double l = (max + min) / 2d;
            double d = max - min;
            if (d < 1e-9)
                return (0, 0, l);

            double s = d / (1d - Math.Abs(2d * l - 1d));
            double h;
            if (max == r)
                h = ((g - b) / d) % 6d;
            else if (max == g)
                h = (b - r) / d + 2d;
            else
                h = (r - g) / d + 4d;

            h *= 60d;
            if (h < 0)
                h += 360d;
            return (h, s, l);
        }

        private static Color FromHsl(double h, double s, double l)
        {
            s = Math.Clamp(s, 0d, 1d);
            l = Math.Clamp(l, 0d, 1d);
            double c = (1d - Math.Abs(2d * l - 1d)) * s;
            double hp = (((h % 360d) + 360d) % 360d) / 60d;
            double x = c * (1d - Math.Abs(hp % 2d - 1d));
            double r1 = 0, g1 = 0, b1 = 0;
            if (hp < 1) { r1 = c; g1 = x; }
            else if (hp < 2) { r1 = x; g1 = c; }
            else if (hp < 3) { g1 = c; b1 = x; }
            else if (hp < 4) { g1 = x; b1 = c; }
            else if (hp < 5) { r1 = x; b1 = c; }
            else { r1 = c; b1 = x; }

            double m = l - c / 2d;
            return Color.FromRgb(
                (byte)Math.Round((r1 + m) * 255d),
                (byte)Math.Round((g1 + m) * 255d),
                (byte)Math.Round((b1 + m) * 255d));
        }
    }
}
