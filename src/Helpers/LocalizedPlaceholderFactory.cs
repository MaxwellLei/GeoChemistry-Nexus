using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using GeoChemistryNexus.Data.Language;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 用于从资源创建带占位符的 LocalizedString 对象的工厂 
    /// </summary>
    public static class LocalizedPlaceholderFactory
    {
        // 定义占位符支持的语言
        private static readonly List<string> _supportedLanguages = new List<string>
        {
            "en-US",
            "zh-CN",
            "zh-TW",
            "ja-JP",
            "ru-RU",
            "ko-KR",
            "de-DE",
            "es-ES"
        };

        private static readonly ResourceManager _resourceManager = 
            new ResourceManager("GeoChemistryNexus.Data.Language.Language", typeof(LanguageService).Assembly);

        /// <summary>
        /// 根据给定的资源键，创建一个已填充对应翻译的 LocalizedString 对象
        /// </summary>
        /// <param name="resourceKey">The key in the resource file (e.g., "Placeholder_Text").</param>
        /// <param name="defaultLanguage">The default language code. If null, uses the current application language.</param>
        /// <param name="targetLanguages">Optional list of languages to populate. If null, uses all supported languages.</param>
        /// <returns>A populated LocalizedString object.</returns>
        public static LocalizedString Create(string resourceKey, string defaultLanguage = null, IEnumerable<string> targetLanguages = null)
        {
            if (string.IsNullOrEmpty(defaultLanguage))
            {
                defaultLanguage = LanguageService.CurrentLanguage;
            }

            var languagesToProcess = targetLanguages ?? _supportedLanguages;

            var localizedString = new LocalizedString
            {
                Default = defaultLanguage,
                Translations = new Dictionary<string, string>()
            };

            foreach (var langCode in languagesToProcess)
            {
                try
                {
                    var culture = new CultureInfo(langCode);
                    var value = _resourceManager.GetString(resourceKey, culture);

                    // 如果资源缺失，GetString 会返回 null
                    // 若返回 null，回退到英文
                    if (string.IsNullOrEmpty(value))
                    {
                        value = _resourceManager.GetString(resourceKey, new CultureInfo("en-US"));
                    }

                    if (!string.IsNullOrEmpty(value))
                    {
                        localizedString.Translations[langCode] = value;
                    }
                }
                catch (Exception)
                {
                    // 处理 CultureInfo 无效或资源缺失的情况  
                    // 如果可能，回退到英文键值
                    var fallbackValue = _resourceManager.GetString(resourceKey, new CultureInfo("en-US"));
                    if (!string.IsNullOrEmpty(fallbackValue))
                    {
                        localizedString.Translations[langCode] = fallbackValue;
                    }
                }
            }

            return localizedString;
        }

        /// <summary>
        /// Helper to get the list of supported languages if needed elsewhere.
        /// </summary>
        public static IReadOnlyList<string> SupportedLanguages => _supportedLanguages.AsReadOnly();
    }
}
