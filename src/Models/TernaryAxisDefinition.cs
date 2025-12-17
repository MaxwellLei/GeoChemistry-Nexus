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
    }
}
