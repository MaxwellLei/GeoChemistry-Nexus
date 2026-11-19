using System.Collections.Generic;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 属性面板中坐标显示/编辑的三元图与笛卡尔坐标转换辅助类。
    /// </summary>
    public static class TernaryCoordinateHelper
    {
        public static bool IsTernaryMode => MainPlotViewModel.BaseMapType == "Ternary";

        public static string XAxisLabel => IsTernaryMode ? "A" : "X";

        public static string YAxisLabel => IsTernaryMode ? "B" : "Y";

        public static (double DisplayX, double DisplayY) CartesianToDisplay(double x, double y)
        {
            if (IsTernaryMode)
            {
                var (bottom, left) = MainPlotViewModel.ToTernary(x, y, MainPlotViewModel.Clockwise);
                return (bottom, left);
            }

            return (x, y);
        }

        public static (double X, double Y) DisplayToCartesian(double displayX, double displayY)
        {
            if (IsTernaryMode)
            {
                return MainPlotViewModel.ToCartesian(displayX, displayY, 1 - displayY - displayX);
            }

            return (displayX, displayY);
        }

        /// <summary>
        /// 保持 A/B/C 组分不变，把一个笛卡尔点从一种端元环绕方向换到另一种方向。
        /// </summary>
        public static bool TryRemap(double x, double y, bool fromClockwise, bool toClockwise, out double newX, out double newY)
        {
            newX = x;
            newY = y;
            if (fromClockwise == toClockwise || !double.IsFinite(x) || !double.IsFinite(y))
                return false;

            var (bottom, left) = MainPlotViewModel.ToTernary(x, y, fromClockwise);
            double right = 1 - bottom - left;
            (newX, newY) = MainPlotViewModel.ToCartesian(bottom, left, right, toClockwise);
            return true;
        }

        /// <summary>
        /// 按端元组分重算模板里已经画好的点、线、多边形、箭头、文本和注释。
        /// </summary>
        public static void RemapGeometry(GraphMapInfo? info, bool fromClockwise, bool toClockwise)
        {
            if (info == null || fromClockwise == toClockwise)
                return;

            RemapPoints(info.Points, fromClockwise, toClockwise);

            if (info.Lines != null)
            {
                foreach (var line in info.Lines)
                {
                    RemapPoint(line?.Start, fromClockwise, toClockwise);
                    RemapPoint(line?.End, fromClockwise, toClockwise);
                }
            }

            if (info.Polygons != null)
            {
                foreach (var polygon in info.Polygons)
                    RemapPoints(polygon?.Vertices, fromClockwise, toClockwise);
            }

            if (info.Arrows != null)
            {
                foreach (var arrow in info.Arrows)
                {
                    RemapPoint(arrow?.Start, fromClockwise, toClockwise);
                    RemapPoint(arrow?.End, fromClockwise, toClockwise);
                }
            }

            if (info.Texts != null)
            {
                foreach (var text in info.Texts)
                    RemapPoint(text?.StartAndEnd, fromClockwise, toClockwise);
            }

            if (info.Annotations != null)
            {
                foreach (var annotation in info.Annotations)
                {
                    if (annotation == null)
                        continue;
                    if (!TryRemap(annotation.X, annotation.Y, fromClockwise, toClockwise, out double x, out double y))
                        continue;
                    annotation.X = x;
                    annotation.Y = y;
                }
            }
        }

        private static void RemapPoints(IEnumerable<PointDefinition>? points, bool fromClockwise, bool toClockwise)
        {
            if (points == null)
                return;

            foreach (var point in points)
                RemapPoint(point, fromClockwise, toClockwise);
        }

        private static void RemapPoint(PointDefinition? point, bool fromClockwise, bool toClockwise)
        {
            if (point == null)
                return;
            if (!TryRemap(point.X, point.Y, fromClockwise, toClockwise, out double x, out double y))
                return;

            point.X = x;
            point.Y = y;
        }
    }
}


