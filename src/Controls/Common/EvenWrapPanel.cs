using System;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls;

/// <summary>
/// 按可用宽度决定每行列数，并让同一行的卡片等宽铺满。
/// </summary>
public class EvenWrapPanel : Panel
{
    public static readonly DependencyProperty PreferredItemWidthProperty =
        DependencyProperty.Register(nameof(PreferredItemWidth), typeof(double), typeof(EvenWrapPanel),
            new FrameworkPropertyMetadata(280d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MinItemWidthProperty =
        DependencyProperty.Register(nameof(MinItemWidth), typeof(double), typeof(EvenWrapPanel),
            new FrameworkPropertyMetadata(210d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MaxItemWidthProperty =
        DependencyProperty.Register(nameof(MaxItemWidth), typeof(double), typeof(EvenWrapPanel),
            new FrameworkPropertyMetadata(420d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ItemGapProperty =
        DependencyProperty.Register(nameof(ItemGap), typeof(double), typeof(EvenWrapPanel),
            new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double PreferredItemWidth
    {
        get => (double)GetValue(PreferredItemWidthProperty);
        set => SetValue(PreferredItemWidthProperty, value);
    }

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double MaxItemWidth
    {
        get => (double)GetValue(MaxItemWidthProperty);
        set => SetValue(MaxItemWidthProperty, value);
    }

    public double ItemGap
    {
        get => (double)GetValue(ItemGapProperty);
        set => SetValue(ItemGapProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = NormalizeWidth(availableSize.Width);
        int columns = CountColumns(width);
        double itemWidth = ItemWidth(width, columns);
        double[] heights = MeasureRows(itemWidth, columns);
        return new Size(double.IsInfinity(availableSize.Width) ? width : availableSize.Width, StackHeight(heights));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double width = NormalizeWidth(finalSize.Width);
        int columns = CountColumns(width);
        double itemWidth = ItemWidth(width, columns);
        double[] heights = MeasureRows(itemWidth, columns);
        double y = 0;
        for (int i = 0; i < InternalChildren.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            if (column == 0 && row > 0)
                y += heights[row - 1] + ItemGap;
            InternalChildren[i].Arrange(new Rect(column * (itemWidth + ItemGap), y, itemWidth, heights[row]));
        }

        return finalSize;
    }

    private double[] MeasureRows(double itemWidth, int columns)
    {
        int count = InternalChildren.Count;
        int rows = count == 0 ? 0 : (count + columns - 1) / columns;
        var heights = new double[rows];
        for (int i = 0; i < count; i++)
        {
            InternalChildren[i].Measure(new Size(itemWidth, double.PositiveInfinity));
            int row = i / columns;
            heights[row] = Math.Max(heights[row], InternalChildren[i].DesiredSize.Height);
        }

        return heights;
    }

    private int CountColumns(double width)
    {
        double preferred = Math.Max(1, PreferredItemWidth);
        double min = Math.Max(1, MinItemWidth);
        double max = Math.Max(min, MaxItemWidth);
        double gap = Math.Max(0, ItemGap);
        int columns = Math.Max(1, (int)Math.Floor((width + gap) / (preferred + gap)));
        while (columns > 1 && ItemWidth(width, columns) < min)
            columns--;
        while (ItemWidth(width, columns) > max && ItemWidth(width, columns + 1) >= min)
            columns++;
        return columns;
    }

    private double ItemWidth(double width, int columns)
    {
        double gap = Math.Max(0, ItemGap) * Math.Max(0, columns - 1);
        double item = (width - gap) / Math.Max(1, columns);
        return item < 1 ? 1 : item;
    }

    private double NormalizeWidth(double width) =>
        double.IsNaN(width) || double.IsInfinity(width) || width <= 0 ? PreferredItemWidth : width;

    private double StackHeight(double[] heights)
    {
        double height = 0;
        foreach (double row in heights)
            height += row;
        if (heights.Length > 1)
            height += (heights.Length - 1) * Math.Max(0, ItemGap);
        return height;
    }
}
