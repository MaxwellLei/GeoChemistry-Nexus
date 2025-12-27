using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using System.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Attributes;
using static GeoChemistryNexus.Models.LineDefinition;

namespace GeoChemistryNexus.Models
{
    public partial class GridDefinition : ObservableObject
    {
        /// <summary>
        /// 主网格 是否显示
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_grid_line")] // 主网格线
        [property: LocalizedDisplayName("is_visible")] // 是否显示
        private bool _majorGridLineIsVisible = false;

        /// <summary>
        /// 主网格 颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_grid_line")] // 主网格线
        [property: LocalizedDisplayName("color")] // 颜色
        private string _majorGridLineColor = "#1A000000";

        /// <summary>
        /// 主网格 线宽
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_grid_line")] // 主网格线
        [property: LocalizedDisplayName("width")] // 线宽
        private float _majorGridLineWidth = 1;

        /// <summary>
        /// 主网格 线型
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_grid_line")] // 主网格线
        [property: LocalizedDisplayName("pattern")] // 线型
        private LineType _majorGridLinePattern = LineType.Solid;

        /// <summary>
        /// 主网格 抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("major_grid_line")] // 主网格线
        [property: LocalizedDisplayName("enable_anti_alias")] // 启用抗锯齿
        private bool _majorGridLineAntiAlias = false;


        /// <summary>
        /// 次网格 是否显示
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_grid_Line")] // 次网格线
        [property: LocalizedDisplayName("is_visible")] // 是否显示
        private bool _minorGridLineIsVisible = false;

        /// <summary>
        /// 次网格 颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_grid_Line")] // 次网格线
        [property: LocalizedDisplayName("color")] // 颜色
        private string _minorGridLineColor = "#0D000000";

        /// <summary>
        /// 次网格 线宽
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_grid_Line")] // 次网格线
        [property: LocalizedDisplayName("width")] // 线宽
        private float _minorGridLineWidth = 1f;

        /// <summary>
        /// 次网格 线型
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_grid_Line")] // 次网格线
        [property: LocalizedDisplayName("pattern")] // 线型
        private LineType _minorGridLinePattern = LineType.Solid;

        /// <summary>
        /// 次网格 抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("minor_grid_Line")] // 次网格线
        [property: LocalizedDisplayName("enable_anti_alias")] // 启用抗锯齿
        private bool _minorGridLineAntiAlias = false;


        /// <summary>
        /// 背景填充    启用交替填充
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("background_fill")] // 背景填充
        [property: LocalizedDisplayName("enable_alternating_fill")] // 启用交替填充
        private bool _gridAlternateFillingIsEnable = false;


        /// <summary>
        /// 背景填充    填充1
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("background_fill_1")] // 背景填充
        [property: LocalizedDisplayName("fill_color_1")] // 填充颜色 1
        private string _gridFillColor1 = Colors.Transparent.ToHex();


        /// <summary>
        /// 背景填充    填充1
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("background_fill_2")] // 背景填充
        [property: LocalizedDisplayName("fill_color_2")] // 填充颜色 2
        private string _gridFillColor2 = Colors.Transparent.ToHex();
        [System.Text.Json.Serialization.JsonIgnore]
        [ObservableProperty]
        private bool _isMinorGridSupported = true;

        [System.Text.Json.Serialization.JsonIgnore]
        [ObservableProperty]
        private bool _isAlternatingFillSupported = true;
    }
}