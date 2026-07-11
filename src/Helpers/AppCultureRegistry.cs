using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    public enum CultureScope
    {
        AppUi,
        Content,
        Both
    }

    public sealed record CultureDescriptor(string Code, CultureScope Scope, int SortOrder);

    public sealed record CultureOption(string Code, string DisplayName);

    /// <summary>
    /// 全项目唯一的文化代码注册表（BCP 47 / CultureInfo.Name）。
    /// </summary>
    public static class AppCultureRegistry
    {
        public const string DefaultAppLanguage = "en-US";
        public const string DefaultContentLanguage = "en-US";

        private static readonly IReadOnlyList<CultureDescriptor> Descriptors = new[]
        {
            new CultureDescriptor("zh-CN", CultureScope.Both, 0),
            new CultureDescriptor("zh-TW", CultureScope.Both, 1),
            new CultureDescriptor("en-US", CultureScope.Both, 2),
            new CultureDescriptor("de-DE", CultureScope.Both, 3),
            new CultureDescriptor("es-ES", CultureScope.Both, 4),
            new CultureDescriptor("ja-JP", CultureScope.Both, 5),
            new CultureDescriptor("ko-KR", CultureScope.Both, 6),
            new CultureDescriptor("ru-RU", CultureScope.Both, 7),
            new CultureDescriptor("fr-FR", CultureScope.Content, 8),
            new CultureDescriptor("pt-BR", CultureScope.Content, 9),
        };

        private static readonly HashSet<string> AppUiCodeSet = Descriptors
            .Where(d => d.Scope is CultureScope.AppUi or CultureScope.Both)
            .Select(d => d.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ContentCodeSet = Descriptors
            .Where(d => d.Scope is CultureScope.Content or CultureScope.Both)
            .Select(d => d.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> CustomDisplayNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["zh-CN"] = "简体中文（中国）",
                ["zh-TW"] = "繁体中文（中國臺灣）",
            };

        public static IReadOnlyList<string> AppUiCodes { get; } = Descriptors
            .Where(d => d.Scope is CultureScope.AppUi or CultureScope.Both)
            .OrderBy(d => d.SortOrder)
            .Select(d => d.Code)
            .ToList();

        public static IReadOnlyList<string> ContentCodes { get; } = Descriptors
            .Where(d => d.Scope is CultureScope.Content or CultureScope.Both)
            .OrderBy(d => d.SortOrder)
            .Select(d => d.Code)
            .ToList();

        public static IReadOnlyList<CultureOption> GetAppUiOptions(bool? includeCultureCode = null)
        {
            return AppUiCodes
                .Select(code => new CultureOption(code, GetDisplayName(code, includeCultureCode)))
                .ToList();
        }

        public static IReadOnlyList<CultureOption> GetContentOptions(bool? includeCultureCode = null)
        {
            return ContentCodes
                .Select(code => new CultureOption(code, GetDisplayName(code, includeCultureCode)))
                .ToList();
        }

        public static bool IsDeveloperModeEnabled()
        {
            return bool.TryParse(ConfigHelper.GetConfig("developer_mode"), out bool enabled) && enabled;
        }

        public static string GetDisplayName(string? code, bool? includeCultureCode = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            if (!TryNormalize(code, out string normalized))
                return code;

            bool showCode = includeCultureCode ?? IsDeveloperModeEnabled();

            if (CustomDisplayNames.TryGetValue(normalized, out string? customName))
                return showCode ? $"{customName} ({normalized})" : customName;

            try
            {
                var culture = new CultureInfo(normalized);
                string nativeName = culture.NativeName;
                if (string.IsNullOrWhiteSpace(nativeName) ||
                    nativeName.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    return normalized;

                return showCode ? $"{nativeName} ({normalized})" : nativeName;
            }
            catch (CultureNotFoundException)
            {
                return normalized;
            }
        }

        public static bool IsValidAppUiCode(string? code)
        {
            return !string.IsNullOrWhiteSpace(code) && AppUiCodeSet.Contains(code);
        }

        public static bool IsValidContentCode(string? code)
        {
            return !string.IsNullOrWhiteSpace(code) && ContentCodeSet.Contains(code);
        }

        /// <summary>
        /// 是否为可用的内容/图解语言码：非空即可（含任意自定义字符串）。
        /// </summary>
        public static bool IsValidLanguageCode(string? code)
        {
            return TryNormalize(code, out _);
        }

        public static bool TryNormalize(string? code, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(code))
                return false;

            string trimmed = code.Trim();
            string hyphenated = trimmed.Replace('_', '-');

            // 预置码统一为注册表中的规范形式（_ → -）。
            if (AppUiCodeSet.Contains(hyphenated) || ContentCodeSet.Contains(hyphenated))
            {
                normalized = hyphenated;
                return true;
            }

            // 自定义语言：任意非空字符串，不强制 BCP 47。
            normalized = trimmed;
            return true;
        }

        public static string ResolveAppLanguage(string? requested)
        {
            if (TryNormalize(requested, out string normalized) && IsValidAppUiCode(normalized))
                return normalized;

            return DefaultAppLanguage;
        }

        /// <summary>
        /// 根据系统 UI 语言解析首次启动默认 App 语言；无法匹配时回退到 <see cref="DefaultAppLanguage"/>。
        /// </summary>
        public static string ResolveFromSystemCulture(CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentUICulture;

            for (CultureInfo? current = culture;
                 current != null && !string.IsNullOrEmpty(current.Name);
                 current = current.Parent)
            {
                if (TryMapCultureNameToAppUiCode(current.Name, out string mapped))
                    return mapped;
            }

            return DefaultAppLanguage;
        }

        private static bool TryMapCultureNameToAppUiCode(string cultureName, out string mapped)
        {
            mapped = string.Empty;
            if (string.IsNullOrWhiteSpace(cultureName))
                return false;

            string name = cultureName.Trim().Replace('_', '-');

            // 精确匹配已支持的 UI 语言码
            string? exact = AppUiCodes.FirstOrDefault(c =>
                c.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                mapped = exact;
                return true;
            }

            // 中文脚本/地区变体 → zh-CN / zh-TW
            if (name.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-SG", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "zh-CN";
                return true;
            }

            if (name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-MO", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "zh-TW";
                return true;
            }

            if (name.Equals("zh", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "zh-CN";
                return true;
            }

            // 仅语言部分匹配，例如 en-GB → en-US、de-AT → de-DE
            int dashIndex = name.IndexOf('-');
            string language = dashIndex > 0 ? name[..dashIndex] : name;
            string? byLanguage = AppUiCodes.FirstOrDefault(c =>
                c.Equals(language, StringComparison.OrdinalIgnoreCase) ||
                c.StartsWith(language + "-", StringComparison.OrdinalIgnoreCase));
            if (byLanguage != null)
            {
                mapped = byLanguage;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 解析图解展示语言：App 语言（若模板支持）→ 模板默认语言 → 任意首个可用。
        /// </summary>
        public static string ResolveDiagramDisplayLanguage(
            string? appLanguage,
            IEnumerable<string>? availableKeys,
            string? templateDefaultLanguage)
        {
            var available = NormalizeAvailableKeys(availableKeys);

            if (TryNormalize(appLanguage, out string normalizedApp) && available.Contains(normalizedApp))
                return normalizedApp;

            if (TryNormalize(templateDefaultLanguage, out string normalizedDefault) && available.Contains(normalizedDefault))
                return normalizedDefault;

            return available.FirstOrDefault()
                   ?? (TryNormalize(templateDefaultLanguage, out string fallback) ? fallback : DefaultContentLanguage);
        }

        /// <summary>
        /// 解析内容语言：requested → defaultLang → en-US → 任意首个可用（zh-TW 不回退 zh-CN）。
        /// </summary>
        public static string ResolveContentLanguage(
            string? requested,
            IEnumerable<string>? availableKeys,
            string? defaultLang)
        {
            var available = NormalizeAvailableKeys(availableKeys);

            if (TryNormalize(requested, out string normalizedRequested) &&
                available.Contains(normalizedRequested))
            {
                return normalizedRequested;
            }

            if (TryNormalize(defaultLang, out string normalizedDefault) &&
                available.Contains(normalizedDefault))
            {
                return normalizedDefault;
            }

            if (available.Contains(DefaultContentLanguage))
                return DefaultContentLanguage;

            return available.FirstOrDefault() ?? DefaultContentLanguage;
        }

        /// <summary>
        /// 从字典中按内容语言规则取文本。
        /// </summary>
        public static string GetLocalizedValue(
            IDictionary<string, string>? translations,
            string? requested,
            string? defaultLang)
        {
            if (translations == null || translations.Count == 0)
                return string.Empty;

            var available = translations.Keys.ToList();
            string resolved = ResolveContentLanguage(requested, available, defaultLang);

            if (translations.TryGetValue(resolved, out string? value) && !string.IsNullOrEmpty(value))
                return value;

            foreach (var pair in translations)
            {
                if (string.Equals(pair.Key, resolved, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(pair.Value))
                {
                    return pair.Value;
                }
            }

            return string.Empty;
        }

        private static HashSet<string> NormalizeAvailableKeys(IEnumerable<string>? availableKeys)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (availableKeys == null)
                return result;

            foreach (string key in availableKeys)
            {
                if (TryNormalize(key, out string normalized))
                    result.Add(normalized);
            }

            return result;
        }
    }
}
