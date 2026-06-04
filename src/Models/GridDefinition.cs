using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using System.ComponentModel;
using GeoChemistryNexus.Helpers;
using static GeoChemistryNexus.Models.LineDefinition;

namespace GeoChemistryNexus.Models
{
    public partial class GridDefinition : ObservableObject
    {
        /// <summary>
        /// 主网格 是否显示
        /// </summary>
        [ObservableProperty]
        private bool _majorGridLineIsVisible = false;

        /// <summary>
        /// 主网格 颜色
        /// </summary>
        [ObservableProperty]
        private string _majorGridLineColor = "#1A000000";

        /// <summary>
        /// 主网格 线宽
        /// </summary>
        [ObservableProperty]
        private float _majorGridLineWidth = 1;

        /// <summary>
        /// 主网格 线型
        /// </summary>
        [ObservableProperty]
        private LineType _majorGridLinePattern = LineType.Solid;

        /// <summary>
        /// 主网格 抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: Browsable(false)]
        private bool _majorGridLineAntiAlias = true;


        /// <summary>
        /// 次网格 是否显示
        /// </summary>
        [ObservableProperty]
        private bool _minorGridLineIsVisible = false;

        /// <summary>
        /// 次网格 颜色
        /// </summary>
        [ObservableProperty]
        private string _minorGridLineColor = "#0D000000";

        /// <summary>
        /// 次网格 线宽
        /// </summary>
        [ObservableProperty]
        private float _minorGridLineWidth = 1f;

        /// <summary>
        /// 次网格 线型
        /// </summary>
        [ObservableProperty]
        private LineType _minorGridLinePattern = LineType.Solid;

        /// <summary>
        /// 次网格 抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: Browsable(false)]
        private bool _minorGridLineAntiAlias = true;


        /// <summary>
        /// 背景填充    启用交替填充
        /// </summary>
        [ObservableProperty]
        private bool _gridAlternateFillingIsEnable = false;


        /// <summary>
        /// 背景填充    填充1
        /// </summary>
        [ObservableProperty]
        private string _gridFillColor1 = Colors.Transparent.ToHex();


        /// <summary>
        /// 背景填充    填充1
        /// </summary>
        [ObservableProperty]
        private string _gridFillColor2 = Colors.Transparent.ToHex();
        [System.Text.Json.Serialization.JsonIgnore]
        [ObservableProperty]
        private bool _isMinorGridSupported = true;

        [System.Text.Json.Serialization.JsonIgnore]
        [ObservableProperty]
        private bool _isAlternatingFillSupported = true;

        partial void OnMajorGridLineAntiAliasChanged(bool value)
        {
            if (!value)
            {
                MajorGridLineAntiAlias = true;
            }
        }

        partial void OnMinorGridLineAntiAliasChanged(bool value)
        {
            if (!value)
            {
                MinorGridLineAntiAlias = true;
            }
        }
    }
}
