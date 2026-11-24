using ScottPlot;
using ScottPlot.LegendLayouts;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// ScottPlot 的图例没有整体的粗体 / 斜体开关（字体样式存在每个图例条目上，而条目每次绘制都会重新生成）。
    /// 这里包装默认的图例布局：在测量与绘制之前，把粗体 / 斜体套用到本次布局涉及的所有条目上。
    /// </summary>
    public sealed class StyledLegendLayout : ILegendLayout
    {
        private readonly ILegendLayout _inner;

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public StyledLegendLayout(ILegendLayout inner)
        {
            _inner = inner;
        }

        public LegendLayout GetLayout(Legend legend, LegendItem[] items, PixelSize maxSize)
        {
            ApplyStyle(legend, items);
            var layout = _inner.GetLayout(legend, items, maxSize);
            ApplyStyle(legend, layout.LegendItems);
            return layout;
        }

        private void ApplyStyle(Legend legend, LegendItem[]? items)
        {
            if (items == null)
            {
                return;
            }

            string legendFont = string.IsNullOrWhiteSpace(legend.FontName) ? Fonts.Default : legend.FontName;

            foreach (var item in items)
            {
                item.LabelBold = Bold;
                item.LabelItalic = Italic;

                // 图例文本来自数据（样品 / 分类名称），可能含有图例字体无法显示的字符：
                // 仅在图例字体不能显示该条目时，才按内容改用自动检测的字体
                string resolved = DiagramFontHelper.Resolve(legendFont, item.LabelText);
                if (!string.Equals(resolved, legendFont, System.StringComparison.OrdinalIgnoreCase))
                {
                    item.LabelFontName = resolved;
                }
            }
        }

        /// <summary>
        /// 给图例设置粗体 / 斜体，并包装布局以便按图例条目内容自动选择字体。
        /// </summary>
        public static void Apply(Legend legend, bool bold, bool italic)
        {
            if (legend.Layout is StyledLegendLayout styled)
            {
                styled.Bold = bold;
                styled.Italic = italic;
                return;
            }

            legend.Layout = new StyledLegendLayout(legend.Layout)
            {
                Bold = bold,
                Italic = italic
            };
        }
    }
}
