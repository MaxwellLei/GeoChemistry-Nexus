using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Converter;
using GeoChemistryNexus.PropertyEditor;
using System.ComponentModel;
using HandyControl.Controls;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    // 箭头绘图对象
    public partial class ArrowDefinition : ObservableObject
    {
        /// <summary>
        /// 箭头起始点（基点）
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("position")]      // 分类：位置
        [property: LocalizedDisplayName("start_coordinates")]     // 显示名称：起始坐标
        [property: Editor(typeof(PointDefinitionPropertyEditor), typeof(PropertyEditorBase))]
        private PointDefinition _start = new PointDefinition();

        /// <summary>
        /// 箭头终止点（尖端）
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("position")]      // 分类：位置
        [property: LocalizedDisplayName("end_coordinates")]      // 显示名称：终止坐标
        [property: Editor(typeof(PointDefinitionPropertyEditor), typeof(PropertyEditorBase))]
        private PointDefinition _end = new PointDefinition();

        /// <summary>
        /// 箭头线条和填充颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 分类：样式
        [property: LocalizedDisplayName("color")]        // 显示名称：颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _color = "#000000";

        /// <summary>
        /// 箭头边框宽度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 分类：样式
        [property: LocalizedDisplayName("line_width")]    // 显示名称：边框宽度
        private float _arrowWidth = 0.5f;

        /// <summary>
        /// 箭头头部宽度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 样式
        [property: LocalizedDisplayName("arrowhead_width")]    // 箭头宽度
        private float _arrowheadWidth = 8f;

        /// <summary>
        /// 箭头头部长度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 样式
        [property: LocalizedDisplayName("arrowhead_length")]    // 箭头长度
        private float _arrowheadLength = 12f;
    }
}
