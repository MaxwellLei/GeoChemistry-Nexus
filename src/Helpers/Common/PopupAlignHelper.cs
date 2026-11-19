using System.Windows;
using System.Windows.Controls.Primitives;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 让 Popup 的可见内容右缘与 PlacementTarget 的右缘严格对齐（向左加宽）。
    /// 使用 CustomPopupPlacementCallback，回调中的尺寸均为屏幕设备像素，
    /// 因此不受 UI 缩放（LayoutTransform）/ DPI 影响。
    /// 用法：Popup 上设置 helpers:PopupAlignHelper.AlignRightEdge="True"，
    /// 并用 helpers:PopupAlignHelper.RightInset 指定弹窗内容外层留白（如阴影空间，单位 DIP）。
    /// </summary>
    public static class PopupAlignHelper
    {
        public static readonly DependencyProperty AlignRightEdgeProperty =
            DependencyProperty.RegisterAttached(
                "AlignRightEdge", typeof(bool), typeof(PopupAlignHelper),
                new PropertyMetadata(false, OnAlignRightEdgeChanged));

        public static bool GetAlignRightEdge(DependencyObject obj) => (bool)obj.GetValue(AlignRightEdgeProperty);
        public static void SetAlignRightEdge(DependencyObject obj, bool value) => obj.SetValue(AlignRightEdgeProperty, value);

        /// <summary>弹窗内容右侧外层留白（DIP），对齐时会被抵消。</summary>
        public static readonly DependencyProperty RightInsetProperty =
            DependencyProperty.RegisterAttached(
                "RightInset", typeof(double), typeof(PopupAlignHelper), new PropertyMetadata(0d));

        public static double GetRightInset(DependencyObject obj) => (double)obj.GetValue(RightInsetProperty);
        public static void SetRightInset(DependencyObject obj, double value) => obj.SetValue(RightInsetProperty, value);

        /// <summary>弹窗顶部与目标底边的垂直偏移（DIP）。</summary>
        public static readonly DependencyProperty VerticalGapProperty =
            DependencyProperty.RegisterAttached(
                "VerticalGap", typeof(double), typeof(PopupAlignHelper), new PropertyMetadata(0d));

        public static double GetVerticalGap(DependencyObject obj) => (double)obj.GetValue(VerticalGapProperty);
        public static void SetVerticalGap(DependencyObject obj, double value) => obj.SetValue(VerticalGapProperty, value);

        private static void OnAlignRightEdgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Popup popup) return;

            if ((bool)e.NewValue)
            {
                popup.Placement = PlacementMode.Custom;
                popup.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
                {
                    // 设备像素 / DIP 的换算比例：用弹窗内容的 DIP 宽度反推
                    double scale = 1d;
                    if (popup.Child is FrameworkElement child)
                    {
                        double dipWidth = child.ActualWidth + child.Margin.Left + child.Margin.Right;
                        if (dipWidth > 0)
                            scale = popupSize.Width / dipWidth;
                    }

                    double inset = GetRightInset(popup) * scale;
                    double gap = GetVerticalGap(popup) * scale;

                    // 可见内容右缘 = 弹窗右缘 - inset，需等于目标右缘
                    double x = targetSize.Width - popupSize.Width + inset;
                    double y = targetSize.Height + gap;
                    return new[]
                    {
                        new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.Horizontal)
                    };
                };
            }
            else
            {
                popup.CustomPopupPlacementCallback = null;
            }
        }
    }
}
