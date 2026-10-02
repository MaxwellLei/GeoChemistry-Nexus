using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Converter;
using System.Text.RegularExpressions;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Models
{
    public partial class FunctionDefinition : ObservableObject
    {
        /// <summary>
        /// 函数表达式 (例如: Math.sin(x))
        /// </summary>
        [ObservableProperty]
        private string _formula = "Math.sin(x)";

        /// <summary>
        /// X轴最小值
        /// </summary>
        [ObservableProperty]
        private double _minX = -10;

        /// <summary>
        /// X轴最大值
        /// </summary>
        [ObservableProperty]
        private double _maxX = 10;

        /// <summary>
        /// Y轴最小值，留空时不限制
        /// </summary>
        [ObservableProperty]
        private double _minY = double.NaN;

        /// <summary>
        /// Y轴最大值，留空时不限制
        /// </summary>
        [ObservableProperty]
        private double _maxY = double.NaN;

        /// <summary>
        /// 采样点数量 (影响平滑度)
        /// </summary>
        [ObservableProperty]
        private int _pointCount = 1000;

        /// <summary>
        /// 颜色
        /// </summary>
        [ObservableProperty]
        private string _color = "#FF0000";

        /// <summary>
        /// 宽度
        /// </summary>
        [ObservableProperty]
        private float _width = 2.0f;

        /// <summary>
        /// 线条的样式
        /// </summary>
        [ObservableProperty]
        public LineDefinition.LineType _style = LineDefinition.LineType.Solid;

        [ObservableProperty]
        private string? _formulaErrorMessage;

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
                FormulaErrorMessage = LanguageService.Instance["invalid_formula"];
            }
        }
    }
}
