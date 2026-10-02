using GeoChemistryNexus.Helpers.PlotMarkers;
using GeoChemistryNexus.Models;
using System.Windows.Media;

namespace GeoChemistryNexus.ViewModels
{
    public class PlotMarkerShapeItem
    {
        public PlotMarkerShape Shape { get; }
        public Geometry Icon { get; }
        public bool IsFilled { get; }

        public PlotMarkerShapeItem(PlotMarkerShape shape, Geometry icon)
        {
            Shape = shape;
            Icon = icon;
            IsFilled = PlotMarkerStyleApplier.IsFilled(shape);
        }
    }
}
