using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public class TreeNode
    {
        // 节点名称
        public string Name { get; set; }       
        // 节点文件路径
        public string BaseMapPath { get; set; }
        // 节点树路径
        public string[] rootNode { get; set; }
        // 弃用：模型底图
        public PlotTemplate PlotTemplate { get; set; }
        public ObservableCollection<TreeNode> Children { get; } = new ObservableCollection<TreeNode>();
    }
}
