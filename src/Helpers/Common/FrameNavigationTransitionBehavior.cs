using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// Frame 导航轻量进场过渡行为：
    /// 无退场等待（连续点击零拖沓），新页面呈现时执行短促渐显 + 微上浮缓出动画，
    /// 动画结束后立即移除变换与动画时钟，避免影响 ScottPlot / ReoGrid 与高分屏文字清晰度。
    /// </summary>
    public class FrameNavigationTransitionBehavior : Behavior<Frame>
    {
        private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(150));
        private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(210));
        private static readonly IEasingFunction FadeEase = CreateFrozenEase(new QuadraticEase { EasingMode = EasingMode.EaseOut });
        private static readonly IEasingFunction SlideEase = CreateFrozenEase(new CubicEase { EasingMode = EasingMode.EaseOut });

        public static readonly DependencyProperty OffsetYProperty =
            DependencyProperty.Register(
                nameof(OffsetY),
                typeof(double),
                typeof(FrameNavigationTransitionBehavior),
                new PropertyMetadata(8.0));

        /// <summary>
        /// 为 true 时（主导航 Frame）：页面顶部 Row 0（PageTitleBar）保持原位仅随整体渐显，
        /// 仅对 Row 1+ 的工作区主体施加微上浮位移，避免顶栏底边分割线在切换瞬间上下错位。
        /// </summary>
        public static readonly DependencyProperty SplitPageHeaderProperty =
            DependencyProperty.Register(
                nameof(SplitPageHeader),
                typeof(bool),
                typeof(FrameNavigationTransitionBehavior),
                new PropertyMetadata(false));

        private readonly List<(UIElement Target, TranslateTransform Transform)> _activeTransforms = new();
        private static bool? _isTransitionEnabled;
        private object? _lastContent;
        private bool _hasPresentedInitialContent;
        private int _animationVersion;
        private bool _detached;

        /// <summary>
        /// 全局开关：是否启用页面切换过渡动画（默认关闭）。
        /// </summary>
        public static bool IsTransitionEnabled
        {
            get => _isTransitionEnabled ??=
                bool.TryParse(ConfigHelper.GetConfig(ConfigKeys.PageTransitionAnimation), out bool enabled) && enabled;
            set => _isTransitionEnabled = value;
        }

        public double OffsetY
        {
            get => (double)GetValue(OffsetYProperty);
            set => SetValue(OffsetYProperty, value);
        }

        public bool SplitPageHeader
        {
            get => (bool)GetValue(SplitPageHeaderProperty);
            set => SetValue(SplitPageHeaderProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            _detached = false;
            _lastContent = AssociatedObject.Content;
            _hasPresentedInitialContent = _lastContent != null;

            AssociatedObject.Navigated += OnNavigated;
            AssociatedObject.Unloaded += OnUnloaded;
        }

        protected override void OnDetaching()
        {
            _detached = true;
            _animationVersion++;
            AssociatedObject.Navigated -= OnNavigated;
            AssociatedObject.Unloaded -= OnUnloaded;
            ResetActiveAnimation();
            base.OnDetaching();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _animationVersion++;
            ResetActiveAnimation();
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            if (_detached || AssociatedObject == null)
                return;

            bool isInitial = !_hasPresentedInitialContent;
            bool isSameContent = ReferenceEquals(_lastContent, e.Content);

            _hasPresentedInitialContent = true;
            _lastContent = e.Content;

            _animationVersion++;
            ResetActiveAnimation();

            // 未开启开关、首次冷启动加载、子导航首次填充或重复导航至同一实例时不播放进场动画
            if (!IsTransitionEnabled || isInitial || isSameContent || !AssociatedObject.IsLoaded || e.Content == null)
                return;

            int version = _animationVersion;
            double offsetY = OffsetY;

            // 在首次渲染前同步置为起始态，避免首帧闪烁；待新页面布局测量完成后再启动动画时钟，保证重页面首次进入也不掉帧
            AssociatedObject.Opacity = 0;

            var targets = new List<UIElement>(4);
            if (SplitPageHeader)
            {
                CollectBodySlideTargets(e.Content, targets);
            }
            else if (CanSafelyAnimateTransform(AssociatedObject))
            {
                targets.Add(AssociatedObject);
            }

            if (Math.Abs(offsetY) > 0.01)
            {
                foreach (UIElement target in targets)
                {
                    var translate = new TranslateTransform(0, offsetY);
                    target.RenderTransform = translate;
                    _activeTransforms.Add((target, translate));
                }
            }

            AssociatedObject.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() => StartEntranceAnimations(version, offsetY)));
        }

        private void StartEntranceAnimations(int version, double offsetY)
        {
            if (_detached || version != _animationVersion || AssociatedObject == null)
                return;

            if (!AssociatedObject.IsLoaded)
            {
                ResetActiveAnimation();
                return;
            }

            var fadeAnimation = new DoubleAnimation(0, 1, FadeDuration)
            {
                EasingFunction = FadeEase
            };

            if (_activeTransforms.Count == 0)
            {
                fadeAnimation.Completed += (_, _) => ScheduleFinish(version);
                AssociatedObject.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
                return;
            }

            AssociatedObject.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);

            for (int i = 0; i < _activeTransforms.Count; i++)
            {
                var (_, translate) = _activeTransforms[i];
                var slideAnimation = new DoubleAnimation(offsetY, 0, SlideDuration)
                {
                    EasingFunction = SlideEase
                };

                if (i == _activeTransforms.Count - 1)
                {
                    slideAnimation.Completed += (_, _) => ScheduleFinish(version);
                }

                translate.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
            }
        }

        private void ScheduleFinish(int version)
        {
            if (_detached || version != _animationVersion || AssociatedObject == null)
                return;

            // 等当前帧动画时钟收尾后再清除动画与变换，还原纯净渲染树
            AssociatedObject.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    if (!_detached && version == _animationVersion)
                    {
                        ResetActiveAnimation();
                    }
                }));
        }

        private void ResetActiveAnimation()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.BeginAnimation(UIElement.OpacityProperty, null);
                AssociatedObject.Opacity = 1.0;
            }

            if (_activeTransforms.Count == 0)
                return;

            foreach (var (target, translate) in _activeTransforms)
            {
                translate.BeginAnimation(TranslateTransform.YProperty, null);
                if (ReferenceEquals(target.RenderTransform, translate))
                {
                    target.ClearValue(UIElement.RenderTransformProperty);
                }
            }

            _activeTransforms.Clear();
        }

        private static void CollectBodySlideTargets(object? content, List<UIElement> targets)
        {
            UIElement? root = content switch
            {
                Page page => page.Content as UIElement,
                UIElement element => element,
                _ => null
            };

            if (root is not Grid grid)
            {
                if (root != null && CanSafelyAnimateTransform(root))
                {
                    targets.Add(root);
                }
                return;
            }

            // 标准模块页：Row 0 为 PageTitleBar，Row 1+ 为页面工作区主体
            if (grid.RowDefinitions.Count >= 2)
            {
                AddGridBodyRows(grid, targets);
                return;
            }

            // 多视图容器页（如 EpmaPageView）：子 UserControl 内部各自包含 Row 0 顶栏与 Row 1 主体
            foreach (UIElement child in grid.Children)
            {
                if (child is UserControl userControl &&
                    userControl.Content is Grid innerGrid &&
                    innerGrid.RowDefinitions.Count >= 2)
                {
                    AddGridBodyRows(innerGrid, targets);
                }
            }

            if (targets.Count == 0 && CanSafelyAnimateTransform(grid))
            {
                targets.Add(grid);
            }
        }

        private static void AddGridBodyRows(Grid grid, List<UIElement> targets)
        {
            foreach (UIElement child in grid.Children)
            {
                if (Grid.GetRow(child) >= 1 && CanSafelyAnimateTransform(child))
                {
                    targets.Add(child);
                }
            }
        }

        private static bool CanSafelyAnimateTransform(UIElement element)
        {
            ValueSource source = DependencyPropertyHelper.GetValueSource(element, UIElement.RenderTransformProperty);
            return source.BaseValueSource == BaseValueSource.Default;
        }

        private static IEasingFunction CreateFrozenEase(Freezable ease)
        {
            ease.Freeze();
            return (IEasingFunction)ease;
        }
    }
}
