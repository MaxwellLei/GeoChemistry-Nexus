using GeoChemistryNexus.Converter;
using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// 背景可以是主题色、自定义颜色，或一张网络图片。没有图片时主页固定绘制同一套品牌图形。
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
        /// 这条公告对哪些界面语言展示。空或未填写表示所有语言都展示。
        /// </summary>
        [JsonPropertyName("languages")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Languages { get; set; }

        /// <summary>
        /// 预设主题名，见 <see cref="AnnouncementBrushes.ThemeKeys"/>。未填写自定义颜色时映射到内置渐变，未知值回退 blue。
        /// </summary>
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "blue";

        /// <summary>
        /// 背景方式：default（颜色）或 image（网络图片）。
        /// </summary>
        [JsonPropertyName("backgroundMode")]
        public string BackgroundMode { get; set; } = HomeAnnouncementBackground.ModeDefault;

        /// <summary>
        /// 自定义背景色（#RRGGBB）。仅 default 模式使用；为空时回退到 <see cref="Theme"/>。
        /// </summary>
        [JsonPropertyName("backgroundColor")]
        public string BackgroundColor { get; set; } = string.Empty;

        /// <summary>
        /// 本版本模型里没有的字段。重新发布时原样写回，避免丢掉更新版本新增的参数。
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
        /// 当前界面语言是否应该看到这条公告。未指定语言时所有语言都展示。
        /// </summary>
        public bool IsVisibleForLanguage(string? language)
            => HomeAnnouncementLanguages.IsVisible(Languages, language);

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
    /// 公告背景的取值约定。服务端只存短字符串，具体画法由客户端决定。
    /// </summary>
    public static class HomeAnnouncementBackground
    {
        public const string ModeDefault = "default";
        public const string ModeImage = "image";

        public static bool IsImageMode(string? mode)
            => string.Equals(mode, ModeImage, StringComparison.OrdinalIgnoreCase);

        public static string NormalizeMode(string? mode)
            => IsImageMode(mode) ? ModeImage : ModeDefault;

        public static bool IsHttpImageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            string trimmed = url.Trim();
            return trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 公告展示语言。空列表表示所有界面语言都展示，便于旧目录保持原样。
    /// </summary>
    public static class HomeAnnouncementLanguages
    {
        public static List<string> Normalize(IEnumerable<string>? languages)
        {
            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (languages != null)
            {
                foreach (string code in languages)
                {
                    if (AppCultureRegistry.TryNormalize(code, out string normalized)
                        && AppCultureRegistry.IsSupportedCode(normalized))
                        selected.Add(normalized);
                }
            }

            return AppCultureRegistry.SupportedCodes.Where(selected.Contains).ToList();
        }

        public static bool IsVisible(IEnumerable<string>? languages, string? language)
        {
            var selected = Normalize(languages);
            if (selected.Count == 0)
                return true;

            string requested = AppCultureRegistry.ResolveAppLanguage(language);
            return selected.Contains(requested, StringComparer.OrdinalIgnoreCase);
        }
    }
}
