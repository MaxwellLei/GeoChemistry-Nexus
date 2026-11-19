using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 构建设置页界面语言选择器的数据：按 <see cref="AppCultureRegistry.SupportedCodes"/> 的顺序，
    /// 补充每种语言的母语名、地区名与国旗。新增语言时在 <see cref="Metadata"/> 追加一项即可。
    /// </summary>
    public static class AppLanguageOptionFactory
    {
        private sealed record LanguageMeta(string NativeName, string RegionName, string? FlagKey, string? BadgeText = null);

        private static readonly Dictionary<string, LanguageMeta> Metadata =
            new(System.StringComparer.OrdinalIgnoreCase)
            {
                ["zh-CN"] = new("简体中文", "中国", "CN"),
                ["zh-TW"] = new("繁體中文", "中國臺灣", "CN"),
                ["en-US"] = new("English", "United States", "US"),
                ["de-DE"] = new("Deutsch", "Deutschland", "DE"),
                ["es-ES"] = new("español", "España", "ES"),
                ["ja-JP"] = new("日本語", "日本", "JP"),
                ["ko-KR"] = new("한국어", "대한민국", "KR"),
                ["ru-RU"] = new("русский", "Россия", "RU"),
                ["fr-FR"] = new("français", "France", "FR"),
                ["pt-BR"] = new("português", "Brasil", "BR"),
            };

        /// <summary>
        /// 创建选项列表。开发者模式下副标题末尾追加文化代码。
        /// </summary>
        public static IReadOnlyList<AppLanguageOption> Create(bool? includeCultureCode = null)
        {
            bool showCode = includeCultureCode ?? AppCultureRegistry.IsDeveloperModeEnabled();

            return AppCultureRegistry.SupportedCodes
                .Select(code => CreateOption(code, showCode))
                .ToList();
        }

        /// <summary>
        /// 语言的母语名，如 zh-CN → 简体中文、en-US → English。不附带地区名和文化代码。
        /// </summary>
        public static string GetNativeName(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            if (!AppCultureRegistry.TryNormalize(code, out string normalized))
                normalized = code.Trim();

            if (Metadata.TryGetValue(normalized, out var meta))
                return meta.NativeName;

            return AppCultureRegistry.GetDisplayName(normalized, false);
        }

        private static AppLanguageOption CreateOption(string code, bool showCode)
        {
            string localizedName = LanguageService.Instance[$"language_name_{code.Replace('-', '_')}"];
            string? extra = showCode ? code : null;

            if (Metadata.TryGetValue(code, out var meta))
            {
                return new AppLanguageOption(code, meta.NativeName, meta.RegionName, meta.FlagKey, meta.BadgeText, localizedName, extra);
            }

            // 注册表中新增但尚未补充元数据的语言：退回到文化名称，使用代码前缀作为角标
            string badge = code.Length >= 2 ? code[..2].ToUpperInvariant() : code;
            return new AppLanguageOption(code, AppCultureRegistry.GetDisplayName(code, false), string.Empty, null, badge, localizedName, extra);
        }
    }
}
