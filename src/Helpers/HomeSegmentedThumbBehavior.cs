using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 主页推荐内容分组的选中滑块。滑块单独绘制，文字不挂像素特效，
    /// 避免高 DPI / 布局缩放下选中项文字被栅格化发糊。
    /// </summary>
    public class HomeSegmentedThumbBehavior : Behavior<Panel>
    {
        private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(240));

        public static readonly DependencyProperty IndicatorProperty =
            DependencyProperty.Register(
                nameof(Indicator),
                typeof(FrameworkElement),
                typeof(HomeSegmentedThumbBehavior),
                new PropertyMetadata(null, OnInputChanged));

        public static readonly DependencyProperty AlternateTargetProperty =
            DependencyProperty.Register(
                nameof(AlternateTarget),
                typeof(FrameworkElement),
                typeof(HomeSegmentedThumbBehavior),
                new PropertyMetadata(null, OnInputChanged));

        public static readonly DependencyProperty IsAlternateSelectedProperty =
            DependencyProperty.Register(
                nameof(IsAlternateSelected),
                typeof(bool),
                typeof(HomeSegmentedThumbBehavior),
                new PropertyMetadata(false, OnSelectionInputChanged));

        private ListBox? _listBox;
        private bool _queued;
        private bool _animatePending;
        private bool _positioned;
        private bool _sliding;
        private int _slideId;
        private int _retries;

        public FrameworkElement? Indicator
        {
            get => (FrameworkElement?)GetValue(IndicatorProperty);
            set => SetValue(IndicatorProperty, value);
        }

        public FrameworkElement? AlternateTarget
        {
            get => (FrameworkElement?)GetValue(AlternateTargetProperty);
            set => SetValue(AlternateTargetProperty, value);
        }

        public bool IsAlternateSelected
        {
            get => (bool)GetValue(IsAlternateSelectedProperty);
            set => SetValue(IsAlternateSelectedProperty, value);
        }

        private static void OnInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HomeSegmentedThumbBehavior behavior)
                behavior.RequestUpdate(animate: false);
        }

        private static void OnSelectionInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HomeSegmentedThumbBehavior behavior)
                behavior.RequestUpdate(animate: true);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Unloaded += OnUnloaded;
            AssociatedObject.SizeChanged += OnSizeChanged;
            if (AssociatedObject.IsLoaded)
                HookListBox();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Unloaded -= OnUnloaded;
            AssociatedObject.SizeChanged -= OnSizeChanged;
            UnhookListBox();
            base.OnDetaching();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            HookListBox();
            RequestUpdate(animate: false);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnhookListBox();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RequestUpdate(animate: false);
        }

        private void HookListBox()
        {
            UnhookListBox();
            foreach (object child in AssociatedObject.Children)
            {
                if (child is not ListBox listBox)
                    continue;

                _listBox = listBox;
                _listBox.SelectionChanged += OnSelectionChanged;
                _listBox.ItemContainerGenerator.StatusChanged += OnGeneratorStatusChanged;
                break;
            }
        }

        private void UnhookListBox()
        {
            if (_listBox == null)
                return;

            _listBox.SelectionChanged -= OnSelectionChanged;
            _listBox.ItemContainerGenerator.StatusChanged -= OnGeneratorStatusChanged;
            _listBox = null;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RequestUpdate(animate: true);
        }

        private void OnGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (_listBox?.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                RequestUpdate(animate: false);
        }

        private void RequestUpdate(bool animate)
        {
            if (AssociatedObject == null)
                return;

            if (animate)
                _animatePending = true;
            else if (_sliding)
                return;

            if (_queued)
                return;

            _queued = true;
            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ApplyQueued));
        }

        private void ApplyQueued()
        {
            _queued = false;
            if (AssociatedObject == null)
                return;

            bool animate = _animatePending && _positioned;
            _animatePending = false;

            if (TryPlace(animate))
            {
                _retries = 0;
                return;
            }

            if (_retries >= 8)
            {
                _retries = 0;
                return;
            }

            _retries++;
            if (animate)
                _animatePending = true;

            _queued = true;
            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(ApplyQueued));
        }

        private bool TryPlace(bool animate)
        {
            FrameworkElement? indicator = Indicator;
            FrameworkElement? target = ResolveTarget();
            if (indicator == null || target == null || target.ActualWidth <= 0 || target.ActualHeight <= 0)
                return false;

            if (indicator.Parent is not UIElement parent)
                return false;

            Point origin;
            try
            {
                origin = target.TranslatePoint(new Point(0, 0), parent);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            double x = Snap(indicator, origin.X);
            double y = Snap(indicator, origin.Y);
            double width = Snap(indicator, target.ActualWidth);
            double height = Snap(indicator, target.ActualHeight);
            if (width <= 0 || height <= 0)
                return false;

            if (indicator.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                indicator.RenderTransform = transform;
            }

            if (!animate)
            {
                _slideId++;
                _sliding = false;
                transform.BeginAnimation(TranslateTransform.XProperty, null);
                transform.BeginAnimation(TranslateTransform.YProperty, null);
                indicator.BeginAnimation(FrameworkElement.WidthProperty, null);
                indicator.BeginAnimation(FrameworkElement.HeightProperty, null);
                transform.X = x;
                transform.Y = y;
                indicator.Width = width;
                indicator.Height = height;
                indicator.Visibility = Visibility.Visible;
                _positioned = true;
                return true;
            }

            int slideId = ++_slideId;
            _sliding = true;
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
            DoubleAnimation xAnimation = CreateAnimation(x, ease);
            xAnimation.Completed += (_, _) => OnSlideCompleted(slideId);
            transform.BeginAnimation(TranslateTransform.XProperty, xAnimation);
            transform.BeginAnimation(TranslateTransform.YProperty, CreateAnimation(y, ease));
            indicator.BeginAnimation(FrameworkElement.WidthProperty, CreateAnimation(width, ease));
            indicator.BeginAnimation(FrameworkElement.HeightProperty, CreateAnimation(height, ease));
            indicator.Visibility = Visibility.Visible;
            _positioned = true;
            return true;
        }

        private void OnSlideCompleted(int slideId)
        {
            if (slideId != _slideId || !_sliding)
                return;

            _sliding = false;
            RequestUpdate(animate: false);
        }

        private FrameworkElement? ResolveTarget()
        {
            if (IsAlternateSelected)
                return AlternateTarget;

            if (_listBox?.SelectedItem == null)
                return null;

            return _listBox.ItemContainerGenerator.ContainerFromItem(_listBox.SelectedItem) as FrameworkElement;
        }

        private static DoubleAnimation CreateAnimation(double to, IEasingFunction ease)
        {
            return new DoubleAnimation(to, SlideDuration)
            {
                EasingFunction = ease
            };
        }

        private static double Snap(Visual visual, double value)
        {
            double scale = VisualTreeHelper.GetDpi(visual).DpiScaleX;
            if (scale <= 0)
                return value;

            return Math.Round(value * scale) / scale;
        }
    }
}
