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
        [property: Category("Content & Position")]       // 内容与位置
        [property: DisplayName("Start Coordinates")]     // 起始坐标
        [property: Editor(typeof(PointDefinitionPropertyEditor), typeof(PropertyEditorBase))]
        private PointDefinition _startAndEnd = new PointDefinition();

        /// <summary>
        /// 注释内容，多语言
        /// </summary>
        [ObservableProperty]
        [property: Category("Content & Position")]       // 内容与位置
        [property: DisplayName("Content")]       // 内容
        [property: Editor(typeof(LocalizedStringPropertyEditor), typeof(PropertyEditorBase))]
        private LocalizedString _content = new LocalizedString();

        /// <summary>
        /// 注释内容，多语言
        /// </summary>
        [ObservableProperty]
        [property: Category("Content & Position")]       // 内容与位置
        [property: DisplayName("Alignment")]     // 对齐方式
        private System.Windows.HorizontalAlignment _contentHorizontalAlignment = System.Windows.HorizontalAlignment.Left;


        /// <summary>
        /// 字体
        /// </summary>
        [ObservableProperty]
        [property: Category("Font Style")]        // 字体样式
        [property: DisplayName("Font Name")]     // 字体名称
        [property: Editor(typeof(FontFamilyPropertyEditor), typeof(PropertyEditorBase))]
        private string _family = "Arial";


        /// <summary>
        /// 字体大小
        /// </summary>
        [ObservableProperty]
        [property: Category("Font Style")]        // 字体样式
        [property: DisplayName("Font Size")]     // 字体大小
        private float _size = 12;

        /// <summary>
        /// 字体旋转
        /// </summary>
        [ObservableProperty]
        [property: Category("Font Style")]        // 字体样式
        [property: DisplayName("Font Rotation")]     // 字体旋转
        private float _rotation = 0;

        /// <summary>
        /// 字体颜色
        /// </summary>
        [ObservableProperty]
        [property: Category("Font Style")]        // 字体样式
        [property: DisplayName("Font Color")]
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _color = "#000000";

        /// <summary>
        /// 粗体样式
        /// </summary>
        [ObservableProperty]
        [property: Category("Font Style")]        // 字体样式
        [property: DisplayName("Bold")]       // 粗体
        private bool _isBold = false;

        /// <summary>
        /// 斜体样式
        /// </summary>
        [ObservableProperty]
        [property: Category("Font Style")]        // 字体样式
        [property: DisplayName("Italic")]       // 斜体
        private bool _isItalic = false;

        /// <summary>
        /// 背景与边框   背景颜色
        /// </summary>
        [ObservableProperty]
        [property: Category("Background and Border")]       // 背景与边框
        [property: DisplayName("Background Color")]     // 背景颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _backgroundColor = Colors.Transparent.ToHex();

        /// <summary>
        /// 背景与边框   边框颜色
        /// </summary>
        [ObservableProperty]
        [property: Category("Background and Border")]       // 背景与边框
        [property: DisplayName("Border Color")]     // 边框颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _borderColor = Colors.Transparent.ToHex();

        /// <summary>
        /// 背景与边框   边框宽度
        /// </summary>
        [ObservableProperty]
        [property: Category("Background and Border")]       // 背景与边框
        [property: DisplayName("Border Width")]     // 边框宽度
        private float _borderWidth = 0;

        /// <summary>
        /// 背景与边框   边框宽度
        /// </summary>
        [ObservableProperty]
        [property: Category("Background and Border")]       // 背景与边框
        [property: DisplayName("Corner Radius")]     // 圆角半径
        private float _filletRadius = 0;

        /// <summary>
        /// 高级渲染   抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: Category("Advanced Rendering")]        // 高级渲染
        [property: DisplayName("Anti-aliasing")]      // 抗锯齿
        private bool _antiAliasEnable = true;
    }
}
