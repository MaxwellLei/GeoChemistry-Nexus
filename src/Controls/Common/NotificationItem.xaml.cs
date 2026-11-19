using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Controls
{
    public partial class NotificationItem : UserControl
    {
        /// <summary>卡片滑出开始后，延迟多久开始收拢占位空间（让下方卡片上移）。</summary>
        private static readonly TimeSpan CollapseDelay = TimeSpan.FromMilliseconds(120);

        /// <summary>占位空间收拢时长。</summary>
        private static readonly TimeSpan CollapseDuration = TimeSpan.FromMilliseconds(380);

        private NotificationViewModel? _subscribedViewModel;

        public NotificationItem()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += (_, _) => Unsubscribe();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Unsubscribe();
            if (e.NewValue is NotificationViewModel vm)
            {
                _subscribedViewModel = vm;
                vm.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedViewModel = null;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NotificationViewModel.IsClosing)
                && _subscribedViewModel?.IsClosing == true)
            {
                BeginCollapse();
            }
        }

        /// <summary>
        /// 纯视图动画：把卡片占用的高度和外边距平滑收拢到 0，
        /// 下方的卡片会随之连续上移，而不是在卡片移除的瞬间一下子跳上去。
        /// </summary>
        private void BeginCollapse()
        {
            var startHeight = ActualHeight;
            if (startHeight <= 0)
                return;

            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

            var heightAnimation = new DoubleAnimation(startHeight, 0, CollapseDuration)
            {
                BeginTime = CollapseDelay,
                EasingFunction = ease,
                FillBehavior = FillBehavior.HoldEnd
            };
            var marginAnimation = new ThicknessAnimation(Margin, new Thickness(Margin.Left, 0, Margin.Right, 0), CollapseDuration)
            {
                BeginTime = CollapseDelay,
                EasingFunction = ease,
                FillBehavior = FillBehavior.HoldEnd
            };

            // 动画开始前先固定当前高度，避免 BeginTime 延迟期间高度回到自动值
            Height = startHeight;
            BeginAnimation(HeightProperty, heightAnimation);
            BeginAnimation(MarginProperty, marginAnimation);
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle || DataContext is not NotificationViewModel vm)
                return;

            if (vm.CloseCommand.CanExecute(null))
            {
                vm.CloseCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
