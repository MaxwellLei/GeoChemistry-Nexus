using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Services;
using ScottPlot;

namespace GeoChemistryNexus.Models.SpiderDiagram
{
    /// <summary>
    /// 蛛网图坐标轴属性模型，用于属性面板编辑
    /// </summary>
    public partial class SpiderAxisPropertyModel : ObservableObject
    {
        private IAxis? _axis;

        /// <summary>
        /// 轴标签
        /// </summary>
        [ObservableProperty]
        private string _label = string.Empty;

        /// <summary>
        /// 标签字体大小
        /// </summary>
        [ObservableProperty]
        private float _fontSize = 14f;

        /// <summary>
        /// 标签粗体
        /// </summary>
        [ObservableProperty]
        private bool _isBold = false;

        /// <summary>
        /// 标签斜体
        /// </summary>
        [ObservableProperty]
        private bool _isItalic = false;

        /// <summary>
        /// 标签颜色 (Hex)
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// 刻度标签字体大小
        /// </summary>
        [ObservableProperty]
        private float _tickFontSize = 12f;

        public SpiderAxisPropertyModel()
        {
        }

        public SpiderAxisPropertyModel(IAxis axis, ScottPlot.WPF.WpfPlot? wpfPlot = null)
        {
            _axis = axis;

            // 从轴读取初始值
            _label = axis.Label.Text ?? string.Empty;
            _fontSize = axis.Label.FontSize;
            _isBold = axis.Label.Bold;
            _isItalic = axis.Label.Italic;
            _color = axis.Label.ForeColor.ToHex();
            _tickFontSize = axis.TickLabelStyle.FontSize;
        }

        partial void OnLabelChanged(string value)
        {
            if (_axis == null) return;
            _axis.Label.Text = value;
            _axis.Label.FontName = ScottPlot.Fonts.Detect(value);
        }

        partial void OnFontSizeChanged(float value)
        {
            if (_axis == null) return;
            _axis.Label.FontSize = value;
        }

        partial void OnIsBoldChanged(bool value)
        {
            if (_axis == null) return;
            _axis.Label.Bold = value;
        }

        partial void OnIsItalicChanged(bool value)
        {
            if (_axis == null) return;
            _axis.Label.Italic = value;
        }

        partial void OnColorChanged(string value)
        {
            if (_axis == null) return;
            try
            {
                _axis.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(value));
            }
            catch { }
        }

        partial void OnTickFontSizeChanged(float value)
        {
            if (_axis == null) return;
            _axis.TickLabelStyle.FontSize = value;
        }
    }
}
