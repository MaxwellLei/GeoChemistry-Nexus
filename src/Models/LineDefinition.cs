using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Converter;

namespace GeoChemistryNexus.Models
{
    public partial class LineDefinition : ObservableObject
    {
        /// <summary>
        /// 起始点
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("position")]      // 位置
        [property: LocalizedDisplayName("start_coordinates")]     // 起始坐标
        private PointDefinition _start = new PointDefinition();

        /// <summary>
        /// 终止点
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("position")]      // 位置
        [property: LocalizedDisplayName("end_coordinates")]      // 终止坐标
        private PointDefinition _end = new PointDefinition();

        /// <summary>
        /// 颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 样式
        [property: LocalizedDisplayName("color")]        // 颜色
        private string _color = "#000000";

        /// <summary>
        /// 宽度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 样式
        [property: LocalizedDisplayName("width")]    // 宽度
        private float _width = 1.5f;


        /// <summary>
        /// 线条的样式，例如：虚线，点线等
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 样式
        [property: LocalizedDisplayName("type")]     // 类型
        public LineType _style;

        // 枚举类型
        [TypeConverter(typeof(EnumLocalizationConverter))]
        public enum LineType
        {
            [LocalizedDescription("line_type_solid")]
            Solid,
            [LocalizedDescription("line_type_dash")]
            Dash,
            [LocalizedDescription("line_type_densely_dashed")]
            DenselyDashed,
            [LocalizedDescription("line_type_dot")]
            Dot
        }
    }
}
