using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using ScottPlot;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Attributes;

namespace GeoChemistryNexus.Models
{
    public partial class LegendDefinition : ObservableObject
    {
        /// <summary>
        /// 图例位置
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("location")] // 位置
        private Alignment _alignment = Alignment.UpperRight;

        /// <summary>
        /// 图例条目排列方式
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("orientation")] // 排列方式
        private Orientation _orientation = Orientation.Horizontal;

        /// <summary>
        /// 图例是否可见
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("is_visible")] // 是否可见
        private bool _isVisible = true;

        /// <summary>
        /// 字体
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")] // 样式
        [property: LocalizedDisplayName("font")] // 字体
        private string _font = "Arial";
    }
}