using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public class ArrowLayerItemViewModel : LayerItemViewModel
    {
        public ArrowDefinition ArrowDefinition { get; }

        public ArrowLayerItemViewModel(ArrowDefinition arrowDefinition, int index)
            : base(LanguageService.Instance["arrow"] + $" {index + 1}")
        {
            ArrowDefinition = arrowDefinition;
        }
    }
}
