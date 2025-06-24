using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using GeoChemistryNexus.PropertyEditor;

namespace GeoChemistryNexus.Models
{

    public partial class AxisDefinition : ObservableObject
    {
        /// <summary>
        /// Axis type ("Left", "Right", "Bottom", "Top")
        /// </summary>
        [ObservableProperty]
        [property: Category("Axis Style")] // 坐标轴样式
        [property: DisplayName("Type")] // 类型
        private string type;

        /// <summary>
        /// Axis label
        /// </summary>
        [ObservableProperty]
        [property: Category("Axis Title")] // 坐标轴标题
        [property: DisplayName("Content")] // 内容
        [property: Editor(typeof(LocalizedStringPropertyEditor), typeof(PropertyEditorBase))]
        private LocalizedString _label = new LocalizedString();

        /// <summary>
        /// Font family
        /// </summary>
        [ObservableProperty]
        [property: Category("Axis Title")] // 坐标轴标题
        [property: DisplayName("Font Family")] // 字体
        [property: Editor(typeof(FontFamilyPropertyEditor), typeof(PropertyEditorBase))]
        private string _family = "Arial";

        /// <summary>
        /// Font size
        /// </summary>
        [ObservableProperty]
        [property: Category("Axis Title")] // 坐标轴标题
        [property: DisplayName("Font Size")] // 字体大小
        private float _size = 12;

        /// <summary>
        /// Font color
        /// </summary>
        [ObservableProperty]
        [property: Category("Axis Title")] // 坐标轴标题
        [property: DisplayName("Font Color")] // 字体颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _color = "#000000";

        /// <summary>
        /// Bold style
        /// </summary>
        [ObservableProperty]
        [property: Category("Axis Title")] // 坐标轴标题
        [property: DisplayName("Bold")] // 粗体
        private bool _isBold = false;

        /// <summary>
        /// Italic style
        /// </summary>
        [ObservableProperty]
        [property: Category("Axis Title")] // 坐标轴标题
        [property: DisplayName("Italic")] // 斜体
        private bool _isItalic = false;

        /// <summary>
        /// Axis tick interval
        /// </summary>
        [ObservableProperty]
        [property: Category("Tick Style")] // 刻度样式
        [property: DisplayName("Tick Interval")] // 刻度间隔
        private double? _tickInterval;

        /// <summary>
        /// Axis major tick length
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Tick Style")] // 主刻度样式
        [property: DisplayName("Length")] // 长度
        private float _majorTickLength = 4;

        /// <summary>
        /// Axis major tick width
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Tick Style")] // 主刻度样式
        [property: DisplayName("Width")] // 宽度
        private float _majorTickWidth = 1;

        /// <summary>
        /// Axis major tick color
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Tick Style")] // 主刻度样式
        [property: DisplayName("Color")] // 颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _majorTickWidthColor = "#000000";

        /// <summary>
        /// Axis major tick anti-aliasing enabled
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Tick Style")] // 主刻度样式
        [property: DisplayName("Anti-aliasing")] // 抗锯齿
        private bool _majorTickAntiAlias = false;

        /// <summary>
        /// Axis minor tick length
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Tick Style")] // 次刻度样式
        [property: DisplayName("Length")] // 长度
        private float _minorTickLength = 4;

        /// <summary>
        /// Axis minor tick width
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Tick Style")] // 次刻度样式
        [property: DisplayName("Width")] // 宽度
        private float _minorTickWidth = 1;

        /// <summary>
        /// Axis minor tick color
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Tick Style")] // 次刻度样式
        [property: DisplayName("Color")] // 颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _minorTickColor = "#000000"; // 例如灰色

        /// <summary>
        /// Axis minor tick anti-aliasing enabled
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Tick Style")] // 次刻度样式
        [property: DisplayName("Anti-aliasing")] // 抗锯齿
        private bool _minorTickAntiAlias = false;

        /// <summary>
        /// Tick label font family
        /// </summary>
        [ObservableProperty]
        [property: Category("Tick Labels")] // 刻度标签
        [property: DisplayName("Font Family")] // 字体
        [property: Editor(typeof(FontFamilyPropertyEditor), typeof(PropertyEditorBase))]
        private string _tickLableFamily = "Arial";

        /// <summary>
        /// Font size
        /// </summary>
        [ObservableProperty]
        [property: Category("Tick Labels")] // 刻度标签
        [property: DisplayName("Font Size")] // 字体大小
        private float _tickLablesize = 12;

        /// <summary>
        /// Font color
        /// </summary>
        [ObservableProperty]
        [property: Category("Tick Labels")] // 刻度标签
        [property: DisplayName("Font Color")] // 字体颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _tickLablecolor = "#000000";

        /// <summary>
        /// Bold style
        /// </summary>
        [ObservableProperty]
        [property: Category("Tick Labels")] // 刻度标签
        [property: DisplayName("Bold")] // 粗体
        private bool _tickLableisBold = false;

        /// <summary>
        /// Italic style
        /// </summary>
        [ObservableProperty]
        [property: Category("Tick Labels")] // 刻度标签
        [property: DisplayName("Italic")] // 斜体
        private bool _tickLableisItalic = false;
    }
}