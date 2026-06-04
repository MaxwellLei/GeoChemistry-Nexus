using ScottPlot;
using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 基于渲染坐标空间网格的吸附点索引，用于加速鼠标附近吸附点查询。
    /// </summary>
    internal sealed class PlotSnapSpatialIndex
    {
        private readonly List<Coordinates> _renderPoints = new();
        private readonly Dictionary<(int X, int Y), List<int>> _grid = new();
        private double _minX;
        private double _minY;
        private double _cellWidth = 1;
        private double _cellHeight = 1;

        public void Build(IReadOnlyList<Coordinates> dataPoints, Plot plot)
        {
            _renderPoints.Clear();
            _grid.Clear();

            if (dataPoints == null || dataPoints.Count == 0 || plot == null)
            {
                return;
            }

            _renderPoints.Capacity = dataPoints.Count;
            foreach (var dataPoint in dataPoints)
            {
                _renderPoints.Add(PlotTransformHelper.ToRenderCoordinates(plot, dataPoint));
            }

            var limits = plot.Axes.GetLimits();
            _minX = limits.Left;
            _minY = limits.Bottom;

            double rangeX = Math.Max(limits.Right - limits.Left, 1e-9);
            double rangeY = Math.Max(limits.Top - limits.Bottom, 1e-9);
            int targetCells = Math.Clamp((int)Math.Ceiling(Math.Sqrt(_renderPoints.Count)), 8, 64);

            _cellWidth = rangeX / targetCells;
            _cellHeight = rangeY / targetCells;

            for (int i = 0; i < _renderPoints.Count; i++)
            {
                var cell = GetCell(_renderPoints[i]);
                if (!_grid.TryGetValue(cell, out var indices))
                {
                    indices = new List<int>();
                    _grid[cell] = indices;
                }

                indices.Add(i);
            }
        }

        public Coordinates? FindNearest(Pixel mousePixel, Plot plot, double snapDistancePixels)
        {
            if (_renderPoints.Count == 0 || plot == null)
            {
                return null;
            }

            var mouseCoordinates = plot.GetCoordinates(mousePixel);
            var centerCell = GetCell(mouseCoordinates);
            double minDistanceSq = snapDistancePixels * snapDistancePixels;
            Coordinates? bestSnap = null;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var key = (centerCell.X + dx, centerCell.Y + dy);
                    if (!_grid.TryGetValue(key, out var indices))
                    {
                        continue;
                    }

                    foreach (int index in indices)
                    {
                        var renderPoint = _renderPoints[index];
                        Pixel pointPixel = plot.GetPixel(renderPoint);
                        double deltaX = pointPixel.X - mousePixel.X;
                        double deltaY = pointPixel.Y - mousePixel.Y;
                        double distanceSq = deltaX * deltaX + deltaY * deltaY;

                        if (distanceSq < minDistanceSq)
                        {
                            minDistanceSq = distanceSq;
                            bestSnap = renderPoint;
                        }
                    }
                }
            }

            return bestSnap;
        }

        private (int X, int Y) GetCell(Coordinates point)
        {
            int cellX = (int)Math.Floor((point.X - _minX) / _cellWidth);
            int cellY = (int)Math.Floor((point.Y - _minY) / _cellHeight);
            return (cellX, cellY);
        }
    }
}
