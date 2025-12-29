using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Helpers
{
    public static class MarkerShapeHelper
    {
        private static List<MarkerShapeItem>? _cache;

        public static List<MarkerShapeItem> GetMarkerShapes()
        {
            if (_cache != null) return _cache;

            var list = new List<MarkerShapeItem>();
            foreach (MarkerShape shape in Enum.GetValues(typeof(MarkerShape)))
            {
                string s = shape.ToString();
                // 过滤掉 None, TriUp, TriDown
                if (s == "None" || s == "TriUp" || s == "TriDown")
                {
                    continue;
                }

                bool isFilled = IsFilled(shape);
                list.Add(new MarkerShapeItem(shape, GetIcon(shape), isFilled));
            }
            _cache = list;
            return list;
        }

        private static bool IsFilled(MarkerShape shape)
        {
            string s = shape.ToString();
            return s.StartsWith("Filled");
        }

        private static Geometry GetIcon(MarkerShape shape)
        {
            string pathData = "";
            string s = shape.ToString();

            if (s == "OpenCircle" || s == "FilledCircle")
            {
                // 中心 (5,5), 半径 4
                pathData = "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z";
            }
            else if (s == "OpenSquare" || s == "FilledSquare")
            {
                pathData = "M 1,1 H 9 V 9 H 1 Z";
            }
            else if (s == "OpenTriangleUp" || s == "FilledTriangleUp")
            {
                pathData = "M 1,8 L 5,1 L 9,8 Z";
            }
            else if (s == "OpenTriangleDown" || s == "FilledTriangleDown")
            {
                pathData = "M 1,2 L 5,9 L 9,2 Z";
            }
            else if (s == "OpenDiamond" || s == "FilledDiamond")
            {
                pathData = "M 5,0 L 9,5 L 5,10 L 1,5 Z";
            }
            else if (s == "Cross") // +
            {
                pathData = "M 1,5 H 9 M 5,1 V 9";
            }
            else if (s == "Eks") // x
            {
                pathData = "M 2,2 L 8,8 M 2,8 L 8,2";
            }
            else if (s == "Asterisk")
            {
                pathData = "M 1,5 H 9 M 5,1 V 9 M 2,2 L 8,8 M 2,8 L 8,2";
            }
            else if (s == "VerticalBar")
            {
                pathData = "M 5,1 V 9";
            }
            else if (s == "HorizontalBar")
            {
                pathData = "M 1,5 H 9";
            }
            // HashTag
            else if (s.Contains("Hash")) 
            {
                // 标准井字格
                pathData = "M 3,1 V 9 M 7,1 V 9 M 1,3 H 9 M 1,7 H 9";
            }
            // OpenCircleWithDot: 圆 + 中心点
            else if (s.Contains("Circle") && s.Contains("Dot"))
            {
                 // 外圆 + 内点 (小圆模拟)
                 pathData = "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z M 4.9,5 A 0.1,0.1 0 1 1 5.1,5 A 0.1,0.1 0 1 1 4.9,5 Z";
            }
            // OpenCircleWithCross: 圆 + 十字
            else if (s.Contains("Circle") && s.Contains("Cross"))
            {
                pathData = "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z M 5,1 V 9 M 1,5 H 9";
            }
            // OpenCircleWithEks: 圆 + 叉
            else if (s.Contains("Circle") && s.Contains("Eks"))
            {
                pathData = "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z M 2.5,2.5 L 7.5,7.5 M 2.5,7.5 L 7.5,2.5";
            }
            else
            {
                // Default fallback
                pathData = "M 1,5 A 4,4 0 1 1 9,5 A 4,4 0 1 1 1,5 Z";
            }

            try
            {
                var geometry = Geometry.Parse(pathData);
                geometry.Freeze(); // 冻结以提高性能
                return geometry;
            }
            catch
            {
                return Geometry.Empty;
            }
        }
    }
}
