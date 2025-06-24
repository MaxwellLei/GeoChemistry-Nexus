using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    public class GraphMapTemplate
    {
        /// <summary>
        /// 底图版本
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 底图类型：笛卡尔坐标系(Cartesian)，三元坐标系(Ternary)
        /// </summary>
        public string TemplateType { get; set; } = "Cartesian";

        /// <summary>
        /// 底图的分类
        /// </summary>
        public LocalizedString NodeList { get; set; } = new LocalizedString();

        /// <summary>
        /// 底图信息
        /// </summary>
        public GraphMapInfo Info { get; set; } = new GraphMapInfo();

        /// <summary>
        /// 脚本信息
        /// </summary>
        public ScriptDefinition Script { get; set; } = new ScriptDefinition();
    }
}
