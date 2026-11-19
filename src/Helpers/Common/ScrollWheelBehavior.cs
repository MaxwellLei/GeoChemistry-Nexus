using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// WPF has no Shift + wheel horizontal scrolling and a nested ScrollViewer swallows the wheel even when it cannot
    /// scroll vertically. These attached properties add the first and fix the second.
    /// </summary>
    public static class ScrollWheelBehavior
    {
        /// <summary>Set on an outer element: Shift + wheel anywhere inside scrolls this ScrollViewer horizontally.</summary>
        public static readonly DependencyProperty ShiftHorizontalTargetProperty = DependencyProperty.RegisterAttached(
            "ShiftHorizontalTarget", typeof(ScrollViewer), typeof(ScrollWheelBehavior),
            new PropertyMetadata(null, OnShiftHorizontalTargetChanged));

        /// <summary>Set on a horizontal-only ScrollViewer: the plain wheel is handed to the enclosing ScrollViewer.</summary>
        public static readonly DependencyProperty PassVerticalWheelToParentProperty = DependencyProperty.RegisterAttached(
            "PassVerticalWheelToParent", typeof(bool), typeof(ScrollWheelBehavior),
            new PropertyMetadata(false, OnPassVerticalWheelToParentChanged));

        public static ScrollViewer? GetShiftHorizontalTarget(DependencyObject element) =>
            (ScrollViewer?)element.GetValue(ShiftHorizontalTargetProperty);
        public static void SetShiftHorizontalTarget(DependencyObject element, ScrollViewer? value) =>
            element.SetValue(ShiftHorizontalTargetProperty, value);
        public static bool GetPassVerticalWheelToParent(DependencyObject element) =>
            (bool)element.GetValue(PassVerticalWheelToParentProperty);
        public static void SetPassVerticalWheelToParent(DependencyObject element, bool value) =>
            element.SetValue(PassVerticalWheelToParentProperty, value);

        private static void OnShiftHorizontalTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element) return;
            element.PreviewMouseWheel -= OnShiftWheel;
            if (e.NewValue is not null) element.PreviewMouseWheel += OnShiftWheel;
        }

        private static void OnPassVerticalWheelToParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element) return;
            element.PreviewMouseWheel -= OnPlainWheel;
            if (e.NewValue is true) element.PreviewMouseWheel += OnPlainWheel;
        }

        /// <summary>Set on any element: it never consumes the wheel; every wheel event inside is handed to its parent.</summary>
        public static readonly DependencyProperty IgnoreWheelProperty = DependencyProperty.RegisterAttached(
            "IgnoreWheel", typeof(bool), typeof(ScrollWheelBehavior),
            new PropertyMetadata(false, OnIgnoreWheelChanged));

        public static bool GetIgnoreWheel(DependencyObject element) => (bool)element.GetValue(IgnoreWheelProperty);
        public static void SetIgnoreWheel(DependencyObject element, bool value) => element.SetValue(IgnoreWheelProperty, value);

        private static void OnIgnoreWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element) return;
            element.PreviewMouseWheel -= OnIgnoreWheel;
            if (e.NewValue is true) element.PreviewMouseWheel += OnIgnoreWheel;
        }

        /// <summary>Set on a ScrollViewer or a control that hosts one, such as a ListBox. When its content does not overflow, the wheel is handed to the enclosing element.</summary>
        public static readonly DependencyProperty PassWheelWhenNotScrollableProperty = DependencyProperty.RegisterAttached(
            "PassWheelWhenNotScrollable", typeof(bool), typeof(ScrollWheelBehavior),
            new PropertyMetadata(false, OnPassWheelWhenNotScrollableChanged));

        public static bool GetPassWheelWhenNotScrollable(DependencyObject element) =>
            (bool)element.GetValue(PassWheelWhenNotScrollableProperty);
        public static void SetPassWheelWhenNotScrollable(DependencyObject element, bool value) =>
            element.SetValue(PassWheelWhenNotScrollableProperty, value);

        private static void OnPassWheelWhenNotScrollableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element) return;
            element.PreviewMouseWheel -= OnNotScrollableWheel;
            if (e.NewValue is true) element.PreviewMouseWheel += OnNotScrollableWheel;
        }

        private static void OnNotScrollableWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || sender is not UIElement element) return;
            var viewer = element as ScrollViewer ?? FindScrollViewer(element);
            if (viewer is { ScrollableHeight: > 0.5 }) return;
            if (VisualTreeHelper.GetParent(element) is not UIElement parent) return;
            e.Handled = true;
            parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            { RoutedEvent = UIElement.MouseWheelEvent, Source = element });
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject root)
        {
            for (int i = 0, count = VisualTreeHelper.GetChildrenCount(root); i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is ScrollViewer viewer)
                    return viewer;
                var nested = FindScrollViewer(child);
                if (nested is not null)
                    return nested;
            }

            return null;
        }

        private static void OnIgnoreWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || sender is not UIElement element) return;
            if (VisualTreeHelper.GetParent(element) is not UIElement parent) return;
            e.Handled = true;
            parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            { RoutedEvent = UIElement.MouseWheelEvent, Source = element });
        }

        private static void OnShiftWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || (Keyboard.Modifiers & ModifierKeys.Shift) == 0) return;
            if (sender is not DependencyObject owner || GetShiftHorizontalTarget(owner) is not { } target) return;
            target.ScrollToHorizontalOffset(target.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private static void OnPlainWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || (Keyboard.Modifiers & ModifierKeys.Shift) != 0 || sender is not ScrollViewer viewer) return;
            // A multi-line box that can scroll its own text keeps the wheel.
            for (var node = e.OriginalSource as DependencyObject; node is not null && !ReferenceEquals(node, viewer); node = VisualTreeHelper.GetParent(node))
                if (node is TextBox { ViewportHeight: > 0 } box && box.ExtentHeight > box.ViewportHeight + 0.5) return;
            if (VisualTreeHelper.GetParent(viewer) is not UIElement parent) return;
            e.Handled = true;
            parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            { RoutedEvent = UIElement.MouseWheelEvent, Source = viewer });
        }
    }
}
