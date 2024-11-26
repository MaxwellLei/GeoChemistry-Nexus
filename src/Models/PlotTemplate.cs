using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public class PlotTemplate
    {
        public string Name { get; set; }
        public Action<ScottPlot.Plot> DrawMethod { get; set; }
        public Func<ScottPlot.Plot, DataTable, Task<int>> PlotMethod { get; set; }
        public string Description { get; set; }
        public string[] RequiredElements { get; set; }
    }
}
