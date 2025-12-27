using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 三元图坐标轴
    /// </summary>
    public partial class TernaryAxisDefinition : BaseAxisDefinition
    {
        /// <summary>
        /// Label offset X
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")]
        [property: LocalizedDisplayName("offset_x")]
        private double _labelOffsetX;

        /// <summary>
        /// Label offset Y
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")]
        [property: LocalizedDisplayName("offset_y")]
        private double _labelOffsetY;

        /// <summary>
        /// Show tick labels
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("tick_labels")]
        [property: LocalizedDisplayName("show_tick_labels")]
        private bool _isShowTickLabels = true;

        /// <summary>
        /// Show major ticks
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_tick_style")]
        [property: LocalizedDisplayName("show_major_ticks")]
        private bool _isShowMajorTicks = true;
    }
}
