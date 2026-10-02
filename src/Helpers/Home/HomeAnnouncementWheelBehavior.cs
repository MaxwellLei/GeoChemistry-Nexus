using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 公告卡片滚轮翻页。向上为上一页，向下为下一页。
    /// 触控板会连续给出较小的 Delta，累计到一格后再翻一页。
    /// </summary>
    public class HomeAnnouncementWheelBehavior : Behavior<UIElement>
    {
        private const int WheelStep = 80;

        private int _accumulatedDelta;

        public static readonly DependencyProperty PreviousCommandProperty =
            DependencyProperty.Register(
                nameof(PreviousCommand),
                typeof(ICommand),
                typeof(HomeAnnouncementWheelBehavior),
                new PropertyMetadata(null));

        public static readonly DependencyProperty NextCommandProperty =
            DependencyProperty.Register(
                nameof(NextCommand),
                typeof(ICommand),
                typeof(HomeAnnouncementWheelBehavior),
                new PropertyMetadata(null));

        public ICommand? PreviousCommand
        {
            get => (ICommand?)GetValue(PreviousCommandProperty);
            set => SetValue(PreviousCommandProperty, value);
        }

        public ICommand? NextCommand
        {
            get => (ICommand?)GetValue(NextCommandProperty);
            set => SetValue(NextCommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
            base.OnDetaching();
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!CanExecute(PreviousCommand) && !CanExecute(NextCommand))
            {
                _accumulatedDelta = 0;
                return;
            }

            _accumulatedDelta += e.Delta;
            if (System.Math.Abs(_accumulatedDelta) < WheelStep)
            {
                e.Handled = true;
                return;
            }

            var command = _accumulatedDelta > 0 ? PreviousCommand : NextCommand;
            _accumulatedDelta = 0;
            if (CanExecute(command))
                command!.Execute(null);

            e.Handled = true;
        }

        private static bool CanExecute(ICommand? command) => command?.CanExecute(null) == true;
    }
}
