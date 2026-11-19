using System;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 属性面板内的纵向排布面板：子项自上而下排列，相邻可见子项之间固定留出 <see cref="Spacing"/> 的间距
    /// （等价于 CSS 的 flex 纵向 gap；已折叠的子项不占位也不产生间距）。
    /// </summary>
    public class InspectorStack : Panel
    {
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(InspectorStack),
                new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>相邻子项之间的间距。</summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;
            bool first = true;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null || child.Visibility == Visibility.Collapsed) continue;

                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                if (!first) height += Spacing;
                height += child.DesiredSize.Height;
                width = Math.Max(width, child.DesiredSize.Width);
                first = false;
            }

            return new Size(double.IsInfinity(availableSize.Width) ? width : Math.Min(width, availableSize.Width), height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double y = 0;
            bool first = true;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null || child.Visibility == Visibility.Collapsed) continue;

                if (!first) y += Spacing;
                child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height;
                first = false;
            }

            return finalSize;
        }
    }
}
