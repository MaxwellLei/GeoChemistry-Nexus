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
        public IPlottable Plottable { get; set; }       // 实际绘图对象
        public string DisplayName { get; set; }     // 显示绘图对象名称
        public string TypeName { get; set; }        // 实际绘图对象类型
        public object Tag { get; set; }     // 保存的实际值，在线中表示线宽
    }
}
