using ScottPlot;
using SkiaSharp;
using System;

namespace GeoChemistryNexus.Helpers.PlotMarkers
{
    internal abstract class PolygonMarkerBase : IMarker
    {
        protected abstract SKPoint[] CreatePoints(Pixel center, float radius);
        protected abstract bool IsFilled { get; }

        public void Render(SKCanvas canvas, SKPaint paint, Pixel center, float size, MarkerStyle markerStyle)
        {
            float radius = size / 2f;
            if (radius <= 0)
            {
                return;
            }

            SKPoint[] points = CreatePoints(center, radius);
            using var path = new SKPath();
            path.AddPoly(points);

            if (IsFilled)
            {
                var rect = new PixelRect(center, radius);
                Drawing.FillPath(canvas, paint, path, markerStyle.FillStyle, rect);
                Drawing.DrawPath(canvas, paint, path, markerStyle.OutlineStyle);
            }
            else
            {
                Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
            }
        }
    }

    internal abstract class LineCompoundMarkerBase : IMarker
    {
        protected abstract void DrawLines(SKCanvas canvas, SKPaint paint, Pixel center, float radius, MarkerStyle markerStyle);

        public void Render(SKCanvas canvas, SKPaint paint, Pixel center, float size, MarkerStyle markerStyle)
        {
            float radius = size / 2f;
            if (radius <= 0)
            {
                return;
            }

            DrawLines(canvas, paint, center, radius, markerStyle);
        }
    }

    internal static class MarkerGeometry
    {
        public static SKPoint[] RegularPolygon(Pixel center, float radius, int sides, float rotationDegrees = -90f)
        {
            var points = new SKPoint[sides];
            double start = rotationDegrees * Math.PI / 180.0;
            for (int i = 0; i < sides; i++)
            {
                double angle = start + i * (2 * Math.PI / sides);
                points[i] = new SKPoint(
                    center.X + radius * (float)Math.Cos(angle),
                    center.Y + radius * (float)Math.Sin(angle));
            }
            return points;
        }

        public static SKPoint[] Star(Pixel center, float outerRadius, int points = 5, float innerRatio = 0.45f)
        {
            var result = new SKPoint[points * 2];
            float innerRadius = outerRadius * innerRatio;
            double start = -Math.PI / 2;
            for (int i = 0; i < points * 2; i++)
            {
                double angle = start + i * Math.PI / points;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                result[i] = new SKPoint(
                    center.X + radius * (float)Math.Cos(angle),
                    center.Y + radius * (float)Math.Sin(angle));
            }
            return result;
        }

        public static SKPoint[] TriangleLeft(Pixel center, float radius)
        {
            return new[]
            {
                new SKPoint(center.X - radius, center.Y),
                new SKPoint(center.X + radius * 0.7f, center.Y - radius),
                new SKPoint(center.X + radius * 0.7f, center.Y + radius),
            };
        }

        public static SKPoint[] TriangleRight(Pixel center, float radius)
        {
            return new[]
            {
                new SKPoint(center.X + radius, center.Y),
                new SKPoint(center.X - radius * 0.7f, center.Y - radius),
                new SKPoint(center.X - radius * 0.7f, center.Y + radius),
            };
        }

        public static SKPoint[] Bowtie(Pixel center, float radius)
        {
            // 自交四边形，填充后呈蝴蝶结/沙漏形
            return new[]
            {
                new SKPoint(center.X - radius, center.Y - radius),
                new SKPoint(center.X + radius, center.Y - radius),
                new SKPoint(center.X - radius, center.Y + radius),
                new SKPoint(center.X + radius, center.Y + radius),
            };
        }

        public static SKPoint[] HorizontalBowtie(Pixel center, float radius)
        {
            return new[]
            {
                new SKPoint(center.X - radius, center.Y - radius),
                new SKPoint(center.X - radius, center.Y + radius),
                new SKPoint(center.X + radius, center.Y - radius),
                new SKPoint(center.X + radius, center.Y + radius),
            };
        }

