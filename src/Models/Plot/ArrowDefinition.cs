using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Converter;
using System.ComponentModel;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    // 箭头绘图对象
    public partial class ArrowDefinition : ObservableObject
    {
        /// <summary>
        /// 箭头起始点（基点）
        /// </summary>
        [ObservableProperty]
        private PointDefinition _start = new PointDefinition();

        /// <summary>
        /// 箭头终止点（尖端）
        /// </summary>
        [ObservableProperty]
        private PointDefinition _end = new PointDefinition();

        /// <summary>
        /// 箭头线条和填充颜色
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// 箭头边框宽度
        /// </summary>
        [ObservableProperty]
        private float _arrowWidth = 0.5f;

        /// <summary>
        /// 箭头头部宽度
        /// </summary>
        [ObservableProperty]
        private float _arrowheadWidth = 8f;

        /// <summary>
        /// 箭头头部长度
        /// </summary>
        [ObservableProperty]
        private float _arrowheadLength = 12f;
    }
}
