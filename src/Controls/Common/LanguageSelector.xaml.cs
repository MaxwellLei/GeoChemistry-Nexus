using GeoChemistryNexus.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 界面语言选择器：触发按钮 + 带搜索的下拉面板（支持键盘 ↑ ↓ / Enter / Esc）。
    /// 只处理纯界面交互；语言切换的业务逻辑由 <see cref="SelectionChangedCommand"/> 指向的 ViewModel 命令完成。
    /// </summary>
    public partial class LanguageSelector : UserControl
    {
        /// <summary>弹层与触发按钮之间的间距。</summary>
        private const double PopupGap = 6;

        private ICollectionView? _view;

        #region 依赖属性

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable<AppLanguageOption>), typeof(LanguageSelector),
                new PropertyMetadata(null, OnItemsChanged));

        public static readonly DependencyProperty SelectedCodeProperty =
            DependencyProperty.Register(nameof(SelectedCode), typeof(string), typeof(LanguageSelector),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedCodeChanged));

        public static readonly DependencyProperty SelectionChangedCommandProperty =
            DependencyProperty.Register(nameof(SelectionChangedCommand), typeof(ICommand), typeof(LanguageSelector),
                new PropertyMetadata(null));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(LanguageSelector),
                new PropertyMetadata(false));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(LanguageSelector),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        private static readonly DependencyPropertyKey SelectedOptionPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(SelectedOption), typeof(AppLanguageOption), typeof(LanguageSelector),
                new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedOptionProperty = SelectedOptionPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey HasNoResultsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasNoResults), typeof(bool), typeof(LanguageSelector),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasNoResultsProperty = HasNoResultsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ItemCountPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ItemCount), typeof(int), typeof(LanguageSelector),
                new PropertyMetadata(0));

        public static readonly DependencyProperty ItemCountProperty = ItemCountPropertyKey.DependencyProperty;

        /// <summary>可选语言列表。</summary>
        public IEnumerable<AppLanguageOption>? Items
        {
            get => (IEnumerable<AppLanguageOption>?)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        /// <summary>当前选中的文化代码（双向）。</summary>
        public string SelectedCode
        {
            get => (string)GetValue(SelectedCodeProperty);
            set => SetValue(SelectedCodeProperty, value);
        }

        /// <summary>用户选择了不同的语言后执行的命令（此时 <see cref="SelectedCode"/> 已更新，参数为新代码）。</summary>
        public ICommand? SelectionChangedCommand
        {
            get => (ICommand?)GetValue(SelectionChangedCommandProperty);
            set => SetValue(SelectionChangedCommandProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public AppLanguageOption? SelectedOption => (AppLanguageOption?)GetValue(SelectedOptionProperty);

        public bool HasNoResults => (bool)GetValue(HasNoResultsProperty);

        public int ItemCount => (int)GetValue(ItemCountProperty);

        #endregion

        public LanguageSelector()
        {
            InitializeComponent();
        }

        #region 属性变化

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LanguageSelector)d;
            control.RebuildView();
            control.UpdateSelectedOption();
        }

        private static void OnSelectedCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LanguageSelector)d).UpdateSelectedOption();

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LanguageSelector)d;
            control._view?.Refresh();
            control.LanguageList.SelectedIndex = -1;
            control.UpdateHasNoResults();
        }

        private void RebuildView()
        {
            var list = Items?.ToList() ?? new List<AppLanguageOption>();
            SetValue(ItemCountPropertyKey, list.Count);

            var view = new ListCollectionView(list)
            {
                Filter = item => item is AppLanguageOption option && option.Matches(SearchText)
            };
            _view = view;
            LanguageList.ItemsSource = view;
            UpdateHasNoResults();
        }

        private void UpdateHasNoResults()
            => SetValue(HasNoResultsPropertyKey, _view != null && _view.IsEmpty);

        /// <summary>根据 SelectedCode 刷新触发按钮显示项与列表中的“当前”标记。</summary>
        private void UpdateSelectedOption()
        {
            AppLanguageOption? selected = null;

            foreach (var option in Items ?? Enumerable.Empty<AppLanguageOption>())
            {
                bool isCurrent = string.Equals(option.Code, SelectedCode, StringComparison.OrdinalIgnoreCase);
                option.IsCurrent = isCurrent;
                if (isCurrent)
                    selected = option;
            }

            SetValue(SelectedOptionPropertyKey, selected);
        }

        #endregion

        #region 弹层

        /// <summary>
        /// 按实际屏幕坐标校正弹层位置：弹层可见边框右缘与触发按钮右缘对齐（左侧可向外伸出），
        /// 并与按钮保持 <see cref="PopupGap"/> 间距（下方空间不足被系统翻转到上方时同样适用）。
        /// 直接测量屏幕坐标，避免 DPI / 界面缩放（LayoutTransform）造成的单位换算误差。
        /// </summary>
        private void AlignPopup(int attempt)
        {
            if (!DropPopup.IsOpen || !PopupBorder.IsLoaded || !TriggerButton.IsLoaded)
                return;

            var dpi = VisualTreeHelper.GetDpi(this);
            double gapPx = PopupGap * dpi.DpiScaleY;

            Point btnTopRight = TriggerButton.PointToScreen(new Point(TriggerButton.ActualWidth, 0));
            Point btnBottomRight = TriggerButton.PointToScreen(new Point(TriggerButton.ActualWidth, TriggerButton.ActualHeight));
            Point boxTopRight = PopupBorder.PointToScreen(new Point(PopupBorder.ActualWidth, 0));
            Point boxBottomLeft = PopupBorder.PointToScreen(new Point(0, PopupBorder.ActualHeight));

            double dx = btnBottomRight.X - boxTopRight.X;

            bool isBelow = boxTopRight.Y >= btnTopRight.Y;
            double dy = isBelow
                ? (btnBottomRight.Y + gapPx) - boxTopRight.Y
                : (btnTopRight.Y - gapPx) - boxBottomLeft.Y;

            if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5)
                return;

            DropPopup.HorizontalOffset += dx / dpi.DpiScaleX;
            DropPopup.VerticalOffset += dy / dpi.DpiScaleY;

            // 偏移量变化会触发重新定位，下一轮再确认一次（最多 3 次，防止无法收敛时死循环）
            if (attempt < 3)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => AlignPopup(attempt + 1)));
            }
        }

        private void DropPopup_Opened(object? sender, EventArgs e)
        {
            // 每次展开都清空搜索、取消键盘高亮，并定位到当前语言
            SearchText = string.Empty;
            LanguageList.SelectedIndex = -1;

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => AlignPopup(0)));

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                SearchBox.Focus();

                if (SelectedOption != null)
                    LanguageList.ScrollIntoView(SelectedOption);
            }));
        }

        private void CloseDropDown()
        {
            IsDropDownOpen = false;
            TriggerButton.Focus();
        }

        #endregion

        #region 选择

        private void Commit(AppLanguageOption option)
        {
            bool changed = !string.Equals(option.Code, SelectedCode, StringComparison.OrdinalIgnoreCase);

            if (changed)
                SelectedCode = option.Code;

            CloseDropDown();

            if (changed && SelectionChangedCommand is { } command && command.CanExecute(option.Code))
                command.Execute(option.Code);
        }

        private void LanguageList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source)
                return;

            var container = ItemsControl.ContainerFromElement(LanguageList, source) as ListBoxItem;
            if (container?.DataContext is AppLanguageOption option)
            {
                Commit(option);
                e.Handled = true;
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int count = LanguageList.Items.Count;

            switch (e.Key)
            {
                case Key.Down:
                case Key.Up:
                    if (count > 0)
                    {
                        int step = e.Key == Key.Down ? 1 : -1;
                        int current = LanguageList.SelectedIndex;
                        int next = current < 0
                            ? (step > 0 ? 0 : count - 1)
                            : (current + step + count) % count;

                        LanguageList.SelectedIndex = next;
                        LanguageList.ScrollIntoView(LanguageList.SelectedItem);
                    }
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (count > 0)
                    {
                        var target = LanguageList.SelectedItem as AppLanguageOption
                                     ?? LanguageList.Items[0] as AppLanguageOption;
                        if (target != null)
                            Commit(target);
                    }
                    e.Handled = true;
                    break;

                case Key.Escape:
                    CloseDropDown();
                    e.Handled = true;
                    break;
            }
        }

        #endregion
    }
}
