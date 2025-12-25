using ScottPlot;
using ScottPlot.AxisPanels;
using SkiaSharp;
using System.Linq;

namespace GeoChemistryNexus.Extensions.ScottPlotExtensions
{
    public class LeftAxisWithSubtitle : YAxisBase
    {
        public override Edge Edge => Edge.Left;
        public string SubLabelText { get => SubLabelStyle.Text; set => SubLabelStyle.Text = value; }
        public LabelStyle SubLabelStyle { get; set; } = new() { Rotation = -90, Alignment = Alignment.UpperCenter };

        public LeftAxisWithSubtitle()
        {
            TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
        }

        public override float Measure()
        {
            if (!IsVisible) return 0;
            if (!Range.HasBeenSet) return SizeWhenNoData;

            using SKPaint paint = new SKPaint();
            
            float maxTickLabelWidth = TickGenerator.Ticks.Length > 0
                ? TickGenerator.Ticks.Select(x => TickLabelStyle.Measure(x.Label, paint).Width).Max()
                : 0;

            float axisLabelHeight = LabelStyle.Measure(LabelText, paint).LineHeight
                                    + PaddingBetweenTickAndAxisLabels.Horizontal
                                    + PaddingOutsideAxisLabels.Horizontal;
            
            if (!string.IsNullOrEmpty(SubLabelText))
            {
                 axisLabelHeight += SubLabelStyle.Measure(SubLabelText, paint).LineHeight;
            }

            return maxTickLabelWidth + axisLabelHeight;
        }

        public override void Render(RenderPack rp, float size, float offset)
        {
            if (!IsVisible) return;

            PixelRect panelRect = GetPanelRect(rp.DataRect, size, offset);
            float x = panelRect.Left + PaddingOutsideAxisLabels.Horizontal;
            
            float subLabelHeight = 0;
            if (!string.IsNullOrEmpty(SubLabelText))
            {
                // Sub Label (Outer / Left)
                subLabelHeight = SubLabelStyle.Measure(SubLabelText, rp.Paint).LineHeight;
                Pixel subLabelPoint = new(x, rp.DataRect.VerticalCenter);
                SubLabelStyle.Alignment = Alignment.UpperCenter;
                SubLabelStyle.Render(rp.Canvas, subLabelPoint, rp.Paint);
            }

            // Main Label (Inner / Right)
            Pixel labelPoint = new(x + subLabelHeight, rp.DataRect.VerticalCenter);
            LabelStyle.Alignment = Alignment.UpperCenter;
            LabelStyle.Render(rp.Canvas, labelPoint, rp.Paint);

            DrawTicks(rp, TickLabelStyle, panelRect, TickGenerator.Ticks, this, MajorTickStyle, MinorTickStyle);
            DrawFrame(rp, panelRect, Edge, FrameLineStyle);
        }
    }

    public class RightAxisWithSubtitle : YAxisBase
    {
        public override Edge Edge => Edge.Right;
        public string SubLabelText { get => SubLabelStyle.Text; set => SubLabelStyle.Text = value; }
        public LabelStyle SubLabelStyle { get; set; } = new() { Rotation = 90, Alignment = Alignment.UpperCenter };

        public RightAxisWithSubtitle()
        {
            TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
        }

        public override float Measure()
        {
            if (!IsVisible) return 0;
            if (!Range.HasBeenSet) return SizeWhenNoData;

            using SKPaint paint = new SKPaint();
            
            float maxTickLabelWidth = TickGenerator.Ticks.Length > 0
                ? TickGenerator.Ticks.Select(x => TickLabelStyle.Measure(x.Label, paint).Width).Max()
                : 0;

            float axisLabelHeight = LabelStyle.Measure(LabelText, paint).LineHeight
                                    + PaddingBetweenTickAndAxisLabels.Horizontal
                                    + PaddingOutsideAxisLabels.Horizontal;
            
            if (!string.IsNullOrEmpty(SubLabelText))
            {
                 axisLabelHeight += SubLabelStyle.Measure(SubLabelText, paint).LineHeight;
            }

            return maxTickLabelWidth + axisLabelHeight;
        }

        public override void Render(RenderPack rp, float size, float offset)
        {
            if (!IsVisible) return;

            PixelRect panelRect = GetPanelRect(rp.DataRect, size, offset);
            float x = panelRect.Right - PaddingOutsideAxisLabels.Horizontal;
            
            float subLabelHeight = 0;
            if (!string.IsNullOrEmpty(SubLabelText))
            {
                // Sub Label (Outer / Right)
                subLabelHeight = SubLabelStyle.Measure(SubLabelText, rp.Paint).LineHeight;
                Pixel subLabelPoint = new(x, rp.DataRect.VerticalCenter);
                SubLabelStyle.Alignment = Alignment.UpperCenter;
                SubLabelStyle.Render(rp.Canvas, subLabelPoint, rp.Paint);
            }

            // Main Label (Inner / Left)
            Pixel labelPoint = new(x - subLabelHeight, rp.DataRect.VerticalCenter);
            LabelStyle.Alignment = Alignment.UpperCenter;
            LabelStyle.Render(rp.Canvas, labelPoint, rp.Paint);

            DrawTicks(rp, TickLabelStyle, panelRect, TickGenerator.Ticks, this, MajorTickStyle, MinorTickStyle);
            DrawFrame(rp, panelRect, Edge, FrameLineStyle);
        }
    }

