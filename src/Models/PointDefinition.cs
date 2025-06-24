using System.ComponentModel;

namespace GeoChemistryNexus.Models
{
    public class PointDefinition
    {
        // X 坐标
        [Category("Coordinates")] // 坐标
        [DisplayName("X Coordinate")] // X 坐标
        public double X { get; set; } = 0;

        // Y 坐标
        [Category("Coordinates")] // 坐标
        [DisplayName("Y Coordinate")] // Y 坐标
        public double Y { get; set; } = 0;
    }
}