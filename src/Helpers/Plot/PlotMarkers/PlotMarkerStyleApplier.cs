using GeoChemistryNexus.Models;
using ScottPlot;

namespace GeoChemistryNexus.Helpers.PlotMarkers
{
    /// <summary>
    /// 将 <see cref="PlotMarkerShape"/> 应用到 ScottPlot MarkerStyle。
    /// </summary>
    public static class PlotMarkerStyleApplier
    {
        private static readonly IMarker FilledStar = new FilledStarMarker();
        private static readonly IMarker OpenStar = new OpenStarMarker();
        private static readonly IMarker FilledStar4 = new FilledStar4Marker();
        private static readonly IMarker OpenStar4 = new OpenStar4Marker();
        private static readonly IMarker FilledStar6 = new FilledStar6Marker();
        private static readonly IMarker OpenStar6 = new OpenStar6Marker();
        private static readonly IMarker FilledHexagon = new FilledHexagonMarker();
        private static readonly IMarker OpenHexagon = new OpenHexagonMarker();
        private static readonly IMarker FilledPentagon = new FilledPentagonMarker();
        private static readonly IMarker OpenPentagon = new OpenPentagonMarker();
        private static readonly IMarker FilledOctagon = new FilledOctagonMarker();
        private static readonly IMarker OpenOctagon = new OpenOctagonMarker();
        private static readonly IMarker FilledTriangleLeft = new FilledTriangleLeftMarker();
        private static readonly IMarker OpenTriangleLeft = new OpenTriangleLeftMarker();
        private static readonly IMarker FilledTriangleRight = new FilledTriangleRightMarker();
        private static readonly IMarker OpenTriangleRight = new OpenTriangleRightMarker();
        private static readonly IMarker FilledBowtie = new FilledBowtieMarker();
        private static readonly IMarker OpenBowtie = new OpenBowtieMarker();
        private static readonly IMarker FilledHorizontalBowtie = new FilledHorizontalBowtieMarker();
        private static readonly IMarker OpenHorizontalBowtie = new OpenHorizontalBowtieMarker();
        private static readonly IMarker FilledParallelogram = new FilledParallelogramMarker();
        private static readonly IMarker OpenParallelogram = new OpenParallelogramMarker();
        private static readonly IMarker FilledTrapezoid = new FilledTrapezoidMarker();
        private static readonly IMarker OpenTrapezoid = new OpenTrapezoidMarker();
        private static readonly IMarker FilledHalfCircleUp = new FilledHalfCircleUpMarker();
        private static readonly IMarker OpenHalfCircleUp = new OpenHalfCircleUpMarker();
        private static readonly IMarker FilledHalfCircleDown = new FilledHalfCircleDownMarker();
        private static readonly IMarker OpenHalfCircleDown = new OpenHalfCircleDownMarker();
        private static readonly IMarker FilledHalfCircleLeft = new FilledHalfCircleLeftMarker();
        private static readonly IMarker OpenHalfCircleLeft = new OpenHalfCircleLeftMarker();
        private static readonly IMarker FilledHalfCircleRight = new FilledHalfCircleRightMarker();
        private static readonly IMarker OpenHalfCircleRight = new OpenHalfCircleRightMarker();
        private static readonly IMarker SquareWithCross = new SquareWithCrossMarker();
        private static readonly IMarker SquareWithEks = new SquareWithEksMarker();
        private static readonly IMarker SquareWithDot = new SquareWithDotMarker();
        private static readonly IMarker DiamondWithCross = new DiamondWithCrossMarker();
        private static readonly IMarker DiamondWithDot = new DiamondWithDotMarker();
        private static readonly IMarker CircleWithSquare = new CircleWithSquareMarker();
        private static readonly IMarker PlusInDiamond = new PlusInDiamondMarker();

        public static void Apply(
            MarkerStyle markerStyle,
            PlotMarkerShape shape,
            Color color,
            float strokeWidth,
            Color strokeColor)
        {
            if (TryGetScottPlotShape(shape, out var scottShape))
            {
                markerStyle.CustomRenderer = null;
                markerStyle.Shape = scottShape;
            }
            else
            {
                markerStyle.Shape = MarkerShape.None;
                markerStyle.CustomRenderer = GetCustomRenderer(shape);
            }

            // ScottPlot：实心形状用 OutlineStyle 描边；空心/线型形状用 LineStyle 画本体
            if (IsFilled(shape))
            {
                markerStyle.FillColor = color;
                markerStyle.LineColor = color;
                markerStyle.OutlineWidth = strokeWidth;
                markerStyle.OutlineColor = strokeColor;
                // 避免 LineStyle 干扰实心轮廓
                if (markerStyle.LineWidth <= 0)
                {
                    markerStyle.LineWidth = 1;
                }
            }
            else
            {
                // 空心/线型标记的视觉就是描边：线色走描边颜色，线宽走描边宽度
                markerStyle.LineColor = strokeColor;
                markerStyle.FillColor = NeedsFillDot(shape) ? strokeColor : Colors.Transparent;
                markerStyle.LineWidth = strokeWidth > 0 ? strokeWidth : 1.5f;
                markerStyle.OutlineWidth = 0;
                markerStyle.OutlineColor = strokeColor;
            }
        }

