using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 温压计模板文本（名称）的多语言解析与整理。
    /// 格式 1.2 起模板在 Languages 中记录支持的语言（第一项为默认语言），Name 始终保存默认语言文本，
    /// 其它语言的名称放在 NameLang；参考文献不分语言，只使用 Reference。
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

        /// <summary>参考文献不分语言，直接使用 Reference（旧模板里的 ReferenceLang 不再参与显示）。</summary>
        public static string ResolveReference(GeothermometerEntity entity)
            => entity.Reference ?? string.Empty;

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
            entity.Groups = NormalizeGroups(entity.Headers, entity.Groups, entity.Languages);
        }

        /// <summary>是否至少有一列设置了分组标题。</summary>
        public static bool HasAnyGroup(IReadOnlyList<Dictionary<string, string>>? groups)
            => groups != null && groups.Any(group => group != null && group.Values.Any(text => !string.IsNullOrWhiteSpace(text)));

        /// <summary>
        /// 把分组列表对齐到表头列数（不足补空字典、多余截断），并清理每列的多语言标题。
        /// 没有任何非空标题时返回空列表，使保存、导出和哈希与没有分组的旧模板内容完全一致。
        /// </summary>
        public static List<Dictionary<string, string>> NormalizeGroups(
            IReadOnlyList<string>? headers,
            IReadOnlyList<Dictionary<string, string>>? groups,
            IReadOnlyList<string>? languages)
        {
            if (headers == null || !HasAnyGroup(groups))
                return new List<Dictionary<string, string>>();

            // 模板没有声明语言（异常文件）时，沿用分组自身出现的语言，避免标题被整体丢弃。
            IReadOnlyList<string> order = languages is { Count: > 0 }
                ? languages
                : groups!.Where(group => group != null).SelectMany(group => group.Keys).Distinct(StringComparer.Ordinal).ToList();

            var result = new List<Dictionary<string, string>>(headers.Count);
            for (int i = 0; i < headers.Count; i++)
                result.Add(Compact(i < groups!.Count ? groups[i] : null, order));
            return result;
        }

        /// <summary>
        /// 按当前界面语言解出每列的分组标题（界面语言 → 模板默认语言 → en-US → 任意语言），没有分组的列为空串。
        /// 结果与表头等长；完全没有分组时返回空列表。
        /// </summary>
        public static List<string> ResolveGroups(
            IReadOnlyList<string>? headers,
            IReadOnlyList<Dictionary<string, string>>? groups,
            IReadOnlyList<string>? languages)
        {
            if (headers == null || !HasAnyGroup(groups))
                return new List<string>();

            var result = new List<string>(headers.Count);
            for (int i = 0; i < headers.Count; i++)
                result.Add(i < groups!.Count && groups[i] != null ? Resolve(groups[i], languages, string.Empty) : string.Empty);
            return result;
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
