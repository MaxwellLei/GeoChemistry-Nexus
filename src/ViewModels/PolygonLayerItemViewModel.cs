using GeoChemistryNexus.Models;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个多边形元素的图层项
    /// </summary>
    public partial class PolygonLayerItemViewModel : LayerItemViewModel
    {
        public PolygonDefinition PolygonDefinition { get; }

        public PolygonLayerItemViewModel(PolygonDefinition polygonDefinition, int index)
            : base(LanguageService.Instance["polygon"] + $" {index + 1}")
        {
            PolygonDefinition = polygonDefinition;
        }
    }
}
