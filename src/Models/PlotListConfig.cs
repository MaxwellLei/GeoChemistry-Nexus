using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    // 列表对象
    public class PlotListConfig
    {
        // 节点信息
        public List<ListNodeConfig> listNodeConfigs { get; set; }
    }

    // 单独对象
    public class ListNodeConfig
    {
        // 节点信息
        public string[] rootNode { get; set; }
        // 模板底图路径 其实就是文件名称
        public string baseMapPath { get; set; }
    }
}
