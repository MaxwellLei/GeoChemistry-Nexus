using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    // 投图底图
    public class PlotBackground
    {
        public string Name { get; set; }    // 投图名称
        public string FilePath { get; set; }  // 投图底图的文件路径
        public string Description { get; set; } // 投图底图的描述
    }
}
