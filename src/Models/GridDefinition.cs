using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using System.ComponentModel;
using GeoChemistryNexus.PropertyEditor;
using static GeoChemistryNexus.Models.LineDefinition;

namespace GeoChemistryNexus.Models
{
    public partial class GridDefinition : ObservableObject
    {
        /// <summary>
        /// 主网格 是否显示
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Grid Line")] // 主网格线
        [property: DisplayName("Is Visible")] // 是否显示
        private bool _majorGridLineIsVisible = false;

        /// <summary>
        /// 主网格 颜色
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Grid Line")] // 主网格线
        [property: DisplayName("Color")] // 颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _majorGridLineColor = "#1A000000";

        /// <summary>
        /// 主网格 线宽
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Grid Line")] // 主网格线
        [property: DisplayName("Width")] // 线宽
        private float _majorGridLineWidth = 1;

        /// <summary>
        /// 主网格 线型
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Grid Line")] // 主网格线
        [property: DisplayName("Pattern")] // 线型
        private LineType _majorGridLinePattern = LineType.Solid;

        /// <summary>
        /// 主网格 抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: Category("Major Grid Line")] // 主网格线
        [property: DisplayName("Enable Anti-Alias")] // 启用抗锯齿
        private bool _majorGridLineAntiAlias = false;


        /// <summary>
        /// 次网格 是否显示
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Grid Line")] // 次网格线
        [property: DisplayName("Is Visible")] // 是否显示
        private bool _minorGridLineIsVisible = false;

        /// <summary>
        /// 次网格 颜色
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Grid Line")] // 次网格线
        [property: DisplayName("Color")] // 颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _minorGridLineColor = "#0D000000";

        /// <summary>
        /// 次网格 线宽
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Grid Line")] // 次网格线
        [property: DisplayName("Width")] // 线宽
        private float _minorGridLineWidth = 1f;

        /// <summary>
        /// 次网格 线型
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Grid Line")] // 次网格线
        [property: DisplayName("Pattern")] // 线型
        private LineType _minorGridLinePattern = LineType.Solid;

        /// <summary>
        /// 次网格 抗锯齿
        /// </summary>
        [ObservableProperty]
        [property: Category("Minor Grid Line")] // 次网格线
        [property: DisplayName("Enable Anti-Alias")] // 启用抗锯齿
        private bool _minorGridLineAntiAlias = false;


        /// <summary>
        /// 背景填充    启用交替填充
        /// </summary>
        [ObservableProperty]
        [property: Category("Background Fill")] // 背景填充
        [property: DisplayName("Enable Alternating Fill")] // 启用填充 (更准确的翻译为 "启用交替填充")
        private bool _gridAlternateFillingIsEnable = false;


        /// <summary>
        /// 背景填充    填充1
        /// </summary>
        [ObservableProperty]
        [property: Category("Background Fill 1")] // 背景填充
        [property: DisplayName("Fill Color 1")] // 填充颜色 1
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _gridFillColor1 = Colors.Transparent.ToHex();


        /// <summary>
        /// 背景填充    填充1
        /// </summary>
        [ObservableProperty]
        [property: Category("Background Fill 2")] // 背景填充
        [property: DisplayName("Fill Color 2")] // 填充颜色 2
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _gridFillColor2 = Colors.Transparent.ToHex();
    }
}