using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Attributes;
using GeoChemistryNexus.Converter;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public partial class LineDefinition : ObservableObject
    {
        /// <summary>
        /// 起始点
        /// </summary>
        [ObservableProperty]
        private PointDefinition _start = new PointDefinition();

        /// <summary>
        /// 终止点
        /// </summary>
        [ObservableProperty]
        private PointDefinition _end = new PointDefinition();

        /// <summary>
        /// 颜色
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// 宽度
        /// </summary>
        [ObservableProperty]
        private float _width = 1.5f;


        /// <summary>
        /// 线条的样式，例如：虚线，点线等
        /// </summary>
        [ObservableProperty]
        public LineType _style;

        // 枚举类型
        [TypeConverter(typeof(EnumLocalizationConverter))]
        [JsonConverter(typeof(JsonStringEnumConverter))]
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
