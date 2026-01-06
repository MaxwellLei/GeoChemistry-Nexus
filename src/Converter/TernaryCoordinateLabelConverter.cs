using System;
using System.Globalization;
using System.Windows.Data;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 转换器：根据当前是否为三元图模式，返回对应的坐标轴标签
    /// 三元图模式下显示 A/B，笛卡尔坐标系下显示 X/Y
    /// </summary>
    public class TernaryCoordinateLabelConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法：参数为坐标轴标识 ("X" 或 "Y")
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string axisType = parameter as string ?? "X";
            bool isTernary = MainPlotViewModel.BaseMapType == "Ternary";

            if (isTernary)
            {
                // 三元图模式: X -> A, Y -> B
                return axisType.ToUpper() == "X" ? "A" : "B";
            }
            else
            {
                // 笛卡尔坐标系模式: X -> X, Y -> Y
                return axisType.ToUpper();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 辅助静态类：提供三元图坐标相关的实用方法
    /// </summary>
    public static class TernaryCoordinateHelper
    {
        /// <summary>
        /// 检查当前是否为三元图模式
        /// </summary>
        public static bool IsTernaryMode => MainPlotViewModel.BaseMapType == "Ternary";

        /// <summary>
        /// 获取X轴标签（三元图显示A，否则显示X）
        /// </summary>
        public static string XAxisLabel => IsTernaryMode ? "A" : "X";

        /// <summary>
        /// 获取Y轴标签（三元图显示B，否则显示Y）
        /// </summary>
        public static string YAxisLabel => IsTernaryMode ? "B" : "Y";

        /// <summary>
        /// 将显示坐标（三元坐标或笛卡尔坐标）转换为内部存储的笛卡尔坐标
        /// </summary>
        /// <param name="displayX">显示的X/A坐标值</param>
        /// <param name="displayY">显示的Y/B坐标值</param>
        /// <returns>内部存储的笛卡尔坐标 (X, Y)</returns>
        public static (double X, double Y) DisplayToCartesian(double displayX, double displayY)
        {
            if (IsTernaryMode)
            {
                // 三元坐标转笛卡尔坐标
                // displayX = A (bottom), displayY = B (left), C = 1 - A - B (right)
                double c = 1 - displayX - displayY;
                return MainPlotViewModel.ToCartesian(displayX, displayY, c);
            }
            else
            {
                // 笛卡尔坐标直接返回
                return (displayX, displayY);
            }
        }

        /// <summary>
        /// 将内部存储的笛卡尔坐标转换为显示坐标（三元坐标或笛卡尔坐标）
        /// </summary>
        /// <param name="cartesianX">内部存储的笛卡尔X坐标</param>
        /// <param name="cartesianY">内部存储的笛卡尔Y坐标</param>
        /// <returns>显示的坐标 (X/A, Y/B)</returns>
        public static (double DisplayX, double DisplayY) CartesianToDisplay(double cartesianX, double cartesianY)
        {
            if (IsTernaryMode)
            {
                // 笛卡尔坐标转三元坐标
                var ternary = MainPlotViewModel.ToTernary(cartesianX, cartesianY, MainPlotViewModel.Clockwise);
                return (ternary.Item1, ternary.Item2);
            }
            else
            {
                // 笛卡尔坐标直接返回
                return (cartesianX, cartesianY);
            }
        }
    }
}
