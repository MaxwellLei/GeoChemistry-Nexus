using System.ComponentModel;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    public class PointDefinition
    {
        // X 坐标
        [LocalizedCategory("coordinates")] // 坐标
        [LocalizedDisplayName("x_coordinate")] // X 坐标
        public double X { get; set; } = 0;

        // Y 坐标
        [LocalizedCategory("coordinates")] // 坐标
        [LocalizedDisplayName("y_coordinate")] // Y 坐标
        public double Y { get; set; } = 0;
    }
}