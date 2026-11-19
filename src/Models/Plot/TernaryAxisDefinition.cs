using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 三元图坐标轴
    /// </summary>
    public partial class TernaryAxisDefinition : BaseAxisDefinition
    {
        /// <summary>
        /// Label offset X
        /// </summary>
        [ObservableProperty]
        private double _labelOffsetX;

        /// <summary>
        /// Label offset Y
        /// </summary>
        [ObservableProperty]
        private double _labelOffsetY;

        /// <summary>
        /// 三角形这一条边的线宽
        /// </summary>
        [ObservableProperty]
        private float _axisLineWidth = 1;

        /// <summary>
        /// 三角形这一条边的颜色
        /// </summary>
        [ObservableProperty]
        private string _axisLineColor = "#000000";

        /// <summary>
        /// 副标题。字体沿用主标题。
        /// </summary>
        [ObservableProperty]
        private LocalizedString _subLabel = new LocalizedString();

        [ObservableProperty]
        private float _subLabelSize = 14;

        [ObservableProperty]
        private string _subLabelColor = "#000000";

        [ObservableProperty]
        private bool _subLabelBold;

        [ObservableProperty]
        private bool _subLabelItalic;

        /// <summary>
        /// 副标题在主标题偏移之外再挪动的 X
        /// </summary>
        [ObservableProperty]
        private double _subLabelOffsetX;

        /// <summary>
        /// 副标题在主标题偏移之外再挪动的 Y
        /// </summary>
        [ObservableProperty]
        private double _subLabelOffsetY;

        /// <summary>
        /// Show tick labels
        /// </summary>
        [ObservableProperty]
        private bool _isShowTickLabels = true;

        /// <summary>
        /// Show major ticks
        /// </summary>
        [ObservableProperty]
        private bool _isShowMajorTicks = true;

        /// <summary>
        /// 刻度短线长度。方向沿用这条边向外的默认方向。
        /// </summary>
        [ObservableProperty]
        private float _tickLength = 5;

        /// <summary>
        /// 刻度短线宽度
        /// </summary>
        [ObservableProperty]
        private float _tickWidth = 1;

        /// <summary>
        /// 沿这条边的刻度间隔。比例模式下单位是 0–1，百分数模式下单位是 0–100。
        /// </summary>
        [ObservableProperty]
        private double _tickStep = 0.1;

        /// <summary>
        /// 刻度数字的记法
        /// </summary>
        [ObservableProperty]
        private TernaryTickValueMode _tickValueMode = TernaryTickValueMode.Fraction;

        /// <summary>
        /// 刻度数字在短线末端之外再离开边线的距离
        /// </summary>
        [ObservableProperty]
        private double _tickLabelGap;

        partial void OnAxisLineWidthChanged(float value)
        {
            if (value < 0)
                AxisLineWidth = 0;
        }

        partial void OnTickLengthChanged(float value)
        {
            if (value < 0)
                TickLength = 0;
        }

        partial void OnTickWidthChanged(float value)
        {
            if (value < 0)
                TickWidth = 0;
        }

        partial void OnTickStepChanged(double value)
        {
            if (value < 0)
                TickStep = 0;
        }

        partial void OnTickLabelGapChanged(double value)
        {
            if (value < 0)
                TickLabelGap = 0;
        }

        partial void OnTickValueModeChanged(TernaryTickValueMode value)
        {
            if (value == TernaryTickValueMode.Percent)
            {
                if (TickStep > 0 && TickStep < 1)
                    TickStep = Math.Round(TickStep * 100, 6);
                else if (TickStep <= 0)
                    TickStep = 10;
            }
            else if (TickStep >= 1)
            {
                TickStep = Math.Round(TickStep / 100, 6);
            }
            else if (TickStep <= 0)
            {
                TickStep = 0.1;
            }
        }
    }

    /// <summary>
    /// 三元图刻度数字的记法
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TernaryTickValueMode
    {
        Fraction,
        Percent
    }
}
