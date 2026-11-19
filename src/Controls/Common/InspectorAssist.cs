using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 属性面板（Inspector）输入控件的附加属性：
    /// <list type="bullet">
    /// <item><description><see cref="UnitProperty"/>：数值框右侧的单位文字（如 pt、°），悬停 / 聚焦时让位给增减按钮。</description></item>
    /// <item><description><see cref="DragAdjustProperty"/>：在数值框的前缀标题上按住左右拖动即可调整数值（类似设计软件）。</description></item>
    /// </list>
    /// </summary>
    public static class InspectorAssist
    {
        #region Unit

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.RegisterAttached("Unit", typeof(string), typeof(InspectorAssist),
                new PropertyMetadata(string.Empty));

        public static string GetUnit(DependencyObject obj) => (string)obj.GetValue(UnitProperty);

        public static void SetUnit(DependencyObject obj, string value) => obj.SetValue(UnitProperty, value);

        #endregion

        #region DragAdjust

        public static readonly DependencyProperty DragAdjustProperty =
            DependencyProperty.RegisterAttached("DragAdjust", typeof(bool), typeof(InspectorAssist),
                new PropertyMetadata(false, OnDragAdjustChanged));

        public static bool GetDragAdjust(DependencyObject obj) => (bool)obj.GetValue(DragAdjustProperty);

        public static void SetDragAdjust(DependencyObject obj, bool value) => obj.SetValue(DragAdjustProperty, value);

        /// <summary>每拖动多少像素调整一个步长。</summary>
        private const double PixelsPerStep = 4;

        private static void OnDragAdjustChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not NumericUpDown numeric) return;

            numeric.Loaded -= Numeric_Loaded;
            if ((bool)e.NewValue)
            {
                numeric.Loaded += Numeric_Loaded;
                if (numeric.IsLoaded) AttachDrag(numeric);
            }
        }

        private static void Numeric_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is NumericUpDown numeric) AttachDrag(numeric);
        }

        private static void AttachDrag(NumericUpDown numeric)
        {
            numeric.ApplyTemplate();
            if (numeric.Template?.FindName("TitleText", numeric) is not UIElement handle) return;

            handle.MouseLeftButtonDown -= Handle_MouseLeftButtonDown;
            handle.MouseMove -= Handle_MouseMove;
            handle.MouseLeftButtonUp -= Handle_MouseLeftButtonUp;
            handle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
            handle.MouseMove += Handle_MouseMove;
            handle.MouseLeftButtonUp += Handle_MouseLeftButtonUp;
        }

        private sealed class DragState
        {
            public double StartX;
            public double StartValue;
        }

        private static readonly DependencyProperty DragStateProperty =
            DependencyProperty.RegisterAttached("DragState", typeof(DragState), typeof(InspectorAssist));

        private static NumericUpDown? OwnerOf(object sender) =>
            (sender as FrameworkElement)?.TemplatedParent as NumericUpDown;

        private static void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var numeric = OwnerOf(sender);
            if (numeric == null || numeric.IsReadOnly || !numeric.IsEnabled) return;

            var handle = (UIElement)sender;
            handle.SetValue(DragStateProperty, new DragState
            {
                StartX = e.GetPosition(null).X,
                StartValue = numeric.Value
            });
            handle.CaptureMouse();
            e.Handled = true;
        }

        private static void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            var handle = (UIElement)sender;
            if (!handle.IsMouseCaptured || handle.GetValue(DragStateProperty) is not DragState state) return;

            var numeric = OwnerOf(sender);
            if (numeric == null) return;

            double step = numeric.Increment > 0 ? numeric.Increment : 1;
            double steps = Math.Round((e.GetPosition(null).X - state.StartX) / PixelsPerStep);
            double value = state.StartValue + steps * step;
            value = Math.Max(numeric.Minimum, Math.Min(numeric.Maximum, value));
            numeric.Value = Math.Round(value, 4);
        }

        private static void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var handle = (UIElement)sender;
            if (!handle.IsMouseCaptured) return;

            handle.ReleaseMouseCapture();
            handle.ClearValue(DragStateProperty);
            e.Handled = true;
        }

        #endregion
    }
}
