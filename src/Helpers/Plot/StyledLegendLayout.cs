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
            ApplyStyle(items);
            var layout = _inner.GetLayout(legend, items, maxSize);
            ApplyStyle(layout.LegendItems);
            return layout;
        }

        private void ApplyStyle(LegendItem[]? items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                item.LabelBold = Bold;
                item.LabelItalic = Italic;
            }
        }

        /// <summary>
        /// 给图例设置粗体 / 斜体；样式全部关闭且尚未包装过布局时不改动图例，保持 ScottPlot 默认行为。
        /// </summary>
        public static void Apply(Legend legend, bool bold, bool italic)
        {
            if (legend.Layout is StyledLegendLayout styled)
            {
                styled.Bold = bold;
                styled.Italic = italic;
                return;
            }

            if (!bold && !italic)
            {
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
