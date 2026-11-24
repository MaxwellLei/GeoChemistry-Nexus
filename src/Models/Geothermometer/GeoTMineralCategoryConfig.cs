using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 官方温压计标签多语言配置（GeoTMineralCategories.json，单层的多语言标签列表）。
    /// </summary>
    public class GeoTMineralCategoryConfig
    {
        public List<Dictionary<string, string>> Minerals { get; set; } = new();
    }
}
