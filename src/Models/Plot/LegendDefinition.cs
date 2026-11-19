using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using ScottPlot;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Models
{
    public partial class LegendDefinition : ObservableObject
    {
        /// <summary>
        /// 图例位置
        /// </summary>
        [ObservableProperty]
        private Alignment _alignment = Alignment.UpperRight;

        /// <summary>
        /// 图例条目排列方式
        /// </summary>
        [ObservableProperty]
        private Orientation _orientation = Orientation.Horizontal;

        /// <summary>
        /// 属性面板是否显示“位置与排列”分组。Harker 多联图的图例位置在设置页里确定，会置为 false。
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [ObservableProperty]
        private bool _isLayoutEditable = true;

        /// <summary>
        /// 图例是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isVisible = true;

        /// <summary>
        /// 字体
        /// </summary>
        [ObservableProperty]
        private string _font = "Arial";

        /// <summary>
        /// 字号（pt）
        /// </summary>
        [ObservableProperty]
        private float _fontSize = 12;

        /// <summary>
        /// 字体颜色（WPF 十六进制，#RRGGBB 或 #AARRGGBB）
        /// </summary>
        [ObservableProperty]
        private string _fontColor = "#000000";

        /// <summary>
        /// 粗体
        /// </summary>
        [ObservableProperty]
        private bool _isBold = false;

        /// <summary>
        /// 斜体
        /// </summary>
        [ObservableProperty]
        private bool _isItalic = false;

        /// <summary>
        /// 背景颜色（WPF 十六进制）
        /// </summary>
        [ObservableProperty]
        private string _backgroundColor = "#FFFFFF";

        /// <summary>
        /// 背景不透明度（0-1），与背景颜色自带的透明度相乘
        /// </summary>
        [ObservableProperty]
        private double _backgroundOpacity = 1;

        /// <summary>
        /// 边框颜色（WPF 十六进制）
        /// </summary>
        [ObservableProperty]
        private string _outlineColor = "#000000";

        /// <summary>
        /// 边框宽度，0 表示无边框
        /// </summary>
        [ObservableProperty]
        private float _outlineWidth = 1;

        /// <summary>
        /// 是否显示阴影
        /// </summary>
        [ObservableProperty]
        private bool _shadowEnabled = false;

        /// <summary>
        /// 阴影颜色（WPF 十六进制，通常带透明度）
        /// </summary>
        [ObservableProperty]
        private string _shadowColor = "#40000000";

        /// <summary>
        /// 阴影水平偏移（像素，正值向右，负值向左）
        /// </summary>
        [ObservableProperty]
        private float _shadowOffsetX = 3;

        /// <summary>
        /// 阴影垂直偏移（像素，正值向下，负值向上）
        /// </summary>
        [ObservableProperty]
        private float _shadowOffsetY = 3;
    }
}