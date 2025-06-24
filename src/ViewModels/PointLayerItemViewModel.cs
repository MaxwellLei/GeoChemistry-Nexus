using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个点元素的图层项
    /// </summary>
    public partial class PointLayerItemViewModel : LayerItemViewModel
    {
        public PointDefinition PointDefinition { get; }

        public PointLayerItemViewModel(PointDefinition pointDefinition, int index): base($"点 {index + 1}")
        {
            PointDefinition = pointDefinition;
        }
    }
}
