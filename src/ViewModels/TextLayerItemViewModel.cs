using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public class TextLayerItemViewModel : LayerItemViewModel
    {
        public TextDefinition TextDefinition { get; }

        public TextLayerItemViewModel(TextDefinition textDefinition, int index)
            : base(LanguageService.Instance["text"] + $" {index + 1}")
        {
            TextDefinition = textDefinition;
        }
    }
}
