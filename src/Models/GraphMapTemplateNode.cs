using GeoChemistryNexus.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public class GraphMapTemplateNode : ObservableObject
    {
        /// <summary>
        /// 当节点名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 记录当前节点的父亲
        /// </summary>
        [JsonIgnore]
        public GraphMapTemplateNode Parent { get; set; }

        /// <summary>
        /// 子节点
        /// </summary>
        public ObservableCollection<GraphMapTemplateNode> Children { get; } = new();

        /// <summary>
        /// 关联的数据库实体 ID
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// 模板文件和缩略图路径，不含文件后缀
        /// </summary>
        private string _graphMapPath;
        public string GraphMapPath
        {
            get => _graphMapPath;
            set
            {
                if (SetProperty(ref _graphMapPath, value))
                {
                    OnPropertyChanged(nameof(TemplateCount));
                }
            }
        }

        // 模板列表文件哈希值
        // 更新逻辑核心依赖
        [JsonPropertyName("file_hash")]
        public string FileHash { get; set; }

        /// <summary>
        /// 数据库记录的状态 (UP_TO_DATE, OUTDATED, NOT_INSTALLED 等)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 标识是否为自定义模板
        /// </summary>
        public bool IsCustomTemplate { get; set; }

        /// <summary>
        /// 是否展开
        /// </summary>
        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 当前类别下的模板数量（递归统计叶子模板数）
        /// 叶子节点（具体模板）计为 1；分类节点为其所有子孙模板数之和
        /// </summary>
        [JsonIgnore]
        public int TemplateCount
        {
            get
            {
                // 叶子节点：存在 GraphMapPath
                if (!string.IsNullOrEmpty(GraphMapPath))
                    return 1;

                // 分类节点：汇总所有子节点的模板数量
                if (Children == null || Children.Count == 0)
                    return 0;

                int sum = 0;
                foreach (var child in Children)
                {
                    sum += child?.TemplateCount ?? 0;
                }
                return sum;
            }
        }

        public GraphMapTemplateNode()
        {
            Children.CollectionChanged += OnChildrenCollectionChanged;
        }

        private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 监听新增/移除子项，以便在子项模板数量变化时联动更新父项
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is GraphMapTemplateNode node)
                    {
                        AttachChild(node);
                    }
                }
            }
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is GraphMapTemplateNode node)
                    {
                        DetachChild(node);
                    }
                }
            }

            // 子集合变动会影响当前节点统计值
            OnPropertyChanged(nameof(TemplateCount));
        }

        private void AttachChild(GraphMapTemplateNode child)
        {
            child.PropertyChanged += Child_PropertyChanged;
        }

        private void DetachChild(GraphMapTemplateNode child)
        {
            child.PropertyChanged -= Child_PropertyChanged;
        }

        private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当任何子节点的模板数量或路径改变时，更新当前节点的统计值
            if (e.PropertyName == nameof(TemplateCount) || e.PropertyName == nameof(GraphMapPath))
            {
                OnPropertyChanged(nameof(TemplateCount));
            }
        }
    }
}
