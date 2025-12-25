using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Attributes;
using System.ComponentModel;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 笛卡尔（二维）坐标轴定义，包含范围、缩放、主次刻度等特定属性
    /// </summary>
    public partial class CartesianAxisDefinition : BaseAxisDefinition
    {
        /// <summary>
        /// 坐标轴类型 (线性/对数)
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_style")] // 坐标轴样式
        [property: LocalizedDisplayName("zoom_type")] // 缩放类型
        private AxisScaleType _scaleType = AxisScaleType.Linear;

        // 优化切换对数缩放类型默认值
        partial void OnScaleTypeChanged(AxisScaleType value)
        {
            if (value == AxisScaleType.Logarithmic)
            {
                Minimum = 0.1;
                Maximum = 100000;
            }
            OnPropertyChanged(nameof(AllowedInputMinimum));
        }

        public double AllowedInputMinimum => ScaleType == AxisScaleType.Logarithmic ? 1E-9 : double.MinValue;

        /// <summary>
        /// 坐标轴最小值
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_style")] // 坐标轴范围
        [property: LocalizedDisplayName("from")] // 最小值
        private double _minimum = 0;

        /// <summary>
        /// 坐标轴最大值
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_style")] // 坐标轴范围
        [property: LocalizedDisplayName("to")] // 最大值
        private double _maximum = 0;

        /// <summary>
        /// Axis major tick length
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_tick_style")] // 主刻度样式
        [property: LocalizedDisplayName("length")] // 长度
        private float _majorTickLength = 4;

        [ObservableProperty]
        [property: LocalizedCategory("major_tick_style")] // 主刻度样式
        [property: LocalizedDisplayName("interval")] // 间隔
        private double _majorTickInterval = 0;


        /// <summary>
        /// 每个主刻度之间的次刻度数量
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_tick_style")] // 次刻度样式
        [property: LocalizedDisplayName("count")] // 数量
        private int _minorTicksPerMajorTick = 0;

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
        private string _minorTickColor = "#000000"; // 例如灰色

        /// <summary>
        /// Axis minor tick anti-aliasing enabled
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_tick_style")] // 次刻度样式
        [property: LocalizedDisplayName("anti_aliasing")] // 抗锯齿
        private bool _minorTickAntiAlias = false;

        #region Subtitle / SubLabel
        /// <summary>
        /// Sub-label text
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")]
        [property: LocalizedDisplayName("subtitle")]
        private LocalizedString _subLabel = new LocalizedString();

        /// <summary>
        /// Sub-label font size
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")]
        [property: LocalizedDisplayName("subtitle_font_size")]
        private float _subLabelSize = 14;

        /// <summary>
        /// Sub-label font color
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")]
        [property: LocalizedDisplayName("subtitle_color")]
        private string _subLabelColor = "#000000";

        /// <summary>
        /// Sub-label bold
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")]
        [property: LocalizedDisplayName("subtitle_bold")]
        private bool _subLabelBold = false;

        /// <summary>
        /// Sub-label italic
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("axis_title")]
        [property: LocalizedDisplayName("subtitle_italic")]
        private bool _subLabelItalic = false;
        #endregion
    }
}
