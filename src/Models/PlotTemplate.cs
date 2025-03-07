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
        // 之前是存储指南文件 rtf 的位置，现在如果是 json 就存储的 json 的位置，json包含了 rtf 数据
        public string Description { get; set; }
        public string[] RequiredElements { get; set; }
    }
}