        public static SKPoint[] Parallelogram(Pixel center, float radius)
        {
            float skew = radius * 0.35f;
            return new[]
            {
                new SKPoint(center.X - radius + skew, center.Y - radius * 0.7f),
                new SKPoint(center.X + radius + skew, center.Y - radius * 0.7f),
                new SKPoint(center.X + radius - skew, center.Y + radius * 0.7f),
                new SKPoint(center.X - radius - skew, center.Y + radius * 0.7f),
            };
        }

        public static SKPoint[] Trapezoid(Pixel center, float radius)
        {
            return new[]
            {
                new SKPoint(center.X - radius * 0.55f, center.Y - radius * 0.7f),
                new SKPoint(center.X + radius * 0.55f, center.Y - radius * 0.7f),
                new SKPoint(center.X + radius, center.Y + radius * 0.7f),
                new SKPoint(center.X - radius, center.Y + radius * 0.7f),
            };
        }

        public static SKPath CreateHalfCirclePath(Pixel center, float radius, float startAngleDegrees, float sweepAngleDegrees)
        {
            var path = new SKPath();
            var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
            path.AddArc(rect, startAngleDegrees, sweepAngleDegrees);
            path.Close();
            return path;
        }
    }

    internal sealed class FilledStarMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Star(center, radius);
    }

    internal sealed class OpenStarMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Star(center, radius);
    }

    internal sealed class FilledStar4Marker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Star(center, radius, 4, 0.4f);
    }

    internal sealed class OpenStar4Marker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Star(center, radius, 4, 0.4f);
    }

    internal sealed class FilledStar6Marker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Star(center, radius, 6, 0.5f);
    }

    internal sealed class OpenStar6Marker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Star(center, radius, 6, 0.5f);
    }

    internal sealed class FilledHexagonMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.RegularPolygon(center, radius, 6, 0f);
    }

    internal sealed class OpenHexagonMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.RegularPolygon(center, radius, 6, 0f);
    }

    internal sealed class FilledPentagonMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.RegularPolygon(center, radius, 5);
    }

    internal sealed class OpenPentagonMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.RegularPolygon(center, radius, 5);
    }

    internal sealed class FilledOctagonMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.RegularPolygon(center, radius, 8, -22.5f);
    }

    internal sealed class OpenOctagonMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.RegularPolygon(center, radius, 8, -22.5f);
    }

    internal sealed class FilledTriangleLeftMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.TriangleLeft(center, radius);
    }

    internal sealed class OpenTriangleLeftMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.TriangleLeft(center, radius);
    }

    internal sealed class FilledTriangleRightMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.TriangleRight(center, radius);
    }

    internal sealed class OpenTriangleRightMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.TriangleRight(center, radius);
    }

    internal sealed class FilledBowtieMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Bowtie(center, radius);
    }

    internal sealed class OpenBowtieMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Bowtie(center, radius);
    }

    internal sealed class FilledHorizontalBowtieMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.HorizontalBowtie(center, radius);
    }

    internal sealed class OpenHorizontalBowtieMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.HorizontalBowtie(center, radius);
    }

    internal sealed class FilledParallelogramMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Parallelogram(center, radius);
    }

    internal sealed class OpenParallelogramMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Parallelogram(center, radius);
    }

    internal sealed class FilledTrapezoidMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => true;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Trapezoid(center, radius);
    }

    internal sealed class OpenTrapezoidMarker : PolygonMarkerBase
    {
        protected override bool IsFilled => false;
        protected override SKPoint[] CreatePoints(Pixel center, float radius) => MarkerGeometry.Trapezoid(center, radius);
    }

    internal abstract class HalfCircleMarkerBase : IMarker
    {
        protected abstract bool IsFilled { get; }
        protected abstract float StartAngle { get; }
        protected abstract float SweepAngle { get; }

        public void Render(SKCanvas canvas, SKPaint paint, Pixel center, float size, MarkerStyle markerStyle)
        {
            float radius = size / 2f;
            if (radius <= 0)
            {
                return;
            }

            using var path = MarkerGeometry.CreateHalfCirclePath(center, radius, StartAngle, SweepAngle);
            if (IsFilled)
            {
                var rect = new PixelRect(center, radius);
                Drawing.FillPath(canvas, paint, path, markerStyle.FillStyle, rect);
                Drawing.DrawPath(canvas, paint, path, markerStyle.OutlineStyle);
            }
            else
            {
                Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
            }
        }
    }

    internal sealed class FilledHalfCircleUpMarker : HalfCircleMarkerBase
    {
        protected override bool IsFilled => true;
        protected override float StartAngle => 180f;
        protected override float SweepAngle => 180f;
    }

    internal sealed class OpenHalfCircleUpMarker : HalfCircleMarkerBase
    {
        protected override bool IsFilled => false;
        protected override float StartAngle => 180f;
        protected override float SweepAngle => 180f;
    }

    internal sealed class FilledHalfCircleDownMarker : HalfCircleMarkerBase
    {
        protected override bool IsFilled => true;
        protected override float StartAngle => 0f;
        protected override float SweepAngle => 180f;
    }

    internal sealed class OpenHalfCircleDownMarker : HalfCircleMarkerBase
    {
        protected override bool IsFilled => false;
        protected override float StartAngle => 0f;
        protected override float SweepAngle => 180f;
    }

    internal sealed class FilledHalfCircleLeftMarker : HalfCircleMarkerBase
    {
        protected override bool IsFilled => true;
        protected override float StartAngle => 90f;
        protected override float SweepAngle => 180f;
    }

    internal sealed class OpenHalfCircleLeftMarker : HalfCircleMarkerBase
    {
        protected override bool IsFilled => false;
        protected override float StartAngle => 90f;
        protected override float SweepAngle => 180f;
    }

    internal sealed class FilledHalfCircleRightMarker : HalfCircleMarkerBase
    {
        protected override bool IsFilled => true;
        protected override float StartAngle => 270f;
        protected override float SweepAngle => 180f;
    }

    internal sealed class OpenHalfCircleRightMarker : HalfCircleMarkerBase
    {
        protected override bool IsFilled => false;
        protected override float StartAngle => 270f;
        protected override float SweepAngle => 180f;
    }

    internal sealed class SquareWithCrossMarker : LineCompoundMarkerBase
    {
        protected override void DrawLines(SKCanvas canvas, SKPaint paint, Pixel center, float radius, MarkerStyle markerStyle)
        {
            using var path = new SKPath();
            path.AddPoly(new[]
            {
                new SKPoint(center.X - radius, center.Y - radius),
                new SKPoint(center.X + radius, center.Y - radius),
                new SKPoint(center.X + radius, center.Y + radius),
                new SKPoint(center.X - radius, center.Y + radius),
            });
            path.MoveTo(center.X, center.Y - radius);
            path.LineTo(center.X, center.Y + radius);
            path.MoveTo(center.X - radius, center.Y);
            path.LineTo(center.X + radius, center.Y);
            Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
        }
    }

    internal sealed class SquareWithEksMarker : LineCompoundMarkerBase
    {
        protected override void DrawLines(SKCanvas canvas, SKPaint paint, Pixel center, float radius, MarkerStyle markerStyle)
        {
            using var path = new SKPath();
            path.AddPoly(new[]
            {
                new SKPoint(center.X - radius, center.Y - radius),
                new SKPoint(center.X + radius, center.Y - radius),
                new SKPoint(center.X + radius, center.Y + radius),
                new SKPoint(center.X - radius, center.Y + radius),
            });
            path.MoveTo(center.X - radius * 0.7f, center.Y - radius * 0.7f);
            path.LineTo(center.X + radius * 0.7f, center.Y + radius * 0.7f);
            path.MoveTo(center.X - radius * 0.7f, center.Y + radius * 0.7f);
            path.LineTo(center.X + radius * 0.7f, center.Y - radius * 0.7f);
            Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
        }
    }

    internal sealed class SquareWithDotMarker : LineCompoundMarkerBase
    {
        protected override void DrawLines(SKCanvas canvas, SKPaint paint, Pixel center, float radius, MarkerStyle markerStyle)
        {
            using var path = new SKPath();
            path.AddPoly(new[]
            {
                new SKPoint(center.X - radius, center.Y - radius),
                new SKPoint(center.X + radius, center.Y - radius),
                new SKPoint(center.X + radius, center.Y + radius),
                new SKPoint(center.X - radius, center.Y + radius),
            });
            Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
            Drawing.FillCircle(canvas, center, Math.Max(1.2f, radius * 0.18f), markerStyle.FillStyle, paint);
        }
    }

    internal sealed class DiamondWithCrossMarker : LineCompoundMarkerBase
    {
        protected override void DrawLines(SKCanvas canvas, SKPaint paint, Pixel center, float radius, MarkerStyle markerStyle)
        {
            using var path = new SKPath();
            path.AddPoly(new[]
            {
                new SKPoint(center.X, center.Y - radius),
                new SKPoint(center.X + radius, center.Y),
                new SKPoint(center.X, center.Y + radius),
                new SKPoint(center.X - radius, center.Y),
            });
            path.MoveTo(center.X, center.Y - radius * 0.55f);
            path.LineTo(center.X, center.Y + radius * 0.55f);
            path.MoveTo(center.X - radius * 0.55f, center.Y);
            path.LineTo(center.X + radius * 0.55f, center.Y);
            Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
        }
    }

    internal sealed class DiamondWithDotMarker : LineCompoundMarkerBase
    {
        protected override void DrawLines(SKCanvas canvas, SKPaint paint, Pixel center, float radius, MarkerStyle markerStyle)
        {
            using var path = new SKPath();
            path.AddPoly(new[]
            {
                new SKPoint(center.X, center.Y - radius),
                new SKPoint(center.X + radius, center.Y),
                new SKPoint(center.X, center.Y + radius),
                new SKPoint(center.X - radius, center.Y),
            });
            Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
            Drawing.FillCircle(canvas, center, Math.Max(1.2f, radius * 0.18f), markerStyle.FillStyle, paint);
        }
    }

    internal sealed class CircleWithSquareMarker : LineCompoundMarkerBase
    {
        protected override void DrawLines(SKCanvas canvas, SKPaint paint, Pixel center, float radius, MarkerStyle markerStyle)
        {
            Drawing.DrawCircle(canvas, center, radius, markerStyle.LineStyle, paint);
            float inner = radius * 0.55f;
            using var path = new SKPath();
            path.AddPoly(new[]
            {
                new SKPoint(center.X - inner, center.Y - inner),
                new SKPoint(center.X + inner, center.Y - inner),
                new SKPoint(center.X + inner, center.Y + inner),
                new SKPoint(center.X - inner, center.Y + inner),
            });
            Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
        }
    }

    internal sealed class PlusInDiamondMarker : LineCompoundMarkerBase
    {
        protected override void DrawLines(SKCanvas canvas, SKPaint paint, Pixel center, float radius, MarkerStyle markerStyle)
        {
            using var path = new SKPath();
            path.AddPoly(new[]
            {
                new SKPoint(center.X, center.Y - radius),
                new SKPoint(center.X + radius, center.Y),
                new SKPoint(center.X, center.Y + radius),
                new SKPoint(center.X - radius, center.Y),
            });
            path.MoveTo(center.X, center.Y - radius * 0.5f);
            path.LineTo(center.X, center.Y + radius * 0.5f);
            path.MoveTo(center.X - radius * 0.5f, center.Y);
            path.LineTo(center.X + radius * 0.5f, center.Y);
            Drawing.DrawPath(canvas, paint, path, markerStyle.LineStyle);
        }
    }
}
