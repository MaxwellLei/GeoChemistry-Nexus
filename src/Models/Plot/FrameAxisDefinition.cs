using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 二维图顶端 / 右侧的图框边（无刻度、无标签的“未图层化”坐标轴），仅承载轴线样式
    /// </summary>
    public partial class FrameAxisDefinition : ObservableObject
    {
        /// <summary>
        /// 坐标轴类型（"Top" / "Right"）
        /// </summary>
        [ObservableProperty]
        private string _type = string.Empty;

        /// <summary>
        /// 轴线宽度
        /// </summary>
        [ObservableProperty]
        private float _axisLineWidth = 1;

        /// <summary>
        /// 轴线颜色
        /// </summary>
        [ObservableProperty]
        private string _axisLineColor = "#000000";

        partial void OnAxisLineWidthChanged(float value)
        {
            if (value < 0)
                AxisLineWidth = 0;
        }
    }
}
