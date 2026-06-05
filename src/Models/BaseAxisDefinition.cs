using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Attributes;
using GeoChemistryNexus.Converter;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 坐标轴定义的基类，包含笛卡尔坐标轴和三元图坐标轴的通用属性
    /// </summary>
    /// 处理多态序列化
    [JsonDerivedType(typeof(CartesianAxisDefinition), typeDiscriminator: "cartesian")]
    [JsonDerivedType(typeof(TernaryAxisDefinition), typeDiscriminator: "ternary")]
    [JsonDerivedType(typeof(SpiderAxisDefinition), typeDiscriminator: "spider")]
    public abstract partial class BaseAxisDefinition : ObservableObject
    {
        /// <summary>
        /// Axis type ("Left", "Right", "Bottom", "Top")
        /// </summary>
        [ObservableProperty]
        [Browsable(false)]      // 取消属性面板展示该属性
        private string type;

        /// <summary>
        /// Axis label
        /// </summary>
        [ObservableProperty]
        private LocalizedString _label = new LocalizedString();

        /// <summary>
        /// Font family
        /// </summary>
        [ObservableProperty]
        private string _family = "Arial";

        /// <summary>
        /// Font size
        /// </summary>
        [ObservableProperty]
        private float _size = 18;

        /// <summary>
        /// Font color
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// Bold style
        /// </summary>
        [ObservableProperty]
        private bool _isBold = true;

        /// <summary>
        /// Italic style
        /// </summary>
        [ObservableProperty]
        private bool _isItalic = false;

        /// <summary>
        /// Axis major tick color
        /// </summary>
        [ObservableProperty]
        private string _majorTickWidthColor = "#000000";

        /// <summary>
        /// Axis major tick anti-aliasing enabled
        /// </summary>
        [ObservableProperty]
        [Browsable(false)]
        private bool _majorTickAntiAlias = true;

        /// <summary>
        /// Tick label font family
        /// </summary>
        [ObservableProperty]
        private string _tickLableFamily = "Arial";

        /// <summary>
        /// Font size
        /// </summary>
        [ObservableProperty]
        private float _tickLablesize = 12;

        /// <summary>
        /// Font color
        /// </summary>
        [ObservableProperty]
        private string _tickLablecolor = "#000000";

        /// <summary>
        /// Bold style
        /// </summary>
        [ObservableProperty]
        private bool _tickLableisBold = false;

        /// <summary>
        /// Italic style
        /// </summary>
        [ObservableProperty]
        private bool _tickLableisItalic = false;

        partial void OnMajorTickAntiAliasChanged(bool value)
        {
            if (!value)
            {
                MajorTickAntiAlias = true;
            }
        }
    }

    /// <summary>
    /// 坐标轴的缩放类型
    /// </summary>
    [TypeConverter(typeof(EnumLocalizationConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AxisScaleType
    {
        [LocalizedDescription("linear")]
        Linear,
        [LocalizedDescription("logarithmic")]
        Logarithmic
    }
}
