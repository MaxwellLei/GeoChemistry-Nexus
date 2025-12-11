using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    // 使用字典来存储动态层级配置
    // Key: "Level1", "Level2", etc.
    // Value: List of localized name dictionaries
    public class PlotTemplateCategoryConfig : Dictionary<string, List<Dictionary<string, string>>>
    {
    }
}
