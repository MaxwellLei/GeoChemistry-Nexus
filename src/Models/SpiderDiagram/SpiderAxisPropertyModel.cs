using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Extensions.ScottPlotExtensions;
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
        /// 标签字体
        /// </summary>
        [ObservableProperty]
        private string _family = "Arial";

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
        /// 子标题文本
        /// </summary>
        [ObservableProperty]
        private string _subLabel = string.Empty;

        /// <summary>
        /// 子标题字体大小
        /// </summary>
        [ObservableProperty]
        private float _subLabelSize = 14f;

        /// <summary>
        /// 子标题颜色 (Hex)
        /// </summary>
        [ObservableProperty]
        private string _subLabelColor = "#000000";

        /// <summary>
        /// 子标题粗体
        /// </summary>
        [ObservableProperty]
        private bool _subLabelBold = false;

        /// <summary>
        /// 子标题斜体
        /// </summary>
        [ObservableProperty]
        private bool _subLabelItalic = false;

        /// <summary>
        /// 刻度标签字体
        /// </summary>
        [ObservableProperty]
        private string _tickFontFamily = "Arial";

        /// <summary>
        /// 刻度标签字体大小
        /// </summary>
        [ObservableProperty]
        private float _tickFontSize = 12f;

        /// <summary>
        /// 刻度标签颜色 (Hex)
        /// </summary>
        [ObservableProperty]
        private string _tickColor = "#000000";

        /// <summary>
        /// 刻度标签粗体
        /// </summary>
        [ObservableProperty]
        private bool _tickIsBold = false;

        /// <summary>
        /// 刻度标签斜体
        /// </summary>
        [ObservableProperty]
        private bool _tickIsItalic = false;

        public SpiderAxisPropertyModel()
        {
        }

        public SpiderAxisPropertyModel(IAxis axis, ScottPlot.WPF.WpfPlot? wpfPlot = null)
        {
            _axis = axis;

            // 从轴读取初始值
            _label = axis.Label.Text ?? string.Empty;
            _family = axis.Label.FontName ?? "Arial";
            _fontSize = axis.Label.FontSize;
            _isBold = axis.Label.Bold;
            _isItalic = axis.Label.Italic;
            _color = axis.Label.ForeColor.ToHex();

            var subLabelStyle = GetSubLabelStyle();
            if (subLabelStyle != null)
            {
                _subLabel = GetSubLabelText() ?? string.Empty;
                _subLabelSize = subLabelStyle.FontSize;
                _subLabelColor = subLabelStyle.ForeColor.ToHex();
                _subLabelBold = subLabelStyle.Bold;
                _subLabelItalic = subLabelStyle.Italic;
            }

            _tickFontFamily = axis.TickLabelStyle.FontName ?? "Arial";
            _tickFontSize = axis.TickLabelStyle.FontSize;
            _tickColor = axis.TickLabelStyle.ForeColor.ToHex();
            _tickIsBold = axis.TickLabelStyle.Bold;
            _tickIsItalic = axis.TickLabelStyle.Italic;
        }

        partial void OnLabelChanged(string value)
        {
            if (_axis == null) return;
            _axis.Label.Text = value;
            _axis.Label.FontName = ScottPlot.Fonts.Detect(value);
            Family = _axis.Label.FontName;
        }

        partial void OnFamilyChanged(string value)
        {
            if (_axis == null || string.IsNullOrWhiteSpace(value)) return;
            _axis.Label.FontName = value;
            ApplySubtitleFontFamily(value);
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

        partial void OnSubLabelChanged(string value)
        {
            SetSubLabelText(value ?? string.Empty);
        }

        partial void OnSubLabelSizeChanged(float value)
        {
            var style = GetSubLabelStyle();
            if (style == null) return;
            style.FontSize = value;
        }

        partial void OnSubLabelColorChanged(string value)
        {
            var style = GetSubLabelStyle();
            if (style == null) return;
            try
            {
                style.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(value));
            }
            catch { }
        }

        partial void OnSubLabelBoldChanged(bool value)
        {
            var style = GetSubLabelStyle();
            if (style == null) return;
            style.Bold = value;
        }

        partial void OnSubLabelItalicChanged(bool value)
        {
            var style = GetSubLabelStyle();
            if (style == null) return;
            style.Italic = value;
        }

        partial void OnTickFontFamilyChanged(string value)
        {
            if (_axis == null || string.IsNullOrWhiteSpace(value)) return;
            _axis.TickLabelStyle.FontName = value;
        }

        partial void OnTickFontSizeChanged(float value)
        {
            if (_axis == null) return;
            _axis.TickLabelStyle.FontSize = value;
        }

        partial void OnTickColorChanged(string value)
        {
            if (_axis == null) return;
            try
            {
                _axis.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(value));
            }
            catch { }
        }

        partial void OnTickIsBoldChanged(bool value)
        {
            if (_axis == null) return;
            _axis.TickLabelStyle.Bold = value;
        }

        partial void OnTickIsItalicChanged(bool value)
        {
            if (_axis == null) return;
            _axis.TickLabelStyle.Italic = value;
        }

        private LabelStyle? GetSubLabelStyle()
        {
            return _axis switch
            {
                LeftAxisWithSubtitle left => left.SubLabelStyle,
                RightAxisWithSubtitle right => right.SubLabelStyle,
                BottomAxisWithSubtitle bottom => bottom.SubLabelStyle,
                TopAxisWithSubtitle top => top.SubLabelStyle,
                _ => null
            };
        }

        private string? GetSubLabelText()
        {
            return _axis switch
            {
                LeftAxisWithSubtitle left => left.SubLabelText,
                RightAxisWithSubtitle right => right.SubLabelText,
                BottomAxisWithSubtitle bottom => bottom.SubLabelText,
                TopAxisWithSubtitle top => top.SubLabelText,
                _ => null
            };
        }

        private void SetSubLabelText(string value)
        {
            switch (_axis)
            {
                case LeftAxisWithSubtitle left:
                    left.SubLabelText = value;
                    break;
                case RightAxisWithSubtitle right:
                    right.SubLabelText = value;
                    break;
                case BottomAxisWithSubtitle bottom:
                    bottom.SubLabelText = value;
                    break;
                case TopAxisWithSubtitle top:
                    top.SubLabelText = value;
                    break;
            }
        }

        private void ApplySubtitleFontFamily(string fontFamily)
        {
            var style = GetSubLabelStyle();
            if (style == null) return;
            style.FontName = fontFamily;
        }
    }
}
