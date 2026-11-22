using System;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 数据表格中“内置列”（样品列 Sample、图解数据表的分组列 Category）的列名与多语言显示文字。
    /// 列头 <c>Text</c> 始终保持英文标识（脚本取数、按列名查找、导入导出、旧项目文件都依赖它），
    /// 只有列头“显示出来的文字”随界面语言变化，见 <see cref="GetDisplayText"/>。
    /// </summary>
    public static class BuiltInColumnHeaders
    {
        /// <summary>样品列的列标识：只标明数据来自哪个样品，不参与计算。</summary>
        public const string Sample = "Sample";

        /// <summary>图解数据表分组列的列标识：控制图层分组与图例。</summary>
        public const string Category = "Category";

        /// <summary>
        /// 取列头的显示文字：内置列（Sample / Category，忽略大小写与首尾空格）按界面语言翻译，
        /// 其它列头或资源缺失时原样返回。
        /// </summary>
        public static string GetDisplayText(string? headerText)
        {
            if (string.IsNullOrEmpty(headerText)) return headerText ?? string.Empty;

            string key = headerText.Trim();
            string? resourceKey = key.Equals(Sample, StringComparison.OrdinalIgnoreCase) ? "table_col_sample"
                : key.Equals(Category, StringComparison.OrdinalIgnoreCase) ? "table_col_category"
                : null;

            return resourceKey == null ? headerText : LanguageService.GetString(resourceKey, headerText);
        }
    }
}
