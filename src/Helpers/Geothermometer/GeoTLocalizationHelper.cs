using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 温压计模板文本（名称、参考文献）的多语言解析与整理。
    /// 格式 1.2 起模板在 Languages 中记录支持的语言（第一项为默认语言），Name / Reference 始终保存默认语言文本，
    /// 其它语言的文本放在 NameLang / ReferenceLang；旧模板没有这些字段，直接使用 Name / Reference。
    /// </summary>
    public static class GeoTLocalizationHelper
    {
        /// <summary>
        /// 按当前界面语言取文本：界面语言 → 模板默认语言 → en-US → 任意语言，都没有时回退到 <paramref name="fallback"/>。
        /// </summary>
        public static string Resolve(
            IDictionary<string, string>? translations,
            IReadOnlyList<string>? languages,
            string? fallback)
        {
            string text = AppCultureRegistry.GetLocalizedValue(
                translations,
                LanguageService.CurrentLanguage,
                languages is { Count: > 0 } ? languages[0] : null);

            return string.IsNullOrWhiteSpace(text) ? fallback ?? string.Empty : text;
        }

        public static string ResolveName(GeothermometerEntity entity)
            => Resolve(entity.NameLang, entity.Languages, entity.Name);

        public static string ResolveReference(GeothermometerEntity entity)
            => Resolve(entity.ReferenceLang, entity.Languages, entity.Reference);

        /// <summary>
        /// 去掉空文本，并让默认语言排在第一位，保证写入文件和计算哈希时顺序稳定。
        /// </summary>
        public static Dictionary<string, string> Compact(
            IReadOnlyDictionary<string, string>? values,
            IReadOnlyList<string> languages)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (values == null)
                return result;

            foreach (string language in languages)
            {
                if (values.TryGetValue(language, out string? text) && !string.IsNullOrWhiteSpace(text))
                    result[language] = text.Trim();
            }

            return result;
        }

        /// <summary>
        /// 导入或读取库时清理多语言字段：语言去重去空，文本字典去掉空值。
        /// </summary>
        public static void Normalize(GeothermometerEntity entity)
        {
            entity.Languages = (entity.Languages ?? new List<string>())
                .Where(language => !string.IsNullOrWhiteSpace(language))
                .Select(language => language.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            entity.NameLang = CleanTexts(entity.NameLang);
            entity.ReferenceLang = CleanTexts(entity.ReferenceLang);
        }

        private static Dictionary<string, string> CleanTexts(IDictionary<string, string>? values)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (values == null)
                return result;

            foreach (var pair in values)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    result[pair.Key.Trim()] = pair.Value.Trim();
            }

            return result;
        }
    }
}
