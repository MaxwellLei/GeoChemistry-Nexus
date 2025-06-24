using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表图例的图层项
    /// </summary>
    public partial class LegendLayerItemViewModel : LayerItemViewModel
    {
        public LegendDefinition LegendDefinition { get; }

        public LegendLayerItemViewModel(LegendDefinition legendDefinition)
            : base(LanguageService.Instance["Legend"])
        {
            LegendDefinition = legendDefinition;
        }
    }
}
