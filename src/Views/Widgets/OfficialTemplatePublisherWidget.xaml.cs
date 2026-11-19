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

        private bool _redirectingWheel;

        /// <summary>
        /// 列表和内层滚动区域在没有滚动条时也会吃掉滚轮。
        /// 内容没有超出区域时，把滚轮交给页面。
        /// </summary>
        private void Publisher_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_redirectingWheel || e.Handled)
                return;

            if (IsInsideOpenComboBox(e.OriginalSource as DependencyObject, this))
                return;

            if (HasInnerScrollableViewer(e.OriginalSource as DependencyObject))
                return;

            e.Handled = true;
            _redirectingWheel = true;
            try
            {
                PageScroll.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = PageScroll
                });
            }
            finally
            {
                _redirectingWheel = false;
            }
        }

        /// <summary>
        /// 鼠标下面有一块真正能滚动的区域（列表超出可视范围）时，滚轮留给它。
        /// </summary>
        private bool HasInnerScrollableViewer(DependencyObject? source)
        {
            for (var current = source; current != null && current != this; current = GetParentObject(current))
            {
                if (current is ScrollViewer viewer
                    && !ReferenceEquals(viewer, PageScroll)
                    && viewer.ScrollableHeight > 1)
                    return true;
            }

            return false;
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

        private static DependencyObject? GetParentObject(DependencyObject current)
        {
            if (current is Visual or System.Windows.Media.Media3D.Visual3D)
                return VisualTreeHelper.GetParent(current);

            return LogicalTreeHelper.GetParent(current);
        }

    }
}
