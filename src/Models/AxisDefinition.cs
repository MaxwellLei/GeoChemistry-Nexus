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
        [property: LocalizedCategory("axis_style")] // 坐标轴样式
        [property: LocalizedDisplayName("type")] // 类型
        [property: Browsable(false)]        // 取消属性面板展示该属性
        private string type;

        /// <summary>
        /// Axis label
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("content")] // 内容
        [property: Editor(typeof(LocalizedStringPropertyEditor), typeof(PropertyEditorBase))]
        private LocalizedString _label = new LocalizedString();

        /// <summary>
        /// Font family
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("font_family")] // 字体
        [property: Editor(typeof(FontFamilyPropertyEditor), typeof(PropertyEditorBase))]
        private string _family = "Arial";

        /// <summary>
        /// Font size
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("font_size")] // 字体大小
        private float _size = 12;

        /// <summary>
        /// Font color
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("font_color")] // 字体颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _color = "#000000";

        /// <summary>
        /// Bold style
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("bold")] // 粗体
        private bool _isBold = false;

        /// <summary>
        /// Italic style
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")] // 坐标轴标题
        [property: LocalizedDisplayName("italic")] // 斜体
        private bool _isItalic = false;

        /// <summary>
        /// Axis tick interval
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("tick_style")] // 刻度样式
        [property: LocalizedDisplayName("tick_interval")] // 刻度间隔
        private double? _tickInterval;

        /// <summary>
        /// Axis major tick length
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_tick_style")] // 主刻度样式
        [property: LocalizedDisplayName("length")] // 长度
        private float _majorTickLength = 4;

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
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _majorTickWidthColor = "#000000";

        /// <summary>
        /// Axis major tick anti-aliasing enabled
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_tick_style")] // 主刻度样式
        [property: LocalizedDisplayName("anti_aliasing")] // 抗锯齿
        private bool _majorTickAntiAlias = false;

        /// <summary>
        /// Axis minor tick length
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_tick_style")] // 次刻度样式
        [property: LocalizedDisplayName("length")] // 长度
        private float _minorTickLength = 4;

        /// <summary>
        /// Axis minor tick width
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_tick_style")] // 次刻度样式
        [property: LocalizedDisplayName("width")] // 宽度
        private float _minorTickWidth = 1;

        /// <summary>
        /// Axis minor tick color
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_tick_style")] // 次刻度样式
        [property: LocalizedDisplayName("color")] // 颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _minorTickColor = "#000000"; // 例如灰色

        /// <summary>
        /// Axis minor tick anti-aliasing enabled
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_tick_style")] // 次刻度样式
        [property: LocalizedDisplayName("anti_aliasing")] // 抗锯齿
        private bool _minorTickAntiAlias = false;

        /// <summary>
        /// Tick label font family
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("tick_labels")] // 刻度标签
        [property: LocalizedDisplayName("font_family")] // 字体
        [property: Editor(typeof(FontFamilyPropertyEditor), typeof(PropertyEditorBase))]
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
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
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


        /// <summary>
        /// 坐标轴类型 (线性/对数)
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("测试")] // 坐标轴样式
        [property: LocalizedDisplayName("缩放类型")] // 缩放类型
        private AxisScaleType _scaleType = AxisScaleType.Linear;

        ///// <summary>
        ///// 坐标轴最小值
        ///// </summary>
        //[ObservableProperty]
        //[property: LocalizedCategory("测试")] // 坐标轴范围
        //[property: LocalizedDisplayName("最小值")] // 最小值
        //private double _minimum;

        ///// <summary>
        ///// 坐标轴最大值
        ///// </summary>
        //[ObservableProperty]
        //[property: LocalizedCategory("测试")] // 坐标轴范围
        //[property: LocalizedDisplayName("最大值")] // 最大值
        //private double? _maximum;

        ///// <summary>
        ///// 是否反转坐标轴方向
        ///// </summary>
        //[ObservableProperty]
        //[property: LocalizedCategory("测试")] // 坐标轴样式
        //[property: LocalizedDisplayName("反转坐标轴")] // 反转
        //private bool _isInverted = false;
    }

    public enum AxisScaleType
    {
        Linear,
        Logarithmic
    }
}