        public static bool IsFilled(PlotMarkerShape shape)
        {
            return shape switch
            {
                PlotMarkerShape.FilledCircle or
                PlotMarkerShape.FilledSquare or
                PlotMarkerShape.FilledTriangleUp or
                PlotMarkerShape.FilledTriangleDown or
                PlotMarkerShape.FilledDiamond or
                PlotMarkerShape.FilledStar or
                PlotMarkerShape.FilledStar4 or
                PlotMarkerShape.FilledStar6 or
                PlotMarkerShape.FilledHexagon or
                PlotMarkerShape.FilledPentagon or
                PlotMarkerShape.FilledOctagon or
                PlotMarkerShape.FilledTriangleLeft or
                PlotMarkerShape.FilledTriangleRight or
                PlotMarkerShape.FilledBowtie or
                PlotMarkerShape.FilledHorizontalBowtie or
                PlotMarkerShape.FilledParallelogram or
                PlotMarkerShape.FilledTrapezoid or
                PlotMarkerShape.FilledHalfCircleUp or
                PlotMarkerShape.FilledHalfCircleDown or
                PlotMarkerShape.FilledHalfCircleLeft or
                PlotMarkerShape.FilledHalfCircleRight => true,
                _ => false,
            };
        }

        private static bool NeedsFillDot(PlotMarkerShape shape)
        {
            return shape is PlotMarkerShape.DiamondWithDot or PlotMarkerShape.SquareWithDot;
        }

        private static bool TryGetScottPlotShape(PlotMarkerShape shape, out MarkerShape scottShape)
        {
            switch (shape)
            {
                case PlotMarkerShape.FilledCircle: scottShape = MarkerShape.FilledCircle; return true;
                case PlotMarkerShape.OpenCircle: scottShape = MarkerShape.OpenCircle; return true;
                case PlotMarkerShape.FilledSquare: scottShape = MarkerShape.FilledSquare; return true;
                case PlotMarkerShape.OpenSquare: scottShape = MarkerShape.OpenSquare; return true;
                case PlotMarkerShape.FilledTriangleUp: scottShape = MarkerShape.FilledTriangleUp; return true;
                case PlotMarkerShape.OpenTriangleUp: scottShape = MarkerShape.OpenTriangleUp; return true;
                case PlotMarkerShape.FilledTriangleDown: scottShape = MarkerShape.FilledTriangleDown; return true;
                case PlotMarkerShape.OpenTriangleDown: scottShape = MarkerShape.OpenTriangleDown; return true;
                case PlotMarkerShape.FilledDiamond: scottShape = MarkerShape.FilledDiamond; return true;
                case PlotMarkerShape.OpenDiamond: scottShape = MarkerShape.OpenDiamond; return true;
                case PlotMarkerShape.Cross: scottShape = MarkerShape.Cross; return true;
                case PlotMarkerShape.Eks: scottShape = MarkerShape.Eks; return true;
                case PlotMarkerShape.Asterisk: scottShape = MarkerShape.Asterisk; return true;
                case PlotMarkerShape.VerticalBar: scottShape = MarkerShape.VerticalBar; return true;
                case PlotMarkerShape.HorizontalBar: scottShape = MarkerShape.HorizontalBar; return true;
                case PlotMarkerShape.HashTag: scottShape = MarkerShape.HashTag; return true;
                case PlotMarkerShape.OpenCircleWithDot: scottShape = MarkerShape.OpenCircleWithDot; return true;
                case PlotMarkerShape.OpenCircleWithCross: scottShape = MarkerShape.OpenCircleWithCross; return true;
                case PlotMarkerShape.OpenCircleWithEks: scottShape = MarkerShape.OpenCircleWithEks; return true;
                case PlotMarkerShape.TriUp: scottShape = MarkerShape.TriUp; return true;
                case PlotMarkerShape.TriDown: scottShape = MarkerShape.TriDown; return true;
                default:
                    scottShape = MarkerShape.None;
                    return false;
            }
        }

