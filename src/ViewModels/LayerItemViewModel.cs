using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public enum LayerTreeIconKind
    {
        Group,
        Line,
        Text,
        Polygon,
        Arrow,
        Function,
        Axis,
        AxisX,
        AxisY,
        AxisA,
        AxisB,
        AxisC,
        Point
    }

    /// <summary>
    /// TreeView 中所有图层项的抽象基类
    /// </summary>
    public abstract partial class LayerItemViewModel : ObservableObject
    {
        private LayerTreeIconKind? _customIconKind;

        [ObservableProperty]
        private string _name; // 图层项的显示名称

        /// <summary>
        /// 截断过长的名称，超过60个字符时截断并添加....
        /// </summary>
        /// <param name="name">原始名称</param>
        /// <returns>截断后的名称</returns>
        protected static string TruncateName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            const int maxLength = 60;
            if (name.Length <= maxLength)
                return name;

            return name.Substring(0, maxLength) + "....";
        }

        [ObservableProperty]
        private bool _isVisible = true; // 控制该图层是否在ScottPlot图表上可见

        [ObservableProperty]
        private bool _isSelected;       // 控制选中状态

        [ObservableProperty]
        private bool _isExpanded = false; // 控制TreeView节点是否展开

        public IPlottable? Plottable { get; set; }      // 绘图对象

        /// <summary>
        /// 通用附加数据，可用于存储关联的属性模型等
        /// </summary>
        public object? Tag { get; set; }

        /// <summary>
        /// 子图层/子项目集合
        /// </summary>
        public ObservableCollection<LayerItemViewModel> Children { get; } = new ObservableCollection<LayerItemViewModel>();

        /// <summary>
        /// 当前节点是否包含子项，用于父节点样式显示。
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>
        /// 当前节点的直属子项数量，用于图层树徽章显示。
        /// </summary>
        public int ChildCount => Children.Count;

        /// <summary>
        /// 图层列表中使用的对象图标类型
        /// </summary>
        public LayerTreeIconKind IconKind => _customIconKind ?? GetDefaultIconKind();

        /// <summary>
        /// 图层列表中是否允许删除该节点。
        /// 仅叶子节点中的非基础图层允许删除。
        /// </summary>
        public virtual bool CanDelete => this is not CategoryLayerItemViewModel and not AxisLayerItemViewModel;

        /// <summary>
        /// 是否在图层树右侧显示内联删除按钮。
        /// 默认仅数据类图层显示，其余对象可继续通过快捷键删除。
        /// </summary>
        public virtual bool ShowInlineDeleteButton => false;

        private LayerTreeIconKind GetDefaultIconKind() => this switch
        {
            CategoryLayerItemViewModel => LayerTreeIconKind.Group,
            AxisLayerItemViewModel => LayerTreeIconKind.Axis,
            LineLayerItemViewModel => LayerTreeIconKind.Line,
            TextLayerItemViewModel => LayerTreeIconKind.Text,
            AnnotationLayerItemViewModel => LayerTreeIconKind.Text,
            PolygonLayerItemViewModel => LayerTreeIconKind.Polygon,
            ArrowLayerItemViewModel => LayerTreeIconKind.Arrow,
            FunctionLayerItemViewModel => LayerTreeIconKind.Function,
            ScatterLayerItemViewModel => LayerTreeIconKind.Point,
            PointLayerItemViewModel => LayerTreeIconKind.Point,
            SpiderSampleLayerItemViewModel => LayerTreeIconKind.Line,
            _ => LayerTreeIconKind.Group
        };

        protected void SetCustomIconKind(LayerTreeIconKind iconKind)
        {
            if (_customIconKind == iconKind)
            {
                return;
            }

            _customIconKind = iconKind;
            OnPropertyChanged(nameof(IconKind));
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">图层名称</param>
        protected LayerItemViewModel(string name)
        {
            _name = name;
            Children.CollectionChanged += OnChildrenCollectionChanged;
        }

        private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasChildren));
            OnPropertyChanged(nameof(ChildCount));
        }

        // IsVisible 状态变化,触发图表重绘
        partial void OnIsVisibleChanged(bool value)
        {
            // 当父项的可见性改变时，递归地更新所有子项的可见性
            foreach (var child in Children)
            {
                child.IsVisible = value;
            }
            OnRefreshRequired();
        }

        /// <summary>
        /// 请求刷新图表事件 (全量重建)
        /// </summary>
        public event EventHandler RequestRefresh;

        /// <summary>
        /// 请求更新图表样式事件 (仅重绘，不重建)
        /// </summary>
        public event EventHandler RequestStyleUpdate;

        protected void OnRefreshRequired()
        {
            RequestRefresh?.Invoke(this, EventArgs.Empty);
        }

        protected void OnStyleUpdateRequired()
        {
            // 如果绘图对象为空，说明还没渲染，直接走全量刷新
            if (Plottable == null)
            {
                OnRefreshRequired();
                return;
            }

            UpdateStyle();
            RequestStyleUpdate?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 更新样式的虚方法
        /// </summary>
        public virtual void UpdateStyle()
        {
            // 默认不做任何事
        }
    }
}
