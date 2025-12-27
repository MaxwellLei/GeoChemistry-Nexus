using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using ScottPlot.Plottables;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Attributes;
using ScottPlot;

namespace GeoChemistryNexus.Models
{
    public partial class ScatterDefinition : ObservableObject
    {
        // 坐标位置
        [ObservableProperty]
        [property: LocalizedCategory("position")] // 位置
        [property: LocalizedDisplayName("start_and_end_coordinates")] // 起始坐标
        private PointDefinition _startAndEnd = new PointDefinition();

        /// <summary>
        /// 散点大小
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("size")] // 大小
        private float _size = 12;

        /// <summary>
        /// 散点颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("color")] // 颜色
        private string _color = "#000000";

        /// <summary>
        /// 散点类型
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("type")] // 类型
        private MarkerShape _markerShape = MarkerShape.FilledSquare;

        /// <summary>
        /// 描边宽度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("stroke_width")] // 描边宽度
        private float _strokeWidth = 0;

        /// <summary>
        /// 描边颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("stroke_color")] // 描边颜色
        private string _strokeColor = "#000000";
    }
}