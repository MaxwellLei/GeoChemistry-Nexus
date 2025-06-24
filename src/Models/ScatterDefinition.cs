using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using GeoChemistryNexus.PropertyEditor;
using ScottPlot.Plottables;
using HandyControl.Controls;
using ScottPlot;

namespace GeoChemistryNexus.Models
{
    public partial class ScatterDefinition : ObservableObject
    {
        // 坐标位置
        [ObservableProperty]
        [property: Category("Position")] // 位置
        [property: DisplayName("Start and End Coordinates")] // 起始坐标
        [property: Editor(typeof(PointDefinitionPropertyEditor), typeof(PropertyEditorBase))]
        private PointDefinition _startAndEnd = new PointDefinition();

        /// <summary>
        /// 散点大小
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")] // 样式
        [property: DisplayName("Size")] // 大小
        private float _size = 12;

        /// <summary>
        /// 散点颜色
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")] // 样式
        [property: DisplayName("Color")] // 颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _color = "#000000";

        /// <summary>
        /// 散点类型
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")] // 样式
        [property: DisplayName("Type")] // 类型
        private MarkerShape _markerShape = MarkerShape.FilledSquare;
    }
}