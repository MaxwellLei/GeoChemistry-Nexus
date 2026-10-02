using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    // 使用字典来存储动态层级配置
    // 键："Level1"、"Level2"等  
    // 值：本地化名称字典的列表
    public class PlotTemplateCategoryConfig : Dictionary<string, List<Dictionary<string, string>>>
    {
    }
}
