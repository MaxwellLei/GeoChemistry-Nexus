using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    public partial class PointDefinition : ObservableObject
    {
        // X 坐标
        [ObservableProperty]
        [property: LocalizedCategory("coordinates")] // 坐标
        [property: LocalizedDisplayName("x_coordinate")] // X 坐标
        private double _x = 0;

        // Y 坐标
        [ObservableProperty]
        [property: LocalizedCategory("coordinates")] // 坐标
        [property: LocalizedDisplayName("y_coordinate")] // Y 坐标
        private double _y = 0;

        /// <summary>
        /// 是否处于高亮状态（例如在属性面板中被聚焦）
        /// </summary>
        [ObservableProperty]
        [property: System.Text.Json.Serialization.JsonIgnore]
        private bool _isHighlighted;
    }
}