        /// <summary>
        /// 将 ScottPlot 内置标记形状映射回 <see cref="PlotMarkerShape"/>（扩展自定义形状无法反推，回退实心圆）。
        /// </summary>
        public static PlotMarkerShape FromScottPlotShape(MarkerShape shape)
        {
            return shape switch
            {
                MarkerShape.FilledCircle => PlotMarkerShape.FilledCircle,
                MarkerShape.OpenCircle => PlotMarkerShape.OpenCircle,
                MarkerShape.FilledSquare => PlotMarkerShape.FilledSquare,
                MarkerShape.OpenSquare => PlotMarkerShape.OpenSquare,
                MarkerShape.FilledTriangleUp => PlotMarkerShape.FilledTriangleUp,
                MarkerShape.OpenTriangleUp => PlotMarkerShape.OpenTriangleUp,
                MarkerShape.FilledTriangleDown => PlotMarkerShape.FilledTriangleDown,
                MarkerShape.OpenTriangleDown => PlotMarkerShape.OpenTriangleDown,
                MarkerShape.FilledDiamond => PlotMarkerShape.FilledDiamond,
                MarkerShape.OpenDiamond => PlotMarkerShape.OpenDiamond,
                MarkerShape.Cross => PlotMarkerShape.Cross,
                MarkerShape.Eks => PlotMarkerShape.Eks,
                MarkerShape.Asterisk => PlotMarkerShape.Asterisk,
                MarkerShape.VerticalBar => PlotMarkerShape.VerticalBar,
                MarkerShape.HorizontalBar => PlotMarkerShape.HorizontalBar,
                MarkerShape.HashTag => PlotMarkerShape.HashTag,
                MarkerShape.OpenCircleWithDot => PlotMarkerShape.OpenCircleWithDot,
                MarkerShape.OpenCircleWithCross => PlotMarkerShape.OpenCircleWithCross,
                MarkerShape.OpenCircleWithEks => PlotMarkerShape.OpenCircleWithEks,
                MarkerShape.TriUp => PlotMarkerShape.TriUp,
                MarkerShape.TriDown => PlotMarkerShape.TriDown,
                _ => PlotMarkerShape.FilledCircle,
            };
        }

        private static IMarker GetCustomRenderer(PlotMarkerShape shape)
        {
            return shape switch
            {
                PlotMarkerShape.FilledStar => FilledStar,
                PlotMarkerShape.OpenStar => OpenStar,
                PlotMarkerShape.FilledStar4 => FilledStar4,
                PlotMarkerShape.OpenStar4 => OpenStar4,
                PlotMarkerShape.FilledStar6 => FilledStar6,
                PlotMarkerShape.OpenStar6 => OpenStar6,
                PlotMarkerShape.FilledHexagon => FilledHexagon,
                PlotMarkerShape.OpenHexagon => OpenHexagon,
                PlotMarkerShape.FilledPentagon => FilledPentagon,
                PlotMarkerShape.OpenPentagon => OpenPentagon,
                PlotMarkerShape.FilledOctagon => FilledOctagon,
                PlotMarkerShape.OpenOctagon => OpenOctagon,
                PlotMarkerShape.FilledTriangleLeft => FilledTriangleLeft,
                PlotMarkerShape.OpenTriangleLeft => OpenTriangleLeft,
                PlotMarkerShape.OpenTriangleRight => OpenTriangleRight,
                PlotMarkerShape.FilledTriangleRight => FilledTriangleRight,
                PlotMarkerShape.FilledBowtie => FilledBowtie,
                PlotMarkerShape.OpenBowtie => OpenBowtie,
                PlotMarkerShape.FilledHorizontalBowtie => FilledHorizontalBowtie,
                PlotMarkerShape.OpenHorizontalBowtie => OpenHorizontalBowtie,
                PlotMarkerShape.FilledParallelogram => FilledParallelogram,
                PlotMarkerShape.OpenParallelogram => OpenParallelogram,
                PlotMarkerShape.FilledTrapezoid => FilledTrapezoid,
                PlotMarkerShape.OpenTrapezoid => OpenTrapezoid,
                PlotMarkerShape.FilledHalfCircleUp => FilledHalfCircleUp,
                PlotMarkerShape.OpenHalfCircleUp => OpenHalfCircleUp,
                PlotMarkerShape.FilledHalfCircleDown => FilledHalfCircleDown,
                PlotMarkerShape.OpenHalfCircleDown => OpenHalfCircleDown,
                PlotMarkerShape.FilledHalfCircleLeft => FilledHalfCircleLeft,
                PlotMarkerShape.OpenHalfCircleLeft => OpenHalfCircleLeft,
                PlotMarkerShape.FilledHalfCircleRight => FilledHalfCircleRight,
                PlotMarkerShape.OpenHalfCircleRight => OpenHalfCircleRight,
                PlotMarkerShape.SquareWithCross => SquareWithCross,
                PlotMarkerShape.SquareWithEks => SquareWithEks,
                PlotMarkerShape.SquareWithDot => SquareWithDot,
                PlotMarkerShape.DiamondWithCross => DiamondWithCross,
                PlotMarkerShape.DiamondWithDot => DiamondWithDot,
                PlotMarkerShape.CircleWithSquare => CircleWithSquare,
                PlotMarkerShape.PlusInDiamond => PlusInDiamond,
                _ => FilledStar,
            };
        }
    }
}
