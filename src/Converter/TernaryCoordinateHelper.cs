using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Converter
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
    }
}
