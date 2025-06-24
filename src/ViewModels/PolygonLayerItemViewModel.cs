using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个多边形元素的图层项
    /// </summary>
    public partial class PolygonLayerItemViewModel : LayerItemViewModel
    {
        public PolygonDefinition PolygonDefinition { get; }

        public PolygonLayerItemViewModel(PolygonDefinition polygonDefinition, int index)
            : base($"多边形 {index + 1}")
        {
            PolygonDefinition = polygonDefinition;
        }
    }
}
