using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Views;

/// <summary>
/// 图层面板的宿主。列被收成 40px 后，仍按子元素自己的 Width 排列，
/// 这样滑出的图层内容不会被列宽裁掉，右侧窄轨也能留在这一列里。
/// </summary>
public class LayerSlideHost : Panel
{
    protected override Geometry? GetLayoutClip(Size layoutSlotSize)
    {
        return ClipToBounds ? base.GetLayoutClip(layoutSlotSize) : null;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        double height = double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : availableSize.Height;

        foreach (UIElement child in InternalChildren)
        {
            double childWidth = double.PositiveInfinity;
            if (child is FrameworkElement element && double.IsNaN(element.Width))
                childWidth = width;

            child.Measure(new Size(childWidth, height));
        }

        return new Size(0, 0);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (UIElement child in InternalChildren)
        {
            double width = finalSize.Width;
            if (child is FrameworkElement element && !double.IsNaN(element.Width) && element.Width > 0)
                width = element.Width;

            child.Arrange(new Rect(0, 0, width, finalSize.Height));
        }

        return finalSize;
    }
}
