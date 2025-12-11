using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    public class PlotTemplateCategory
    {
        public Dictionary<string, string> Names { get; set; }
        public List<PlotTemplateCategory> Children { get; set; }
    }
}
