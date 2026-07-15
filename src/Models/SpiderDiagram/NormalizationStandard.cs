using System.Collections.Generic;

namespace GeoChemistryNexus.Models.SpiderDiagram
{
    /// <summary>
    /// 标准化方案分组键（稳定标识，用于排序与多语言查找）
    /// </summary>
    public static class NormalizationCategories
    {
        public const string Chondrite = "norm_category_chondrite";
        public const string Shale = "norm_category_shale";
        public const string Crust = "norm_category_crust";
        public const string Mantle = "norm_category_mantle";
        public const string Basalt = "norm_category_basalt";

        /// <summary>
        /// 分组显示顺序
        /// </summary>
        public static readonly string[] DisplayOrder =
        {
            Chondrite,
            Shale,
            Crust,
            Mantle,
            Basalt
        };
    }

    /// <summary>
    /// 标准化方案定义
    /// </summary>
    public class NormalizationStandard
    {
        /// <summary>
        /// 方案名称（如 "Sun & McDonough 1989"）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 方案简称（如 "C1 Chondrite"）
        /// </summary>
        public string ShortName { get; set; } = string.Empty;

        /// <summary>
        /// 参考文献
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// 标准化类型：REE / TraceElement
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 分组键（对应多语言资源键，如 norm_category_chondrite）
        /// </summary>
        public string CategoryKey { get; set; } = string.Empty;

        /// <summary>
        /// 分组显示名（已本地化，供 ComboBox GroupStyle 使用）
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 方案发表年份（组内排序用）
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 是否为该图类型的推荐默认方案
        /// </summary>
        public bool IsRecommended { get; set; }

        /// <summary>
        /// 元素标准化值字典 (元素符号 -> 参考值, ppm)
        /// </summary>
        public Dictionary<string, double> Values { get; set; } = new Dictionary<string, double>();
    }
}
