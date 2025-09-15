using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 底图信息核心类
    /// </summary>
    public class GraphMapInfo
    {
        /// <summary>
        /// 点列表
        /// </summary>
        public List<PointDefinition> Points { get; set; } = new List<PointDefinition>();

        /// <summary>
        /// 线列表
        /// </summary>
        public List<LineDefinition> Lines { get; set; } = new List<LineDefinition>();

        /// <summary>
        /// 多边形列表
        /// </summary>
        public List<PolygonDefinition> Polygons { get; set; } = new List<PolygonDefinition>();

        /// <summary>
        /// 文本列表
        /// </summary>
        public List<TextDefinition> Texts { get; set; } = new List<TextDefinition>();

        /// <summary>
        /// 注释列表
        /// </summary>
        public List<AnnotationDefinition> Annotations { get; set; } = new List<AnnotationDefinition>();

        /// <summary>
        /// 坐标轴列表
        /// </summary>
        public List<BaseAxisDefinition> Axes { get; set; } = new List<BaseAxisDefinition>();

        /// <summary>
        /// 绘图标题
        /// </summary>
        public TitleDefinition Title { get; set; } = new TitleDefinition();

        /// <summary>
        /// 网格设置
        /// </summary>
        public GridDefinition Grid { get; set; } = new GridDefinition();

        /// <summary>
        /// 图例
        /// </summary>
        public LegendDefinition Legend { get; set; } = new LegendDefinition();
    }
}
