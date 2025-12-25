using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using GeoChemistryNexus.Data.Language;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 用于从资源文件创建带有占位符的 LocalizedString 对象的工厂类
    /// </summary>
    public static class LocalizedPlaceholderFactory
    {
        // 占位符支持的语言列表
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
        /// 根据给定的资源键创建一个包含对应翻译的 LocalizedString 对象
        /// </summary>
        /// <param name="resourceKey">资源文件中的键（例如 "Placeholder_Text"）。</param>
        /// <param name="defaultLanguage">默认语言代码。如果为 null，则使用当前应用程序语言。</param>
        /// <param name="targetLanguages">可选的需要填充的语言列表。如果为 null，则使用所有支持的语言。</param>
        /// <returns>已填充的 LocalizedString 对象。</returns>
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
                    // 如果为 null，回退到英语
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
                    // 忽略无效的 CultureInfo 或缺失的资源
                    // 尝试回退到英语键值
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
        /// 获取支持语言列表的辅助属性，以备他用。
        /// </summary>
        public static IReadOnlyList<string> SupportedLanguages => _supportedLanguages.AsReadOnly();
    }
}
