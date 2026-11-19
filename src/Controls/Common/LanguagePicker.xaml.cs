using GeoChemistryNexus.ViewModels;
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
using System.Windows.Threading;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 统一的“添加语言”控件（图解 / 地质温压计 / EPMA 模板编辑器共用）：
    /// 触发按钮 + 带阴影和搜索框的弹层（支持 Enter 切换第一个匹配项、Esc 关闭）。
    /// 只处理纯界面交互；添加 / 移除语言的业务逻辑由 <see cref="ToggleCommand"/> 指向的 ViewModel 命令完成。
    /// </summary>
    public partial class LanguagePicker : UserControl
    {
        /// <summary>弹层外层为阴影预留的留白，需与 Styles.xaml 中 LanguagePickerPopupSurfaceStyle 的 Margin 保持一致。</summary>
        private const double PopupShadowMargin = 24;

        /// <summary>弹层与触发按钮之间的间距。</summary>
        private const double PopupGap = 4;

        private ICollectionView? _view;

        #region 依赖属性

        public static readonly DependencyProperty CandidatesProperty =
            DependencyProperty.Register(nameof(Candidates), typeof(IEnumerable<EpmaEditorLanguageOption>), typeof(LanguagePicker),
                new PropertyMetadata(null, OnCandidatesChanged));

        public static readonly DependencyProperty ToggleCommandProperty =
            DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(LanguagePicker),
                new PropertyMetadata(null));

        public static readonly DependencyProperty SelectAllCommandProperty =
            DependencyProperty.Register(nameof(SelectAllCommand), typeof(ICommand), typeof(LanguagePicker),
                new PropertyMetadata(null));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(LanguagePicker),
                new PropertyMetadata(false));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(LanguagePicker),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        private static readonly DependencyPropertyKey HasNoResultsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasNoResults), typeof(bool), typeof(LanguagePicker),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasNoResultsProperty = HasNoResultsPropertyKey.DependencyProperty;

        /// <summary>候选语言列表（通常是 ViewModel 的 LanguageCandidates）。</summary>
        public IEnumerable<EpmaEditorLanguageOption>? Candidates
        {
            get => (IEnumerable<EpmaEditorLanguageOption>?)GetValue(CandidatesProperty);
            set => SetValue(CandidatesProperty, value);
        }

        /// <summary>点击候选项后执行的命令，参数为被点击的 <see cref="EpmaEditorLanguageOption"/>（已添加则移除，未添加则添加）。</summary>
        public ICommand? ToggleCommand
        {
            get => (ICommand?)GetValue(ToggleCommandProperty);
            set => SetValue(ToggleCommandProperty, value);
        }

        /// <summary>弹层标题行的“全选”：一次加入尚未添加的语言。</summary>
        public ICommand? SelectAllCommand
        {
            get => (ICommand?)GetValue(SelectAllCommandProperty);
            set => SetValue(SelectAllCommandProperty, value);
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

        public bool HasNoResults => (bool)GetValue(HasNoResultsProperty);

        #endregion

        public LanguagePicker()
        {
            InitializeComponent();
            DropPopup.CustomPopupPlacementCallback = PlacePopup;
        }

        /// <summary>让触发按钮重新获得焦点（例如从其他页面跳回时）。</summary>
        public void FocusTrigger() => TriggerButton.Focus();

        #region 属性变化

        private static void OnCandidatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LanguagePicker)d).RebuildView();

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LanguagePicker)d;
            control._view?.Refresh();
            control.UpdateHasNoResults();
        }

        private void RebuildView()
        {
            if (_view != null)
                ((System.Collections.Specialized.INotifyCollectionChanged)_view).CollectionChanged -= View_CollectionChanged;

            if (Candidates == null)
            {
                _view = null;
                CandidateList.ItemsSource = null;
                UpdateHasNoResults();
                return;
            }

            // 候选集合通常是 ObservableCollection，直接包装以跟随增删；否则复制一份
            IList source = Candidates as IList ?? Candidates.ToList();
            var view = new ListCollectionView(source)
            {
                Filter = item => item is EpmaEditorLanguageOption option && option.Matches(SearchText)
            };
            ((System.Collections.Specialized.INotifyCollectionChanged)view).CollectionChanged += View_CollectionChanged;

            _view = view;
            CandidateList.ItemsSource = view;
            UpdateHasNoResults();
        }

        private void View_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            => UpdateHasNoResults();

        private void UpdateHasNoResults()
            => SetValue(HasNoResultsPropertyKey, _view != null && _view.IsEmpty);

        #endregion

        #region 弹层

        private CustomPopupPlacement[] PlacePopup(Size popupSize, Size targetSize, Point offset)
        {
            // 与触发按钮右对齐；下方空间不足时翻转到上方
            double x = targetSize.Width - popupSize.Width + PopupShadowMargin;
            return new[]
            {
                new CustomPopupPlacement(new Point(x, targetSize.Height + PopupGap - PopupShadowMargin), PopupPrimaryAxis.Horizontal),
                new CustomPopupPlacement(new Point(x, -popupSize.Height - PopupGap + PopupShadowMargin), PopupPrimaryAxis.Horizontal),
            };
        }

        private void TriggerButton_Click(object sender, RoutedEventArgs e)
            => IsDropDownOpen = true;

        private void DropPopup_Opened(object? sender, EventArgs e)
        {
            // 每次展开都清空搜索，并把焦点交给搜索框
            SearchText = string.Empty;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => SearchBox.Focus()));
        }

        private void CloseDropDown()
        {
            IsDropDownOpen = false;
            TriggerButton.Focus();
        }

        #endregion

        #region 键盘

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    // Enter：切换当前筛选结果中的第一个可用项
                    var target = _view?.Cast<EpmaEditorLanguageOption>().FirstOrDefault(option => option.CanToggle);
                    if (target != null && ToggleCommand is { } command && command.CanExecute(target))
                        command.Execute(target);
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
