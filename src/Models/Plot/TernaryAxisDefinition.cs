using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
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
        private double _labelOffsetX;

        /// <summary>
        /// Label offset Y
        /// </summary>
        [ObservableProperty]
        private double _labelOffsetY;

        /// <summary>
        /// Show tick labels
        /// </summary>
        [ObservableProperty]
        private bool _isShowTickLabels = true;

        /// <summary>
        /// Show major ticks
        /// </summary>
        [ObservableProperty]
        private bool _isShowMajorTicks = true;
    }
}
