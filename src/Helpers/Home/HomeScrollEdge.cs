using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 主页滚过一半时，顶栏「回到顶部」淡入；点击后平滑滚回顶部。
    /// 滚动位置是视图状态，不进 ViewModel。
    /// </summary>
    public static class HomeScrollEdge
    {
        private const double BottomTolerance = 2;
        private const double NearBottomFraction = 0.5;
        private const double ScrollMilliseconds = 450;
        private static readonly Duration ShowDuration = new(TimeSpan.FromMilliseconds(220));
        private static readonly Duration HideDuration = new(TimeSpan.FromMilliseconds(150));
        private static readonly ScrollEventHandler ScrollBarScrollHandler = OnScrollBarScroll;

        private static readonly DependencyPropertyKey IsAtBottomKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsAtBottom",
                typeof(bool),
                typeof(HomeScrollEdge),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsAtBottomProperty = IsAtBottomKey.DependencyProperty;

        public static bool GetIsAtBottom(DependencyObject element) => (bool)element.GetValue(IsAtBottomProperty);

        public static readonly DependencyProperty TrackBottomProperty =
            DependencyProperty.RegisterAttached(
                "TrackBottom",
                typeof(bool),
                typeof(HomeScrollEdge),
                new PropertyMetadata(false, OnTrackBottomChanged));

        public static void SetTrackBottom(DependencyObject element, bool value) => element.SetValue(TrackBottomProperty, value);

        public static bool GetTrackBottom(DependencyObject element) => (bool)element.GetValue(TrackBottomProperty);

        public static readonly DependencyProperty ScrollTargetProperty =
            DependencyProperty.RegisterAttached(
                "ScrollTarget",
                typeof(ScrollViewer),
                typeof(HomeScrollEdge),
                new PropertyMetadata(null, OnScrollTargetChanged));

        public static void SetScrollTarget(DependencyObject element, ScrollViewer? value) => element.SetValue(ScrollTargetProperty, value);

        public static ScrollViewer? GetScrollTarget(DependencyObject element) => (ScrollViewer?)element.GetValue(ScrollTargetProperty);

        private static readonly DependencyProperty SessionProperty =
            DependencyProperty.RegisterAttached(
                "Session",
                typeof(ScrollSession),
                typeof(HomeScrollEdge),
                new PropertyMetadata(null));

        private static readonly DependencyProperty HookProperty =
            DependencyProperty.RegisterAttached(
                "Hook",
                typeof(ButtonHook),
                typeof(HomeScrollEdge),
                new PropertyMetadata(null));

        private static readonly ConditionalWeakTable<ScrollViewer, List<Button>> BoundButtons = new();

        private static void OnTrackBottomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer viewer)
                return;

            viewer.ScrollChanged -= OnScrollChanged;
            viewer.Loaded -= OnViewerLoaded;
            viewer.PreviewMouseWheel -= OnUserInterruptWheel;
            viewer.PreviewKeyDown -= OnUserInterruptKey;
            viewer.RemoveHandler(ScrollBar.ScrollEvent, ScrollBarScrollHandler);
            if ((bool)e.NewValue)
            {
                viewer.ScrollChanged += OnScrollChanged;
                viewer.Loaded += OnViewerLoaded;
                viewer.PreviewMouseWheel += OnUserInterruptWheel;
                viewer.PreviewKeyDown += OnUserInterruptKey;
                viewer.AddHandler(ScrollBar.ScrollEvent, ScrollBarScrollHandler, handledEventsToo: true);
                UpdateIsAtBottom(viewer);
            }
            else
            {
                StopAnimation(viewer);
            }
        }

        private static void OnViewerLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer viewer)
                UpdateIsAtBottom(viewer);
        }

        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 子滚动条的 ScrollChanged 会冒泡上来，只认主页这一层。
            if (sender is not ScrollViewer viewer || !ReferenceEquals(e.OriginalSource, viewer))
                return;

            UpdateIsAtBottom(viewer);
        }

        private static void OnUserInterruptWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer viewer)
                StopAnimation(viewer);
        }

        private static void OnUserInterruptKey(object sender, KeyEventArgs e)
        {
            if (sender is ScrollViewer viewer)
                StopAnimation(viewer);
        }

        private static void OnScrollBarScroll(object sender, ScrollEventArgs e)
        {
            if (sender is not ScrollViewer viewer)
                return;
            if (viewer.GetValue(SessionProperty) is ScrollSession { Driving: true })
                return;

            StopAnimation(viewer);
        }

        private static void UpdateIsAtBottom(ScrollViewer viewer)
        {
            bool atBottom = viewer.ScrollableHeight > BottomTolerance
                && viewer.VerticalOffset >= viewer.ScrollableHeight * (1 - NearBottomFraction);
            if (GetIsAtBottom(viewer) == atBottom)
                return;

            viewer.SetValue(IsAtBottomKey, atBottom);
            if (!BoundButtons.TryGetValue(viewer, out var buttons))
                return;

            foreach (var button in buttons)
                ApplyButton(button, atBottom, animate: true);
        }

        private static void OnScrollTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Button button)
                return;

            DetachButton(button);
            button.Loaded -= OnButtonLoaded;

            if (e.NewValue is not ScrollViewer)
                return;

            button.Loaded += OnButtonLoaded;
            if (button.IsLoaded && GetScrollTarget(button) is ScrollViewer viewer)
                AttachButton(button, viewer);
        }

        private static void OnButtonLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.GetValue(HookProperty) != null)
                return;

            if (GetScrollTarget(button) is ScrollViewer viewer)
                AttachButton(button, viewer);
        }

        private static void AttachButton(Button button, ScrollViewer viewer)
        {
            var hook = new ButtonHook { Viewer = viewer };
            if (!BoundButtons.TryGetValue(viewer, out var buttons))
            {
                buttons = new List<Button>();
                BoundButtons.Add(viewer, buttons);
            }

            if (!buttons.Contains(button))
                buttons.Add(button);

            button.SetValue(HookProperty, hook);
            button.Click += OnButtonClick;
            button.Unloaded += OnButtonUnloaded;
            ApplyButton(button, GetIsAtBottom(viewer), animate: false);
        }

        private static void OnButtonUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                DetachButton(button);
        }

        private static void DetachButton(Button button)
        {
            if (button.GetValue(HookProperty) is not ButtonHook hook)
                return;

            button.Click -= OnButtonClick;
            button.Unloaded -= OnButtonUnloaded;
            if (hook.Viewer != null && BoundButtons.TryGetValue(hook.Viewer, out var buttons))
                buttons.Remove(button);

            button.ClearValue(HookProperty);
        }

        private static void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && GetScrollTarget(button) is ScrollViewer viewer)
                AnimateToTop(viewer);
        }

        private static void ApplyButton(Button button, bool atBottom, bool animate)
        {
            button.IsHitTestVisible = atBottom;
            button.Focusable = atBottom;

            if (button.RenderTransform is not TranslateTransform transform || transform.IsFrozen)
            {
                transform = new TranslateTransform(atBottom ? 0 : 8, 0);
                button.RenderTransform = transform;
            }

            if (!animate)
            {
                button.BeginAnimation(UIElement.OpacityProperty, null);
                transform.BeginAnimation(TranslateTransform.XProperty, null);
                button.Opacity = atBottom ? 1 : 0;
                transform.X = atBottom ? 0 : 8;
                return;
            }

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = atBottom ? ShowDuration : HideDuration;
            button.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(atBottom ? 1 : 0, duration) { EasingFunction = ease });
            transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(atBottom ? 0 : 8, duration) { EasingFunction = ease });
        }

        private static void AnimateToTop(ScrollViewer viewer)
        {
            StopAnimation(viewer);

            double from = viewer.VerticalOffset;
            if (from <= 0.5)
            {
                viewer.ScrollToVerticalOffset(0);
                UpdateIsAtBottom(viewer);
                return;
            }

            var session = new ScrollSession
            {
                From = from,
                StartTimestamp = Stopwatch.GetTimestamp(),
            };
            EventHandler handler = (_, _) => OnRenderFrame(viewer, session);
            session.Handler = handler;
            viewer.SetValue(SessionProperty, session);
            CompositionTarget.Rendering += handler;
        }

        private static void OnRenderFrame(ScrollViewer viewer, ScrollSession session)
        {
            if (session.Cancelled || !ReferenceEquals(viewer.GetValue(SessionProperty), session))
            {
                if (session.Handler != null)
                    CompositionTarget.Rendering -= session.Handler;
                return;
            }

            double elapsedMs = (Stopwatch.GetTimestamp() - session.StartTimestamp) * 1000d / Stopwatch.Frequency;
            double t = Math.Clamp(elapsedMs / ScrollMilliseconds, 0, 1);
            double eased = 1 - Math.Pow(1 - t, 3);
            double offset = session.From * (1 - eased);

            session.Driving = true;
            viewer.ScrollToVerticalOffset(offset);
            session.Driving = false;

            if (t < 1)
                return;

            session.Finished = true;
            StopAnimation(viewer);
            viewer.ScrollToVerticalOffset(0);
            UpdateIsAtBottom(viewer);
            viewer.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (session.Finished && viewer.VerticalOffset > 0.5)
                    viewer.ScrollToVerticalOffset(0);
            });
        }

        private static void StopAnimation(ScrollViewer viewer)
        {
            if (viewer.GetValue(SessionProperty) is not ScrollSession session)
                return;

            session.Cancelled = true;
            if (session.Handler != null)
                CompositionTarget.Rendering -= session.Handler;
            viewer.ClearValue(SessionProperty);
        }

        private sealed class ScrollSession
        {
            public double From;
            public long StartTimestamp;
            public bool Driving;
            public bool Cancelled;
            public bool Finished;
            public EventHandler? Handler;
        }

        private sealed class ButtonHook
        {
            public ScrollViewer? Viewer;
        }
    }
}
