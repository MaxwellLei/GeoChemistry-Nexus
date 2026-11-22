using System;
using System.Collections.Generic;
using System.Linq;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 数据表格的一个列分组（图解脚本格式 1.3 新增，可选）：一个多语言标题加上它覆盖的数据列。
    /// 表格表头里，相邻且属于同一分组的列在列名上方合并成一条分组带，带上显示按图解内容语言解析出的标题。
    /// 分组以列名引用数据列（<see cref="ScriptDefinition.RequiredDataSeries"/> 中的条目），所以列的增删、重排不会让对应关系错位。
    /// </summary>
    public class DataSeriesGroup
    {
        /// <summary>分组标题：Default 为模板默认语言代码，各语言的标题放在 Translations 中。</summary>
        public LocalizedString Title { get; set; } = new LocalizedString();

        /// <summary>分组包含的数据列名（与 RequiredDataSeries 中的列名一致，不区分大小写）。</summary>
        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>
        /// 按语言取标题：该语言 → 模板默认语言 → 任意已填写的语言，翻译到一半时分组带也不会出现空白。
        /// </summary>
        public string ResolveTitle(string? language)
        {
            var translations = Title?.Translations;
            if (translations == null || translations.Count == 0) return string.Empty;

            if (!string.IsNullOrEmpty(language)
                && translations.TryGetValue(language, out var requested)
                && !string.IsNullOrWhiteSpace(requested))
                return requested.Trim();

            if (!string.IsNullOrEmpty(Title!.Default)
                && translations.TryGetValue(Title.Default, out var fallback)
                && !string.IsNullOrWhiteSpace(fallback))
                return fallback.Trim();

            return translations.Values.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))?.Trim() ?? string.Empty;
        }

        /// <summary>该分组是否包含指定数据列（不区分大小写）。</summary>
        public bool Contains(string? column)
            => !string.IsNullOrWhiteSpace(column)
               && Columns != null
               && Columns.Any(item => string.Equals(item?.Trim(), column.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
