using GeoChemistryNexus.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// GeothermometerPageView.xaml 的交互逻辑
    /// </summary>
    public partial class GeothermometerPageView : Page
    {
        private static GeothermometerPageView commonPage = null;
        private GeothermometerPageViewModel _viewModel;

        /// <summary>
        /// 面板正常展开时的记录高度（与 CIPW 计算详情一致）
        /// </summary>
        private double _normalDetailPanelHeight = 180;

        /// <summary>
        /// 是否处于最大化状态
        /// </summary>
        private bool _isDetailPanelMaximized = false;

        /// <summary>
        /// 用户是否手动收缩了面板（锁定收缩状态）
        /// </summary>
        private bool _userManuallyCollapsed = false;

        public GeothermometerPageView()
        {
            InitializeComponent();
            _viewModel = new GeothermometerPageViewModel();
            this.DataContext = _viewModel;

            // 将帮助文档 RichTextBox 控件的引用传递给 ViewModel
            _viewModel.SetHelpRichTextBox(this.HelpDocRichTextBox);

            // 监听 ReoGrid 选区变化事件，用于触发详细计算
            MyReoGrid.CurrentWorksheet.SelectionRangeChanged += OnSelectionRangeChanged;
            MyReoGrid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
            _viewModel.AttachWorksheetRowExpansionEvents(MyReoGrid.CurrentWorksheet);

            // 默认状态：收缩为底部状态条
            DetailRow.Height = GridLength.Auto;
            _viewModel.IsDetailPanelMinimized = true;
            DetailExpandIcon.Text = "\uE70E";

            // 监听 ViewModel 属性变化，处理计算详情面板的布局切换
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// 当 ViewModel 属性变化时，处理计算详情面板的布局
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeothermometerPageViewModel.IsDetailPanelMaximized))
            {
                SyncDetailPanelLayoutFromViewModel();
            }
            else if (e.PropertyName == nameof(GeothermometerPageViewModel.IsDetailPanelMinimized))
            {
                SyncDetailPanelLayoutFromViewModel();
            }
            else if (e.PropertyName == nameof(GeothermometerPageViewModel.HasCalculationData))
            {
                // 无数据时恢复默认布局
                if (!_viewModel.HasCalculationData)
                {
                    CollapseDetailPanel(resetManualLock: false);
                }
            }
        }

        /// <summary>
        /// 根据 ViewModel 状态同步计算详情面板布局
        /// </summary>
        private void SyncDetailPanelLayoutFromViewModel()
        {
            if (_viewModel.IsDetailPanelMaximized)
            {
                MaximizeDetailPanel();
            }
            else if (_viewModel.IsDetailPanelMinimized)
            {
                CollapseDetailPanel(resetManualLock: false);
            }
            else
            {
                ExpandDetailPanel(resetManualLock: false);
            }
        }

        /// <summary>
        /// 最大化/还原计算详情面板（与 CIPW 逻辑一致）
        /// </summary>
        private void OnMaximizeDetailPanelClick(object? sender, RoutedEventArgs e)
        {
            _viewModel.ToggleMaximizeDetailPanelCommand.Execute(null);
            _userManuallyCollapsed = false;
            SyncDetailPanelLayoutFromViewModel();
        }

        /// <summary>
        /// 展开/收缩计算详情面板（与 CIPW 逻辑一致）
        /// </summary>
        private void OnToggleDetailExpandClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel.IsDetailPanelMinimized)
            {
                _viewModel.RestoreDetailPanelCommand.Execute(null);
                _userManuallyCollapsed = false;
            }
            else
            {
                _viewModel.MinimizeDetailPanelCommand.Execute(null);
                _userManuallyCollapsed = true;
            }

            SyncDetailPanelLayoutFromViewModel();
        }

        /// <summary>
        /// 最大化：表格保留小部分可见，计算详情占主要区域
        /// </summary>
        private void MaximizeDetailPanel()
        {
            if (!_viewModel.IsDetailPanelMinimized)
            {
                if (DetailRow.Height.IsAbsolute && DetailRow.Height.Value > 40)
                {
                    _normalDetailPanelHeight = DetailRow.Height.Value;
                }
            }

            TableRow.Height = new GridLength(1, GridUnitType.Star);
            DetailRow.Height = new GridLength(3, GridUnitType.Star);
            DetailPanelBorder.MaxHeight = double.PositiveInfinity;
            MyReoGrid.Visibility = Visibility.Visible;
            _isDetailPanelMaximized = true;
            DetailMaximizeIcon.Text = "\uE73F";
            DetailExpandIcon.Text = "\uE70D";
        }

        /// <summary>
        /// 展开：恢复正常高度并解除自动展开锁定
        /// </summary>
        private void ExpandDetailPanel(bool resetManualLock)
        {
            TableRow.Height = new GridLength(1, GridUnitType.Star);
            DetailRow.Height = new GridLength(_normalDetailPanelHeight);
            DetailPanelBorder.MaxHeight = 260;
            MyReoGrid.Visibility = Visibility.Visible;
            _isDetailPanelMaximized = false;
            DetailMaximizeIcon.Text = "\uE740";
            DetailExpandIcon.Text = "\uE70D";

            if (resetManualLock)
            {
                _userManuallyCollapsed = false;
            }
        }

        /// <summary>
        /// 收缩：仅保留标题栏状态条
        /// </summary>
        private void CollapseDetailPanel(bool resetManualLock)
        {
            if (_isDetailPanelMaximized)
            {
                TableRow.Height = new GridLength(1, GridUnitType.Star);
                _isDetailPanelMaximized = false;
                DetailMaximizeIcon.Text = "\uE740";
            }
            else if (DetailRow.Height.IsAbsolute && DetailRow.Height.Value > 40)
            {
                _normalDetailPanelHeight = DetailRow.Height.Value;
            }

            TableRow.Height = new GridLength(1, GridUnitType.Star);
            DetailRow.Height = GridLength.Auto;
            DetailPanelBorder.MaxHeight = 260;
            MyReoGrid.Visibility = Visibility.Visible;
            DetailExpandIcon.Text = "\uE70E";

            if (resetManualLock)
            {
                _userManuallyCollapsed = true;
            }
        }

        /// <summary>
        /// 当切换工作表时，重新绑定选区变化事件
        /// </summary>
        private void OnCurrentWorksheetChanged(object? sender, System.EventArgs e)
        {
            var worksheet = MyReoGrid.CurrentWorksheet;
            if (worksheet != null)
            {
                worksheet.SelectionRangeChanged += OnSelectionRangeChanged;
            }

            _viewModel.AttachWorksheetRowExpansionEvents(MyReoGrid.CurrentWorksheet);
        }

        /// <summary>
        /// 当选区变化时，提取选中行数据并计算中间步骤
        /// </summary>
        private void OnSelectionRangeChanged(object? sender, RangeEventArgs e)
        {
            if (_viewModel == null) return;

            var worksheet = MyReoGrid.CurrentWorksheet;
            if (worksheet == null) return;

            // 获取选中区域的起始行
            var selRange = worksheet.SelectionRange;
            int row = selRange.Row;

            // 跳过表头行（第0行）
            if (row < 1) return;

            _viewModel.OnRowSelected(worksheet, row);

            // 如果用户没有手动锁定收缩，且当前行有计算数据，自动展开面板
            if (!_userManuallyCollapsed && !_isDetailPanelMaximized && _viewModel.HasCalculationData)
            {
                if (_viewModel.IsDetailPanelMinimized)
                {
                    _viewModel.RestoreDetailPanelCommand.Execute(null);
                }

                ExpandDetailPanel(resetManualLock: false);
            }
        }

        public static Page GetPage()
        {
            if (commonPage == null)
            {
                commonPage = new GeothermometerPageView();
            }
            return commonPage;
        }

        public Task CheckUpdatesIfNeededAsync()
        {
            return _viewModel.CheckUpdatesIfNeededAsync();
        }
    }
}
