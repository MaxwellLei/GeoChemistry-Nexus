using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    public static class PlotTransformHelper
    {
        /// <summary>
        /// 判断坐标轴是否处于对数模式
        /// </summary>
        public static bool IsLogAxis(IAxis axis)
        {
            if (axis.TickGenerator is ScottPlot.TickGenerators.NumericAutomatic numTick)
            {
                return numTick.MinorTickGenerator is ScottPlot.TickGenerators.LogMinorTickGenerator;
            }
            return false;
        }

        /// <summary>
        /// 【渲染】将真实数据坐标转换为绘图坐标（如果是对数轴，则取Log）
        /// </summary>
        public static Coordinates ToRenderCoordinates(Plot plot, double realX, double realY)
        {
            // 三元图模式下，坐标已经是笛卡尔坐标，不需要对数转换
            var hasTriangularAxis = plot.GetPlottables()
                .OfType<ScottPlot.Plottables.TriangularAxis>()
                .Any();

            if (hasTriangularAxis)
            {
                // 三元图模式：直接返回坐标，不进行对数转换
                return new Coordinates(realX, realY);
            }

            var xAxis = plot.Axes.Bottom;
            var yAxis = plot.Axes.Left;

            double renderX = realX;
            double renderY = realY;

            // 处理 X 轴
            if (IsLogAxis(xAxis))
            {
                // 避免 Log(<=0) 报错
                renderX = realX > 0 ? Math.Log10(realX) : -10;
            }

            // 处理 Y 轴
            if (IsLogAxis(yAxis))
            {
                renderY = realY > 0 ? Math.Log10(realY) : -10;
            }

            return new Coordinates(renderX, renderY);
        }

        /// <summary>
        /// 【渲染】重载方法，直接接受 Coordinates 对象
        /// </summary>
        public static Coordinates ToRenderCoordinates(Plot plot, Coordinates realLocation)
        {
            return ToRenderCoordinates(plot, realLocation.X, realLocation.Y);
        }

        /// <summary>
        /// 【交互】将绘图坐标（鼠标位置）还原为真实数据坐标（如果是对数轴，则取10^x）
        /// </summary>
        public static Coordinates ToRealDataCoordinates(Plot plot, Coordinates mouseCoordinates)
        {
            // 三元图模式下，坐标已经是笛卡尔坐标，不需要对数转换
            var hasTriangularAxis = plot.GetPlottables()
                .OfType<ScottPlot.Plottables.TriangularAxis>()
                .Any();

            if (hasTriangularAxis)
            {
                // 三元图模式：直接返回坐标，不进行对数转换
                return new Coordinates(mouseCoordinates.X, mouseCoordinates.Y);
            }

            var xAxis = plot.Axes.Bottom;
            var yAxis = plot.Axes.Left;

            double realX = mouseCoordinates.X;
            double realY = mouseCoordinates.Y;

            if (IsLogAxis(xAxis))
            {
                realX = Math.Pow(10, mouseCoordinates.X);
            }

            if (IsLogAxis(yAxis))
            {
                realY = Math.Pow(10, mouseCoordinates.Y);
            }

            return new Coordinates(realX, realY);
        }
    }
}
