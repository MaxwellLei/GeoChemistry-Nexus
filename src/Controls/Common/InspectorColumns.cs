using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 属性面板内的等宽并排布局（对应设计稿的 mfg / rng）：可见子项在一行内等分宽度，子项之间留出 <see cref="Spacing"/>；
    /// 指定 <see cref="Columns"/> 后按该列数自动换行。已折叠的子项不占位。
    /// </summary>
    public class InspectorColumns : Panel
    {
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(InspectorColumns),
                new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(nameof(Columns), typeof(int), typeof(InspectorColumns),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>子项之间的水平 / 垂直间距。</summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        /// <summary>列数；0 表示所有可见子项排成一行。</summary>
        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        private List<UIElement> VisibleChildren()
        {
            var list = new List<UIElement>();
            foreach (UIElement child in InternalChildren)
            {
                if (child != null && child.Visibility != Visibility.Collapsed) list.Add(child);
            }
            return list;
        }

        private int ResolveColumns(int count) => Columns > 0 ? Math.Min(Columns, Math.Max(count, 1)) : Math.Max(count, 1);

        protected override Size MeasureOverride(Size availableSize)
        {
            var children = VisibleChildren();
            if (children.Count == 0) return new Size(0, 0);

            int cols = ResolveColumns(children.Count);
            int rows = (children.Count + cols - 1) / cols;
            double cellWidth = double.IsInfinity(availableSize.Width)
                ? double.PositiveInfinity
                : Math.Max(0, (availableSize.Width - Spacing * (cols - 1)) / cols);

            double totalHeight = 0;
            double maxWidth = 0;
            for (int r = 0; r < rows; r++)
            {
                double rowHeight = 0;
                double rowWidth = 0;
                for (int c = 0; c < cols; c++)
                {
                    int index = r * cols + c;
                    if (index >= children.Count) break;

                    children[index].Measure(new Size(cellWidth, double.PositiveInfinity));
                    rowHeight = Math.Max(rowHeight, children[index].DesiredSize.Height);
                    rowWidth += children[index].DesiredSize.Width + (c > 0 ? Spacing : 0);
                }
                totalHeight += rowHeight + (r > 0 ? Spacing : 0);
                maxWidth = Math.Max(maxWidth, rowWidth);
            }

            double width = double.IsInfinity(availableSize.Width) ? maxWidth : availableSize.Width;
            return new Size(width, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var children = VisibleChildren();
            if (children.Count == 0) return finalSize;

            int cols = ResolveColumns(children.Count);
            int rows = (children.Count + cols - 1) / cols;
            double cellWidth = Math.Max(0, (finalSize.Width - Spacing * (cols - 1)) / cols);

            double y = 0;
            for (int r = 0; r < rows; r++)
            {
                double rowHeight = 0;
                for (int c = 0; c < cols; c++)
                {
                    int index = r * cols + c;
                    if (index >= children.Count) break;
                    rowHeight = Math.Max(rowHeight, children[index].DesiredSize.Height);
                }

                for (int c = 0; c < cols; c++)
                {
                    int index = r * cols + c;
                    if (index >= children.Count) break;
                    children[index].Arrange(new Rect(c * (cellWidth + Spacing), y, cellWidth, rowHeight));
                }
                y += rowHeight + Spacing;
            }

            return finalSize;
        }
    }
}
