using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using ScottPlot;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    public partial class LegendDefinition : ObservableObject
    {
        /// <summary>
        /// 图例位置
        /// </summary>
        [ObservableProperty]
        private Alignment _alignment = Alignment.UpperRight;

        /// <summary>
        /// 图例条目排列方式
        /// </summary>
        [ObservableProperty]
        private Orientation _orientation = Orientation.Horizontal;

        /// <summary>
        /// 图例是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isVisible = true;

        /// <summary>
        /// 字体
        /// </summary>
        [ObservableProperty]
        private string _font = "Arial";
    }
}