using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Converter;
using System.Text.RegularExpressions;

namespace GeoChemistryNexus.Models
{
    public partial class FunctionDefinition : ObservableObject
    {
        /// <summary>
        /// 函数表达式 (例如: Math.sin(x))
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("content")]       // 内容
        [property: LocalizedDisplayName("formula")]    // 公式
        private string _formula = "Math.sin(x)";

        /// <summary>
        /// X轴最小值
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("range")]         // 范围
        [property: LocalizedDisplayName("min_x")]      // 最小X
        private double _minX = -10;

        /// <summary>
        /// X轴最大值
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("range")]         // 范围
        [property: LocalizedDisplayName("max_x")]      // 最大X
        private double _maxX = 10;

        /// <summary>
        /// 采样点数量 (影响平滑度)
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("precision")]     // 精度
        [property: LocalizedDisplayName("point_count")] // 点数量
        private int _pointCount = 1000;

        /// <summary>
        /// 颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 样式
        [property: LocalizedDisplayName("color")]        // 颜色
        private string _color = "#FF0000";

        /// <summary>
        /// 宽度
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 样式
        [property: LocalizedDisplayName("width")]    // 宽度
        private float _width = 2.0f;

        /// <summary>
        /// 线条的样式
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("style")]       // 样式
        [property: LocalizedDisplayName("type")]     // 类型
        public LineDefinition.LineType _style = LineDefinition.LineType.Solid;

        [ObservableProperty]
        private string _formulaErrorMessage;

        [ObservableProperty]
        private bool _hasFormulaError;

        partial void OnFormulaChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                HasFormulaError = false;
                FormulaErrorMessage = null;
                return;
            }

            if (JintHelper.IsValidFunctionExpression(value))
            {
                HasFormulaError = false;
                FormulaErrorMessage = null;
            }
            else
            {
                HasFormulaError = true;
                FormulaErrorMessage = "公式不合法";
            }
        }
    }
}
