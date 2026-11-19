using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 用于拖动调整尺寸的细条（表格列宽 / 行高）。
    /// <see cref="Value"/> 通常双向绑定到视图模型中的宽度或高度，拖动时按鼠标位移更新，并受 <see cref="MinValue"/> 限制。
    /// 外观由 Themes/Styles.xaml 中的 LocResizeGripStyle 提供。
    /// </summary>
    public class ResizeGripThumb : Thumb
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(double), typeof(ResizeGripThumb),
            new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(MinValue), typeof(double), typeof(ResizeGripThumb), new PropertyMetadata(60d));

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(ResizeGripThumb), new PropertyMetadata(Orientation.Horizontal));

        /// <summary>被调整的宽度（Horizontal）或高度（Vertical）。</summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>允许调整到的最小值。</summary>
        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        /// <summary>Horizontal：左右拖动调整宽度；Vertical：上下拖动调整高度。</summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private void Apply(double delta)
        {
            double current = double.IsNaN(Value) ? (Orientation == Orientation.Horizontal ? ActualWidth : ActualHeight) : Value;
            Value = Math.Max(MinValue, current + delta);
        }

        public ResizeGripThumb()
        {
            DragDelta += (_, e) => Apply(Orientation == Orientation.Horizontal ? e.HorizontalChange : e.VerticalChange);
        }
    }
}
