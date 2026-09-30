using GeoChemistryNexus.Converter;
using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 主页公告目录（CDN 下发 Announcements.json 的根对象）
    /// </summary>
    public class HomeAnnouncementCatalog
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("announcements")]
        public List<HomeAnnouncementEntry> Announcements { get; set; } = new();
    }

    /// <summary>
    /// 单条公告。文案全部为结构化多语言纯文本。
    /// 背景可以是主题色或自定义颜色（可选装饰图形），也可以是一张网络图片。
    /// </summary>
    public class HomeAnnouncementEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 眉题标签，例如 “产品更新” / “社区活动”
        /// </summary>
        [JsonPropertyName("tag")]
        [JsonConverter(typeof(LocalizedStringJsonConverter))]
        public LocalizedString Tag { get; set; } = new();

        [JsonPropertyName("title")]
        [JsonConverter(typeof(LocalizedStringJsonConverter))]
        public LocalizedString Title { get; set; } = new();

        [JsonPropertyName("body")]
        [JsonConverter(typeof(LocalizedStringJsonConverter))]
        public LocalizedString Body { get; set; } = new();

        /// <summary>
        /// 展示用发布日期（yyyy-MM-dd）
        /// </summary>
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// 上架时间（含），为空表示立即生效
        /// </summary>
        [JsonPropertyName("startDate")]
        public string StartDate { get; set; } = string.Empty;

        /// <summary>
        /// 下架时间（含），为空表示长期有效
        /// </summary>
        [JsonPropertyName("endDate")]
        public string EndDate { get; set; } = string.Empty;

        /// <summary>
        /// 数值越大越靠前
        /// </summary>
        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// 预设主题名：blue / purple / teal / orange。未填写自定义颜色时映射到内置渐变，未知值回退 blue。
        /// </summary>
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "blue";

        /// <summary>
        /// 背景方式：default（颜色 + 可选装饰图形）或 image（网络图片）。
        /// </summary>
        [JsonPropertyName("backgroundMode")]
        public string BackgroundMode { get; set; } = HomeAnnouncementBackground.ModeDefault;

        /// <summary>
        /// 自定义背景色（#RRGGBB）。仅 default 模式使用；为空时回退到 <see cref="Theme"/>。
        /// </summary>
        [JsonPropertyName("backgroundColor")]
        public string BackgroundColor { get; set; } = string.Empty;

        /// <summary>
        /// default 模式下是否绘制装饰多边形。图片背景不绘制。
        /// </summary>
        [JsonPropertyName("showPolygons")]
        public bool ShowPolygons { get; set; } = true;

        /// <summary>
        /// 装饰图形的样式名。为空表示按条目顺序在本版本已知样式间轮换。
        /// 未知名称（更新版本新增的样式）会原样保留，当前版本绘制时回退到已知图形。
        /// </summary>
        [JsonPropertyName("polygonStyle")]
        public string PolygonStyle { get; set; } = string.Empty;

        /// <summary>
        /// 本版本模型里没有的字段。重新发布时原样写回，避免丢掉更新版本的样式参数。
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        /// <summary>
        /// 自定义背景图的网络地址（http/https）。backgroundMode 为 image 时铺满卡片。
        /// </summary>
        [JsonPropertyName("backgroundImageUrl")]
        public string BackgroundImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public HomeAnnouncementAction? Action { get; set; }

        /// <summary>
        /// 当前时间是否在展示窗口内
        /// </summary>
        public bool IsActive(DateTime now)
        {
            if (TryParseDate(StartDate, out var start) && now.Date < start.Date)
                return false;

            if (TryParseDate(EndDate, out var end) && now.Date > end.Date)
                return false;

            return true;
        }

        private static bool TryParseDate(string value, out DateTime result)
        {
            result = default;
            return !string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, out result);
        }
    }

    /// <summary>
    /// 公告动作按钮（可选）。长内容通过 Url 跳转，卡片本身只放短文案。
    /// </summary>
    public class HomeAnnouncementAction
    {
        [JsonPropertyName("label")]
        [JsonConverter(typeof(LocalizedStringJsonConverter))]
        public LocalizedString Label { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 本版本能够绘制的一种公告装饰图形。
    /// </summary>
    public sealed class HomeAnnouncementPolygonStyle
    {
        public HomeAnnouncementPolygonStyle(string id, string label)
        {
            Id = id;
            Label = label;
        }

        public string Id { get; }

        public string Label { get; }
    }

    /// <summary>
    /// 公告背景的取值约定。服务端只存短字符串，具体画法由客户端决定。
    /// 多边形样式以 <see cref="HomeAnnouncementBackground.KnownPolygonStyles"/> 为闭集：
    /// 能识别的才绘制；不认识的名称留给更新版本，当前版本回退到已知图形。
    /// </summary>
    public static class HomeAnnouncementBackground
    {
        public const string ModeDefault = "default";
        public const string ModeImage = "image";
        public const string PolygonStyleAuto = "auto";

        public const string PolygonRing = "ring";
        public const string PolygonTile = "tile";
        public const string PolygonCircles = "circles";
        public const string PolygonDots = "dots";
        public const string PolygonDiamond = "diamond";
        public const string PolygonHex = "hex";
        public const string PolygonArc = "arc";
        public const string PolygonBars = "bars";

        /// <summary>
        /// 本版本会绘制的样式。新增图形时只扩展这里，并在主页补上对应画布。
        /// </summary>
        public static IReadOnlyList<HomeAnnouncementPolygonStyle> KnownPolygonStyles { get; } = new[]
        {
            new HomeAnnouncementPolygonStyle(PolygonRing, "圆环"),
            new HomeAnnouncementPolygonStyle(PolygonTile, "圆角方块"),
            new HomeAnnouncementPolygonStyle(PolygonCircles, "同心圆"),
            new HomeAnnouncementPolygonStyle(PolygonDots, "散点"),
            new HomeAnnouncementPolygonStyle(PolygonDiamond, "菱形"),
            new HomeAnnouncementPolygonStyle(PolygonHex, "六边形"),
            new HomeAnnouncementPolygonStyle(PolygonArc, "弧线"),
            new HomeAnnouncementPolygonStyle(PolygonBars, "条纹")
        };

        public static bool IsImageMode(string? mode)
            => string.Equals(mode, ModeImage, StringComparison.OrdinalIgnoreCase);

        public static string NormalizeMode(string? mode)
            => IsImageMode(mode) ? ModeImage : ModeDefault;

        public static bool IsKnownPolygonStyle(string? style)
            => TryGetKnownPolygonStyle(style, out _);

        /// <summary>
        /// 写入目录时：已知样式规范为小写 id；自动存空字符串；未知非空名称原样保留。
        /// </summary>
        public static string NormalizePolygonStyle(string? style)
        {
            if (string.IsNullOrWhiteSpace(style))
                return string.Empty;

            string trimmed = style.Trim();
            if (string.Equals(trimmed, PolygonStyleAuto, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (TryGetKnownPolygonStyle(trimmed, out string canonical))
                return canonical;

            return trimmed;
        }

        /// <summary>
        /// 得到本版本实际要画的样式。未指定或来自更新版本的未知名称，按顺序回退到已知图形。
        /// </summary>
        public static string ResolvePolygonStyle(string? style, int index)
        {
            if (TryGetKnownPolygonStyle(style, out string canonical))
                return canonical;

            var known = KnownPolygonStyles;
            if (known.Count == 0)
                return PolygonRing;

            int count = known.Count;
            int safeIndex = index % count;
            if (safeIndex < 0)
                safeIndex += count;

            return known[safeIndex].Id;
        }

        public static bool TryGetKnownPolygonStyle(string? style, out string canonical)
        {
            canonical = string.Empty;
            if (string.IsNullOrWhiteSpace(style))
                return false;

            string trimmed = style.Trim();
            foreach (var item in KnownPolygonStyles)
            {
                if (string.Equals(item.Id, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = item.Id;
                    return true;
                }
            }

            return false;
        }

        public static bool IsHttpImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            string trimmed = url.Trim();
            return trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
