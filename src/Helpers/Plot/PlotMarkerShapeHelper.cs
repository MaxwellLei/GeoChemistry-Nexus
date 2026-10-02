using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解投点形状列表与属性面板图标。
    /// </summary>
    public static class PlotMarkerShapeHelper
    {
        private static List<PlotMarkerShapeItem>? _cache;

        public static IReadOnlyList<PlotMarkerShapeItem> GetMarkerShapes()
        {
            if (_cache != null)
            {
                return _cache;
            }

            var list = new List<PlotMarkerShapeItem>();
            foreach (PlotMarkerShape shape in Enum.GetValues(typeof(PlotMarkerShape)))
            {
                list.Add(new PlotMarkerShapeItem(shape, GetIcon(shape)));
            }

            _cache = list;
            return list;
        }

        /// <summary>
        /// 清空图标缓存（形状路径更新后可调用）。
        /// </summary>
        public static void InvalidateCache()
        {
            _cache = null;
        }

        public static PlotMarkerShapeItem GetItem(PlotMarkerShape shape)
        {
            foreach (var item in GetMarkerShapes())
            {
                if (item.Shape == shape)
                {
                    return item;
                }
            }

            return GetMarkerShapes()[0];
        }

        private static Geometry GetIcon(PlotMarkerShape shape)
        {
            string pathData = shape switch
            {
                PlotMarkerShape.OpenCircle or PlotMarkerShape.FilledCircle
                    => "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z",
                PlotMarkerShape.OpenSquare or PlotMarkerShape.FilledSquare
                    => "M 1,1 H 9 V 9 H 1 Z",
                PlotMarkerShape.OpenTriangleUp or PlotMarkerShape.FilledTriangleUp
                    => "M 1,8 L 5,1 L 9,8 Z",
                PlotMarkerShape.OpenTriangleDown or PlotMarkerShape.FilledTriangleDown
                    => "M 1,2 L 5,9 L 9,2 Z",
                PlotMarkerShape.OpenDiamond or PlotMarkerShape.FilledDiamond
                    => "M 5,0 L 9,5 L 5,10 L 1,5 Z",
                PlotMarkerShape.Cross
                    => "M 1,5 H 9 M 5,1 V 9",
                PlotMarkerShape.Eks
                    => "M 2,2 L 8,8 M 2,8 L 8,2",
                PlotMarkerShape.Asterisk
                    => "M 1,5 H 9 M 5,1 V 9 M 2,2 L 8,8 M 2,8 L 8,2",
                PlotMarkerShape.VerticalBar
                    => "M 5,1 V 9",
                PlotMarkerShape.HorizontalBar
                    => "M 1,5 H 9",
                PlotMarkerShape.HashTag
                    => "M 3,1 V 9 M 7,1 V 9 M 1,3 H 9 M 1,7 H 9",
                PlotMarkerShape.OpenCircleWithDot
                    => "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z M 4.9,5 A 0.1,0.1 0 1 1 5.1,5 A 0.1,0.1 0 1 1 4.9,5 Z",
                PlotMarkerShape.OpenCircleWithCross
                    => "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z M 5,1 V 9 M 1,5 H 9",
                PlotMarkerShape.OpenCircleWithEks
                    => "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z M 2.5,2.5 L 7.5,7.5 M 2.5,7.5 L 7.5,2.5",
                PlotMarkerShape.TriUp
                    // 与 ScottPlot TriUp 一致：中心向「下 + 左上 + 右上」三条射线
                    => "M 5,5 L 5,9.5 M 5,5 L 1.5,2.8 M 5,5 L 8.5,2.8",
                PlotMarkerShape.TriDown
                    // 与 ScottPlot TriDown 一致：中心向「上 + 左下 + 右下」三条射线
                    => "M 5,5 L 5,0.5 M 5,5 L 1.5,7.2 M 5,5 L 8.5,7.2",
                PlotMarkerShape.OpenStar or PlotMarkerShape.FilledStar
                    => "M 5,0.5 L 6.2,3.6 L 9.5,3.8 L 7,6.1 L 7.8,9.5 L 5,7.8 L 2.2,9.5 L 3,6.1 L 0.5,3.8 L 3.8,3.6 Z",
                PlotMarkerShape.OpenStar4 or PlotMarkerShape.FilledStar4
                    => "M 5,0.5 L 6,4 L 9.5,5 L 6,6 L 5,9.5 L 4,6 L 0.5,5 L 4,4 Z",
                PlotMarkerShape.OpenStar6 or PlotMarkerShape.FilledStar6
                    => "M 5,0.4 L 5.9,3.2 L 8.9,2.2 L 7,5 L 8.9,7.8 L 5.9,6.8 L 5,9.6 L 4.1,6.8 L 1.1,7.8 L 3,5 L 1.1,2.2 L 4.1,3.2 Z",
                PlotMarkerShape.OpenHexagon or PlotMarkerShape.FilledHexagon
                    => "M 2,1 L 8,1 L 10,5 L 8,9 L 2,9 L 0,5 Z",
                PlotMarkerShape.OpenPentagon or PlotMarkerShape.FilledPentagon
                    => "M 5,0.5 L 9.5,3.6 L 7.8,8.8 L 2.2,8.8 L 0.5,3.6 Z",
                PlotMarkerShape.OpenOctagon or PlotMarkerShape.FilledOctagon
                    => "M 3,1 L 7,1 L 9,3 L 9,7 L 7,9 L 3,9 L 1,7 L 1,3 Z",
                PlotMarkerShape.OpenTriangleLeft or PlotMarkerShape.FilledTriangleLeft
                    => "M 1,5 L 9,1 L 9,9 Z",
                PlotMarkerShape.OpenTriangleRight or PlotMarkerShape.FilledTriangleRight
                    => "M 9,5 L 1,1 L 1,9 Z",
                PlotMarkerShape.OpenBowtie or PlotMarkerShape.FilledBowtie
                    => "M 1,1 L 9,1 L 1,9 L 9,9 Z",
                PlotMarkerShape.OpenHorizontalBowtie or PlotMarkerShape.FilledHorizontalBowtie
                    => "M 1,1 L 1,9 L 9,1 L 9,9 Z",
                PlotMarkerShape.OpenParallelogram or PlotMarkerShape.FilledParallelogram
                    => "M 2.5,1.5 L 9.5,1.5 L 7.5,8.5 L 0.5,8.5 Z",
                PlotMarkerShape.OpenTrapezoid or PlotMarkerShape.FilledTrapezoid
                    => "M 3,1.5 L 7,1.5 L 9.5,8.5 L 0.5,8.5 Z",
                PlotMarkerShape.OpenHalfCircleUp or PlotMarkerShape.FilledHalfCircleUp
                    => "M 1,5 A 4,4 0 0 1 9,5 Z",
                PlotMarkerShape.OpenHalfCircleDown or PlotMarkerShape.FilledHalfCircleDown
                    => "M 1,5 A 4,4 0 0 0 9,5 Z",
                PlotMarkerShape.OpenHalfCircleLeft or PlotMarkerShape.FilledHalfCircleLeft
                    => "M 5,1 A 4,4 0 0 0 5,9 Z",
                PlotMarkerShape.OpenHalfCircleRight or PlotMarkerShape.FilledHalfCircleRight
                    => "M 5,1 A 4,4 0 0 1 5,9 Z",
                PlotMarkerShape.SquareWithCross
                    => "M 1,1 H 9 V 9 H 1 Z M 5,1 V 9 M 1,5 H 9",
                PlotMarkerShape.SquareWithEks
                    => "M 1,1 H 9 V 9 H 1 Z M 2.5,2.5 L 7.5,7.5 M 2.5,7.5 L 7.5,2.5",
                PlotMarkerShape.SquareWithDot
                    => "M 1,1 H 9 V 9 H 1 Z M 4.9,5 A 0.15,0.15 0 1 1 5.1,5 A 0.15,0.15 0 1 1 4.9,5 Z",
                PlotMarkerShape.DiamondWithCross
                    => "M 5,0 L 9,5 L 5,10 L 1,5 Z M 5,2 V 8 M 2,5 H 8",
                PlotMarkerShape.DiamondWithDot
                    => "M 5,0 L 9,5 L 5,10 L 1,5 Z M 4.9,5 A 0.15,0.15 0 1 1 5.1,5 A 0.15,0.15 0 1 1 4.9,5 Z",
                PlotMarkerShape.CircleWithSquare
                    => "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z M 3.2,3.2 H 6.8 V 6.8 H 3.2 Z",
                PlotMarkerShape.PlusInDiamond
                    => "M 5,0 L 9,5 L 5,10 L 1,5 Z M 5,2.5 V 7.5 M 2.5,5 H 7.5",
                _ => "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z",
            };

            try
            {
                var geometry = Geometry.Parse(pathData);
                geometry.Freeze();
                return geometry;
            }
            catch
            {
                return Geometry.Empty;
            }
        }
    }
}
