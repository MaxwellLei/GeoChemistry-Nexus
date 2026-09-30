using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 公告切换时的横向滚动。方向为正时旧内容向左滑出、新内容从右侧进入；方向为负时相反。
    /// </summary>
    public class HomeAnnouncementSlideBehavior : Behavior<FrameworkElement>
    {
        private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(450));

        private int _playId;
        private int _retries;
        private bool _detached;

        public static readonly DependencyProperty SlideIdProperty =
            DependencyProperty.Register(
                nameof(SlideId),
                typeof(int),
                typeof(HomeAnnouncementSlideBehavior),
                new PropertyMetadata(0, OnSlideIdChanged));

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(
                nameof(Direction),
                typeof(int),
                typeof(HomeAnnouncementSlideBehavior),
                new PropertyMetadata(0));

        public static readonly DependencyProperty IncomingProperty =
            DependencyProperty.Register(
                nameof(Incoming),
                typeof(UIElement),
                typeof(HomeAnnouncementSlideBehavior),
                new PropertyMetadata(null));

        public static readonly DependencyProperty OutgoingProperty =
            DependencyProperty.Register(
                nameof(Outgoing),
                typeof(UIElement),
                typeof(HomeAnnouncementSlideBehavior),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CompletedCommandProperty =
            DependencyProperty.Register(
                nameof(CompletedCommand),
                typeof(ICommand),
                typeof(HomeAnnouncementSlideBehavior),
                new PropertyMetadata(null));

        public int SlideId
        {
            get => (int)GetValue(SlideIdProperty);
            set => SetValue(SlideIdProperty, value);
        }

        public int Direction
        {
            get => (int)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        public UIElement? Incoming
        {
            get => (UIElement?)GetValue(IncomingProperty);
            set => SetValue(IncomingProperty, value);
        }

        public UIElement? Outgoing
        {
            get => (UIElement?)GetValue(OutgoingProperty);
            set => SetValue(OutgoingProperty, value);
        }

        public ICommand? CompletedCommand
        {
            get => (ICommand?)GetValue(CompletedCommandProperty);
            set => SetValue(CompletedCommandProperty, value);
        }

        protected override void OnDetaching()
        {
            _detached = true;
            _playId++;
            ParkAtRest();
            base.OnDetaching();
        }

        private static void OnSlideIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((HomeAnnouncementSlideBehavior)d).Play((int)e.NewValue);
        }

        private void Play(int slideId)
        {
            if (_detached || slideId != SlideId)
                return;

            if (AssociatedObject == null || Incoming == null || Outgoing == null)
            {
                if (_retries >= 2)
                    return;

                _retries++;
                Dispatcher.BeginInvoke(() => Play(slideId), DispatcherPriority.Loaded);
                return;
            }

            _retries = 0;

            int direction = Direction;
            bool animate = direction != 0 && Outgoing is ContentPresenter presenter && presenter.Content != null;
            double width = AssociatedObject.ActualWidth;
            if (!animate || width <= 1)
            {
                ParkAtRest();
                if (animate)
                    Complete(slideId);
                return;
            }

            int playId = ++_playId;
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
            ease.Freeze();
            double travel = direction * width;

            Outgoing.Visibility = Visibility.Visible;
            var outgoingTransform = Prepare(Outgoing, 0);
            var incomingTransform = Prepare(Incoming, 0);

            var outgoingAnimation = CreateAnimation(0, -travel, ease);
            var incomingAnimation = CreateAnimation(travel, 0, ease);
            incomingAnimation.Completed += (_, _) =>
            {
                if (_detached || playId != _playId)
                    return;

                // 等这一帧的动画时钟处理完，再换掉变换对象。在 Completed 里改原变换会被时钟写回去。
                Dispatcher.BeginInvoke(() => Finish(playId, slideId), DispatcherPriority.Loaded);
            };

            outgoingTransform.BeginAnimation(TranslateTransform.XProperty, outgoingAnimation);
            incomingTransform.BeginAnimation(TranslateTransform.XProperty, incomingAnimation);
        }

        private void Finish(int playId, int slideId)
        {
            if (_detached || playId != _playId)
                return;

            ParkAtRest();
            Complete(slideId);
        }

        /// <summary>
        /// 丢掉正在播放的变换，让当前公告回到卡片内，并藏起已经滑出的一页。
        /// </summary>
        private void ParkAtRest()
        {
            if (Incoming != null)
                Incoming.RenderTransform = new TranslateTransform(0, 0);

            if (Outgoing == null)
                return;

            Outgoing.RenderTransform = new TranslateTransform(0, 0);
            Outgoing.Visibility = Visibility.Collapsed;
        }

        private void Complete(int slideId)
        {
            if (CompletedCommand?.CanExecute(slideId) == true)
                CompletedCommand.Execute(slideId);
        }

        private static TranslateTransform Prepare(UIElement element, double x)
        {
            var transform = EnsureTransform(element);
            transform.BeginAnimation(TranslateTransform.XProperty, null);
            transform.X = x;
            return transform;
        }

        private static TranslateTransform EnsureTransform(UIElement element)
        {
            if (element.RenderTransform is TranslateTransform transform)
                return transform;

            transform = new TranslateTransform();
            element.RenderTransform = transform;
            return transform;
        }

        private static DoubleAnimation CreateAnimation(double from, double to, EasingFunctionBase ease)
        {
            return new DoubleAnimation(from, to, SlideDuration)
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.HoldEnd
            };
        }
    }
}
