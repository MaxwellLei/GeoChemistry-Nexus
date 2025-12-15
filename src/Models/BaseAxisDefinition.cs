using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System.ComponentModel;
using System.Text.Json.Serialization;
using GeoChemistryNexus.Converter;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 坐标轴定义的基类，包含笛卡尔坐标轴和三元图坐标轴的通用属性
    /// </summary>
    /// 处理多态序列化
    [JsonDerivedType(typeof(CartesianAxisDefinition), typeDiscriminator: "cartesian")]
    [JsonDerivedType(typeof(TernaryAxisDefinition), typeDiscriminator: "ternary")]
    public abstract partial class BaseAxisDefinition : ObservableObject
    {
        /// <summary>
        /// Axis type ("Left", "Right", "Bottom", "Top")
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_style")] // 坐标轴样式
        [property: LocalizedDisplayName("type")] // 类型
        [property: Browsable(false)]      // 取消属性面板展示该属性
        private string type;

        /// <summary>
        /// Axis label
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("content")] // 内容
        private LocalizedString _label = new LocalizedString();

        /// <summary>
        /// Font family
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("font_family")] // 字体
        private string _family = "Arial";

        /// <summary>
        /// Font size
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("font_size")] // 字体大小
        private float _size = 20;

        /// <summary>
        /// Font color
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("font_color")] // 字体颜色
        private string _color = "#000000";

        /// <summary>
        /// Bold style
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("bold")] // 粗体
        private bool _isBold = true;

        /// <summary>
        /// Italic style
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("italic")] // 斜体
        private bool _isItalic = false;

        /// <summary>
        /// Axis major tick width
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_tick_style")] // 主刻度样式
        [property: LocalizedDisplayName("width")] // 宽度
        private float _majorTickWidth = 1;

        /// <summary>
        /// Axis major tick color
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_tick_style")] // 主刻度样式
        [property: LocalizedDisplayName("color")] // 颜色
        private string _majorTickWidthColor = "#000000";

        /// <summary>
        /// Axis major tick anti-aliasing enabled
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_tick_style")] // 主刻度样式
        [property: LocalizedDisplayName("anti_aliasing")] // 抗锯齿
        private bool _majorTickAntiAlias = false;

        /// <summary>
        /// Tick label font family
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("tick_labels")] // 刻度标签
        [property: LocalizedDisplayName("font_family")] // 字体
        private string _tickLableFamily = "Arial";

        /// <summary>
        /// Font size
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("tick_labels")] // 刻度标签
        [property: LocalizedDisplayName("font_size")] // 字体大小
        private float _tickLablesize = 12;

        /// <summary>
        /// Font color
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("tick_labels")] // 刻度标签
        [property: LocalizedDisplayName("font_color")] // 字体颜色
        private string _tickLablecolor = "#000000";

        /// <summary>
        /// Bold style
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("tick_labels")] // 刻度标签
        [property: LocalizedDisplayName("bold")] // 粗体
        private bool _tickLableisBold = false;

        /// <summary>
        /// Italic style
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("tick_labels")] // 刻度标签
        [property: LocalizedDisplayName("italic")] // 斜体
        private bool _tickLableisItalic = false;
    }

    /// <summary>
    /// 坐标轴的缩放类型
    /// </summary>
    [TypeConverter(typeof(EnumLocalizationConverter))]
    public enum AxisScaleType
    {
        [LocalizedDescription("linear")]
        Linear,
        [LocalizedDescription("logarithmic")]
        Logarithmic
    }
}
