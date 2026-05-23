using System.Collections.Generic;

namespace GeoChemistryNexus.Models.SpiderDiagram
{
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
        /// 元素标准化值字典 (元素符号 -> 参考值, ppm)
        /// </summary>
        public Dictionary<string, double> Values { get; set; } = new Dictionary<string, double>();
    }
}
