using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public class AnnotationLayerItemViewModel : LayerItemViewModel
    {
        public AnnotationDefinition AnnotationDefinition { get; }

        public AnnotationLayerItemViewModel(AnnotationDefinition annotationDefinition, int index)
            : base(LanguageService.Instance["annotation"] + (index + 1))
        {
            AnnotationDefinition = annotationDefinition;
        }
    }
}
