using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using ScottPlot.Plottables;
using GeoChemistryNexus.Helpers;
using ScottPlot;

namespace GeoChemistryNexus.Models
{
    public partial class ScatterDefinition : ObservableObject
    {
        /// <summary>
        /// 数据系列名称
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        // 坐标位置
        [ObservableProperty]
        private PointDefinition _startAndEnd = new PointDefinition();

        /// <summary>
        /// 散点大小
        /// </summary>
        [ObservableProperty]
        private float _size = 12;

        /// <summary>
        /// 散点颜色
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// 散点类型
        /// </summary>
        [ObservableProperty]
        private MarkerShape _markerShape = MarkerShape.FilledSquare;

        /// <summary>
        /// 描边宽度
        /// </summary>
        [ObservableProperty]
        private float _strokeWidth = 0;

        /// <summary>
        /// 描边颜色
        /// </summary>
        [ObservableProperty]
        private string _strokeColor = "#000000";
    }
}
