using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Attributes;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace GeoChemistryNexus.Models
{
    public partial class PolygonDefinition : ObservableObject
    {
        /// <summary>
        /// 多边形顶点
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("位置")]        // 位置
        [property: LocalizedDisplayName("顶点位置")]     // 顶点位置
        private ObservableCollection<PointDefinition> _vertices = new();


        /// <summary>
        /// 多边形填充颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("样式")]        // 样式
        [property: LocalizedDisplayName("填充颜色")]     // 填充颜色
        private string _fillColor = "#FFFF00";

        /// <summary>
        /// 边框线样式-边框颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("样式")]        // 样式
        [property: LocalizedDisplayName("边框颜色")]     // 边框颜色
        public string _borderColor = "#FF0078D4";       // 默认蓝色

        /// <summary>
        /// 边框线样式-边框宽度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("样式")]        // 样式
        [property: LocalizedDisplayName("边框宽度")]     // 边框宽度
        public float _borderWidth  = 2;

        /// <summary>
        /// 当 Vertices 属性本身被一个新的集合替换时调用。
        /// </summary>
        partial void OnVerticesChanged(ObservableCollection<PointDefinition>? oldValue, ObservableCollection<PointDefinition> newValue)
        {
            // 为旧集合解绑事件
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnVerticesCollectionChanged;
                foreach (var point in oldValue)
                {
                    point.PropertyChanged -= OnPointPropertyChanged;
                }
            }

            // 为新集合绑定事件
            if (newValue != null)
            {
                newValue.CollectionChanged += OnVerticesCollectionChanged;
                foreach (var point in newValue)
                {
                    point.PropertyChanged += OnPointPropertyChanged;
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PolygonDefinition()
        {
            _vertices = new ObservableCollection<PointDefinition>();
            _vertices.CollectionChanged += OnVerticesCollectionChanged;
        }

        /// <summary>
        /// 当集合内容发生增、删、改时触发。
        /// </summary>
        private void OnVerticesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 确保新添加的顶点也能被监听
            if (e.NewItems != null)
            {
                foreach (PointDefinition item in e.NewItems)
                {
                    item.PropertyChanged += OnPointPropertyChanged;
                }
            }
            // 确保被移除的顶点不再被监听，防止内存泄漏
            if (e.OldItems != null)
            {
                foreach (PointDefinition item in e.OldItems)
                {
                    item.PropertyChanged -= OnPointPropertyChanged;
                }
            }

            // 重绘
            OnPropertyChanged(nameof(Vertices));
        }

        /// <summary>
        /// 当某个顶点的坐标X或Y发生变化时触发。
        /// </summary>
        private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 重绘对象
            OnPropertyChanged(nameof(Vertices));
        }
    }
}
