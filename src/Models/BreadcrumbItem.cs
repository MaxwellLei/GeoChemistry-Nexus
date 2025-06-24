using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    // 面包屑导航项
    public class BreadcrumbItem
    {
        public string Name { get; set; }

        // 存储路径的原始节点
        public GraphMapTemplateNode Node { get; set; }
    }
}
