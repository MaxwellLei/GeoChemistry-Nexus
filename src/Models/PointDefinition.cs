using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    public partial class PointDefinition : ObservableObject
    {
        // X 坐标
        [ObservableProperty]
        private double _x = 0;

        // Y 坐标
        [ObservableProperty]
        private double _y = 0;

        /// <summary>
        /// 是否处于高亮状态
        /// </summary>
        [ObservableProperty]
        [System.Text.Json.Serialization.JsonIgnore]
        private bool _isHighlighted;
    }
}