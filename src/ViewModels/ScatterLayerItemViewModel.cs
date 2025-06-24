using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.ViewModels
{
    public partial class ScatterLayerItemViewModel : LayerItemViewModel
    {
        public ScatterDefinition ScatterDefinition { get; }

        public ScatterLayerItemViewModel(ScatterDefinition scatterDefinition)
            : base(LanguageService.Instance["data_point"])
        {
            ScatterDefinition = scatterDefinition;
        }
    }
}
