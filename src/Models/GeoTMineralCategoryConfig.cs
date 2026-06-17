using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 官方温压计标签多语言配置（GeoTMineralCategories.json，结构与图解 PlotTemplateCategories 的单层列表类似）。
    /// </summary>
    public class GeoTMineralCategoryConfig
    {
        public List<Dictionary<string, string>> Minerals { get; set; } = new();
    }
}
