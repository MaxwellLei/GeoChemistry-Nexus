using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.PropertyEditor;
using System.ComponentModel;
using HandyControl.Controls;
using GeoChemistryNexus.Converter;
using System.Windows;
using ScottPlot;

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
        [property: LocalizedCategory("content_and_position")]       // 内容与位置
        [property: LocalizedDisplayName("start_coordinates")]     // 起始坐标
        [property: Editor(typeof(PointDefinitionPropertyEditor), typeof(PropertyEditorBase))]
        private PointDefinition _startAndEnd = new PointDefinition();

        /// <summary>
        /// 注释内容，多语言
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("content_and_position")]       // 内容与位置
        [property: LocalizedDisplayName("content")]       // 内容
        [property: Editor(typeof(LocalizedStringPropertyEditor), typeof(PropertyEditorBase))]
        private LocalizedString _content = new LocalizedString();

        /// <summary>
        /// 注释内容，多语言
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("content_and_position")]       // 内容与位置
        [property: LocalizedDisplayName("alignment")]     // 对齐方式
        private System.Windows.HorizontalAlignment _contentHorizontalAlignment = System.Windows.HorizontalAlignment.Left;


        /// <summary>
        /// 字体
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("font_style")]        // 字体样式
        [property: LocalizedDisplayName("font_name")]     // 字体名称
        [property: Editor(typeof(FontFamilyPropertyEditor), typeof(PropertyEditorBase))]
        private string _family = "Arial";


        /// <summary>
        /// 字体大小
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("font_style")]        // 字体样式
        [property: LocalizedDisplayName("font_size")]     // 字体大小
        private float _size = 12;

        /// <summary>
        /// 字体旋转
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("font_style")]        // 字体样式
        [property: LocalizedDisplayName("font_rotation")]     // 字体旋转
        private float _rotation = 0;

        /// <summary>
        /// 字体颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("font_style")]        // 字体样式
        [property: LocalizedDisplayName("font_color")]    //字体颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _color = "#000000";

        /// <summary>
        /// 粗体样式
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("font_style")]        // 字体样式
        [property: LocalizedDisplayName("bold")]       // 粗体
        private bool _isBold = false;

        /// <summary>
        /// 斜体样式
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("font_style")]        // 字体样式
        [property: LocalizedDisplayName("italic")]       // 斜体
        private bool _isItalic = false;

        /// <summary>
        /// 背景与边框   背景颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("background_and_border")]       // 背景与边框
        [property: LocalizedDisplayName("background_color")]     // 背景颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _backgroundColor = Colors.Transparent.ToHex();

        /// <summary>
        /// 背景与边框   边框颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("background_and_border")]       // 背景与边框
        [property: LocalizedDisplayName("border_color")]     // 边框颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _borderColor = Colors.Transparent.ToHex();

        /// <summary>
        /// 背景与边框   边框宽度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("background_and_border")]       // 背景与边框
        [property: LocalizedDisplayName("border_width")]     // 边框宽度
        private float _borderWidth = 0;

        /// <summary>
        /// 背景与边框   边框宽度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("background_and_border")]       // 背景与边框
        [property: LocalizedDisplayName("corner_radius")]     // 圆角半径
        private float _filletRadius = 0;

        /// <summary>
        /// 高级渲染   抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("advanced_rendering")]        // 高级渲染
        [property: LocalizedDisplayName("anti_aliasing")]      // 抗锯齿
        private bool _antiAliasEnable = true;
    }
}