    public class BottomAxisWithSubtitle : XAxisBase
    {
        public override Edge Edge => Edge.Bottom;
        public string SubLabelText { get => SubLabelStyle.Text; set => SubLabelStyle.Text = value; }
        public LabelStyle SubLabelStyle { get; set; } = new() { Alignment = Alignment.UpperCenter };

        public BottomAxisWithSubtitle()
        {
            TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
        }

        public override float Measure()
        {
            if (!IsVisible) return 0;
            if (!Range.HasBeenSet) return SizeWhenNoData;

            using SKPaint paint = new SKPaint();

            float maxTickLabelHeight = TickGenerator.Ticks.Length > 0
                ? TickGenerator.Ticks.Select(x => TickLabelStyle.Measure(x.Label, paint).Height).Max()
                : 0;

            float axisLabelHeight = LabelStyle.Measure(LabelText, paint).LineHeight
                                    + PaddingBetweenTickAndAxisLabels.Vertical
                                    + PaddingOutsideAxisLabels.Vertical;

            if (!string.IsNullOrEmpty(SubLabelText))
            {
                 axisLabelHeight += SubLabelStyle.Measure(SubLabelText, paint).LineHeight;
            }

            return maxTickLabelHeight + axisLabelHeight;
        }

        public override void Render(RenderPack rp, float size, float offset)
        {
            if (!IsVisible) return;

            PixelRect panelRect = GetPanelRect(rp.DataRect, size, offset);
            float y = panelRect.Bottom - PaddingOutsideAxisLabels.Vertical;

            float subLabelHeight = 0;
            if (!string.IsNullOrEmpty(SubLabelText))
            {
                // Sub Label (Outer / Bottom)
                subLabelHeight = SubLabelStyle.Measure(SubLabelText, rp.Paint).LineHeight;
                Pixel subLabelPoint = new(rp.DataRect.HorizontalCenter, y);
                SubLabelStyle.Alignment = Alignment.LowerCenter;
                SubLabelStyle.Render(rp.Canvas, subLabelPoint, rp.Paint);
            }

            // Main Label (Inner / Top)
            Pixel labelPoint = new(rp.DataRect.HorizontalCenter, y - subLabelHeight);
            LabelStyle.Alignment = Alignment.LowerCenter; 
            LabelStyle.Render(rp.Canvas, labelPoint, rp.Paint);

            DrawTicks(rp, TickLabelStyle, panelRect, TickGenerator.Ticks, this, MajorTickStyle, MinorTickStyle);
            DrawFrame(rp, panelRect, Edge, FrameLineStyle);
        }
    }
    
    public class TopAxisWithSubtitle : XAxisBase
    {
        public override Edge Edge => Edge.Top;
        public string SubLabelText { get => SubLabelStyle.Text; set => SubLabelStyle.Text = value; }
        public LabelStyle SubLabelStyle { get; set; } = new() { Alignment = Alignment.LowerCenter };

        public TopAxisWithSubtitle()
        {
            TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
        }

        public override float Measure()
        {
            if (!IsVisible) return 0;
            if (!Range.HasBeenSet) return SizeWhenNoData;

            using SKPaint paint = new SKPaint();

            float maxTickLabelHeight = TickGenerator.Ticks.Length > 0
                ? TickGenerator.Ticks.Select(x => TickLabelStyle.Measure(x.Label, paint).Height).Max()
                : 0;

            float axisLabelHeight = LabelStyle.Measure(LabelText, paint).LineHeight
                                    + PaddingBetweenTickAndAxisLabels.Vertical
                                    + PaddingOutsideAxisLabels.Vertical;

            if (!string.IsNullOrEmpty(SubLabelText))
            {
                 axisLabelHeight += SubLabelStyle.Measure(SubLabelText, paint).LineHeight;
            }

            return maxTickLabelHeight + axisLabelHeight;
        }

        public override void Render(RenderPack rp, float size, float offset)
        {
            if (!IsVisible) return;

            PixelRect panelRect = GetPanelRect(rp.DataRect, size, offset);
            float y = panelRect.Top + PaddingOutsideAxisLabels.Vertical;

            float subLabelHeight = 0;
            if (!string.IsNullOrEmpty(SubLabelText))
            {
                // Sub Label (Outer / Top)
                subLabelHeight = SubLabelStyle.Measure(SubLabelText, rp.Paint).LineHeight;
                Pixel subLabelPoint = new(rp.DataRect.HorizontalCenter, y);
                SubLabelStyle.Alignment = Alignment.UpperCenter;
                SubLabelStyle.Render(rp.Canvas, subLabelPoint, rp.Paint);
            }

            // Main Label (Inner / Bottom)
            Pixel labelPoint = new(rp.DataRect.HorizontalCenter, y + subLabelHeight);
            LabelStyle.Alignment = Alignment.UpperCenter; 
            LabelStyle.Render(rp.Canvas, labelPoint, rp.Paint);

            DrawTicks(rp, TickLabelStyle, panelRect, TickGenerator.Ticks, this, MajorTickStyle, MinorTickStyle);
            DrawFrame(rp, panelRect, Edge, FrameLineStyle);
        }
    }
}
