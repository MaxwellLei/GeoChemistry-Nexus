using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个线元素的图层项
    /// </summary>
    public partial class LineLayerItemViewModel : LayerItemViewModel
    {
        public LineDefinition LineDefinition { get; }

        public LineLayerItemViewModel(LineDefinition lineDefinition, int index)
            : base(LanguageService.Instance["line"] + $" {index + 1}")
        {
            LineDefinition = lineDefinition;
        }
    }
}
