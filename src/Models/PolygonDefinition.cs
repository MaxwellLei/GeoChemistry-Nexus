using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.PropertyEditor;
using System.ComponentModel;

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
        private List<PointDefinition> _vertices = new List<PointDefinition>();

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
    }
}
