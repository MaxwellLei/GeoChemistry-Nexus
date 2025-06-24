using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public partial class PolygonDefinition: ObservableObject
    {
        /// <summary>
        /// 多边形顶点
        /// </summary>
        [ObservableProperty]
        private List<PointDefinition> _vertices = new List<PointDefinition>();

        /// <summary>
        /// 多边形填充颜色
        /// </summary>
        [ObservableProperty]
        private string _fillColor = "#FFFF00";

        /// <summary>
        /// 边缘线样式
        /// </summary>
        [ObservableProperty]
        private LineDefinition _edgeLine;
    }
}
