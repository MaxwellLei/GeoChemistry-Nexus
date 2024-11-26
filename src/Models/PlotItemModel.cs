using ScottPlot;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public class PlotItemModel
    {
        public object Plottable { get; set; }       // 实际绘图对象
        public string Name { get; set; }     // 显示绘图对象名称
        public string ObjectType { get; set; }        // 实际绘图对象类型
    }
}
