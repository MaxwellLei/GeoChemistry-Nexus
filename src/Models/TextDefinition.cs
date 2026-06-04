using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System.ComponentModel;
using GeoChemistryNexus.Converter;
using System.Windows;
using ScottPlot;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 注释类
    /// </summary>
    public partial class TextDefinition : ObservableObject
    {
        /// <summary>
        /// Start-End坐标位置
        /// </summary>
        [ObservableProperty]
        private PointDefinition _startAndEnd = new PointDefinition();

        /// <summary>
        /// 注释内容，多语言
        /// </summary>
        [ObservableProperty]
        private LocalizedString _content = new LocalizedString();

        /// <summary>
        /// 注释内容，多语言
        /// </summary>
        [ObservableProperty]
        private TextAlignment _contentHorizontalAlignment = TextAlignment.Left;


        /// <summary>
        /// 字体
        /// </summary>
        [ObservableProperty]
        private string _family = "Arial";


        /// <summary>
        /// 字体大小
        /// </summary>
        [ObservableProperty]
        private float _size = 12;

        /// <summary>
        /// 字体旋转
        /// </summary>
        [ObservableProperty]
        private float _rotation = 0;

        /// <summary>
        /// 字体颜色
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// 粗体样式
        /// </summary>
        [ObservableProperty]
        private bool _isBold = false;

        /// <summary>
        /// 斜体样式
        /// </summary>
        [ObservableProperty]
        private bool _isItalic = false;

        /// <summary>
        /// 背景与边框   背景颜色
        /// </summary>
        [ObservableProperty]
        private string _backgroundColor = Colors.Transparent.ToHex();

        /// <summary>
        /// 背景与边框   边框颜色
        /// </summary>
        [ObservableProperty]
        private string _borderColor = Colors.Transparent.ToHex();

        /// <summary>
        /// 背景与边框   边框宽度
        /// </summary>
        [ObservableProperty]
        private float _borderWidth = 0;

        /// <summary>
        /// 背景与边框   边框宽度
        /// </summary>
        [ObservableProperty]
        private float _filletRadius = 0;

        /// <summary>
        /// 高级渲染   抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: Browsable(false)]
        private bool _antiAliasEnable = true;

        partial void OnAntiAliasEnableChanged(bool value)
        {
            if (!value)
            {
                AntiAliasEnable = true;
            }
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }
}
