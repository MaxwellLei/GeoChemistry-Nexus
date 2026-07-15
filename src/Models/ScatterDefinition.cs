using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers.PlotMarkers;

namespace GeoChemistryNexus.Models
{
    public partial class ScatterDefinition : ObservableObject
    {
        /// <summary>
        /// 数据系列名称
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        // 坐标位置
        [ObservableProperty]
        private PointDefinition _startAndEnd = new PointDefinition();

        /// <summary>
        /// 散点大小
        /// </summary>
        [ObservableProperty]
        private float _size = 12;

        /// <summary>
        /// 填充颜色（仅实心形状使用）
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// 散点类型（含内置扩展形状）
        /// </summary>
        [ObservableProperty]
        private PlotMarkerShape _markerShape = PlotMarkerShape.FilledSquare;

        /// <summary>
        /// 描边宽度
        /// </summary>
        [ObservableProperty]
        private float _strokeWidth = 0;

        /// <summary>
        /// 描边颜色（空心/线型形状的线条颜色也用此属性）
        /// </summary>
        [ObservableProperty]
        private string _strokeColor = "#000000";

        /// <summary>
        /// 当前形状是否具有填充（用于属性面板显示填充色）
        /// </summary>
        public bool HasFill => PlotMarkerStyleApplier.IsFilled(MarkerShape);

        partial void OnMarkerShapeChanged(PlotMarkerShape oldValue, PlotMarkerShape newValue)
        {
            OnPropertyChanged(nameof(HasFill));

            bool wasFilled = PlotMarkerStyleApplier.IsFilled(oldValue);
            bool isFilled = PlotMarkerStyleApplier.IsFilled(newValue);

            if (wasFilled && !isFilled)
            {
                // 实心 → 空心/线型：用原填充色作为线条色，并给一个可见线宽
                StrokeColor = Color;
                if (StrokeWidth <= 0)
                {
                    StrokeWidth = 1.5f;
                }
            }
            else if (!wasFilled && isFilled)
            {
                // 空心/线型 → 实心：用原线条色作为填充色
                Color = StrokeColor;
            }
        }
    }
}
