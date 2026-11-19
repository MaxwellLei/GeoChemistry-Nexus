using ScottPlot;
using ScottPlot.AxisRules;
using System;

namespace GeoChemistryNexus.Extensions.ScottPlotExtensions
{
    /// <summary>
    /// 三元图画在隐藏的二维坐标轴里。这条规则在渲染时把三角形尽量放大到数据区，
    /// 并保持像素等比，只留出刻度和标题需要的边距。用户滚轮放大后的视野会保留。
    /// </summary>
    public class TernaryPlotFitRule : IAxisRule
    {
        private readonly IXAxis _xAxis;
        private readonly IYAxis _yAxis;

        public TernaryPlotFitRule(IXAxis xAxis, IYAxis yAxis)
        {
            _xAxis = xAxis;
            _yAxis = yAxis;
        }

        public void Apply(RenderPack rp, bool beforeLayout)
        {
            if (beforeLayout || rp.DataRect.Width < 1 || rp.DataRect.Height < 1)
                return;

            const double triangleWidth = 1;
            double triangleHeight = Math.Sqrt(3) / 2;
            double homeCenterX = triangleWidth / 2;
            double homeCenterY = triangleHeight / 2;

            float shortSide = Math.Min(rp.DataRect.Width, rp.DataRect.Height);
            float margin = Math.Clamp(shortSide * 0.045f, 24f, 44f);
            float innerWidth = Math.Max(1, rp.DataRect.Width - margin * 2);
            float innerHeight = Math.Max(1, rp.DataRect.Height - margin * 2);

            // 0.9：在撑满图框的尺寸上再缩小一成，避免三角形贴边。
            const double fitScale = 0.9;
            double fitUnitsPerPixel = Math.Max(triangleWidth / innerWidth, triangleHeight / innerHeight) / fitScale;
            double currentUnitsPerPixel = Math.Abs(_xAxis.Width / rp.DataRect.Width);
            // 自动缩放留下的视野比目标更松时，收回来并居中。放大或拖动后的视野按原中心保留。
            bool looserThanFit = !(currentUnitsPerPixel > 0) || currentUnitsPerPixel > fitUnitsPerPixel * 1.02;

            double unitsPerPixel = looserThanFit ? fitUnitsPerPixel : currentUnitsPerPixel;
            double centerX = looserThanFit ? homeCenterX : _xAxis.Range.Center;
            double centerY = looserThanFit ? homeCenterY : _yAxis.Range.Center;

            double halfWidth = rp.DataRect.Width / 2d * unitsPerPixel;
            double halfHeight = rp.DataRect.Height / 2d * unitsPerPixel;
            _xAxis.Range.Set(centerX - halfWidth, centerX + halfWidth);
            _yAxis.Range.Set(centerY - halfHeight, centerY + halfHeight);
        }
    }
}
