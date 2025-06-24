using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// TreeView 中所有图层项的抽象基类
    /// </summary>
    public abstract partial class LayerItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name; // 图层项的显示名称

        [ObservableProperty]
        private bool _isVisible = true; // 控制该图层是否在ScottPlot图表上可见

        [ObservableProperty]
        private bool _isSelected;       // 控制选中状态

        [ObservableProperty]
        private bool _isExpanded = false; // 控制TreeView节点是否展开

        public IPlottable? Plottable { get; set; }      // 绘图对象

        /// <summary>
        /// 子图层/子项目集合
        /// </summary>
        public ObservableCollection<LayerItemViewModel> Children { get; } = new ObservableCollection<LayerItemViewModel>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">图层名称</param>
        protected LayerItemViewModel(string name)
        {
            _name = name;
        }

        // IsVisible 状态变化,触发图表重绘
        partial void OnIsVisibleChanged(bool value)
        {
            // 当父项的可见性改变时，递归地更新所有子项的可见性
            foreach (var child in Children)
            {
                child.IsVisible = value;
            }
        }
    }
}
