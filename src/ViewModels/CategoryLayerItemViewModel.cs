using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表一个分类的父图层节点
    /// </summary>
    public partial class CategoryLayerItemViewModel : LayerItemViewModel
    {
        public CategoryLayerItemViewModel(string name) : base(name)
        {
            IsExpanded = true; // 默认展开分类节点
        }
    }
}
