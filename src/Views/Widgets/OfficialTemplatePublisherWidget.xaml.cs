using GeoChemistryNexus.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class OfficialTemplatePublisherWidget : UserControl
    {
        private OfficialTemplatePublisherViewModel? _viewModel;

        public OfficialTemplatePublisherWidget()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = e.NewValue as OfficialTemplatePublisherViewModel;
            if (_viewModel != null)
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OfficialTemplatePublisherViewModel.SecretKey)
                && _viewModel != null
                && string.IsNullOrEmpty(_viewModel.SecretKey))
            {
                SecretKeyBox.Password = string.Empty;
            }
        }

        private void SecretKeyBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is OfficialTemplatePublisherViewModel vm && sender is PasswordBox box)
                vm.SecretKey = box.Password;
        }

        private bool _redirectingAnnouncementWheel;

        /// <summary>
        /// 公告与版本区域内的文本框、下拉框、列表和内层滚动条会吃掉滚轮。
        /// 当前控件还能滚动时留给它；否则把滚轮交给页面。
        /// </summary>
        private void AnnouncementSection_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_redirectingAnnouncementWheel || e.Handled || sender is not DependencyObject section)
                return;

            if (IsInsideOpenComboBox(e.OriginalSource as DependencyObject, section))
                return;

            var page = FindVisualParent<ScrollViewer>(section);
            if (page == null)
                return;

            var target = FindWheelScrollTarget(e.OriginalSource as DependencyObject, page, e.Delta) ?? page;
            e.Handled = true;
            _redirectingAnnouncementWheel = true;
            try
            {
                target.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = target
                });
            }
            finally
            {
                _redirectingAnnouncementWheel = false;
            }
        }

        private static bool IsInsideOpenComboBox(DependencyObject? source, DependencyObject boundary)
        {
            for (var current = source; current != null && current != boundary; current = GetParentObject(current))
            {
                if (current is ComboBox combo && combo.IsDropDownOpen)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 从鼠标下的控件向上找第一个还能沿滚轮方向滚动的 ScrollViewer，找不到则由页面滚动。
        /// </summary>
        private static ScrollViewer? FindWheelScrollTarget(DependencyObject? source, ScrollViewer page, int delta)
        {
            for (var current = source; current != null && current != page; current = GetParentObject(current))
            {
                if (current is ScrollViewer viewer && CanScrollVertically(viewer, delta))
                    return viewer;
            }

            return null;
        }

        private static bool CanScrollVertically(ScrollViewer viewer, int delta)
        {
            if (viewer.ScrollableHeight <= 0)
                return false;

            if (delta > 0)
                return viewer.VerticalOffset > 0.5;

            return viewer.VerticalOffset < viewer.ScrollableHeight - 0.5;
        }

        private static DependencyObject? GetParentObject(DependencyObject current)
        {
            if (current is Visual or System.Windows.Media.Media3D.Visual3D)
                return VisualTreeHelper.GetParent(current);

            return LogicalTreeHelper.GetParent(current);
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                var parent = GetParentObject(child);
                if (parent is T match)
                    return match;

                child = parent;
            }

            return null;
        }
    }
}
