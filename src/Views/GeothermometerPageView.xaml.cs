using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
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
        /// 计算详情面板最小高度
        /// </summary>
        private const double MinDetailPanelHeight = 80;

        public GeothermometerPageView()
        {
            InitializeComponent();
            _viewModel = new GeothermometerPageViewModel();
            this.DataContext = _viewModel;

            if (Resources["ViewModelProxy"] is BindingProxy viewModelProxy)
                viewModelProxy.Data = _viewModel;

            // 将帮助文档 RichTextBox 控件的引用传递给 ViewModel
            _viewModel.SetHelpRichTextBox(this.HelpDocRichTextBox);

            // 监听 ReoGrid 选区变化事件，用于触发详细计算
            MyReoGrid.CurrentWorksheet.SelectionRangeChanged += OnSelectionRangeChanged;
            MyReoGrid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
            _viewModel.AttachWorksheetRowExpansionEvents(MyReoGrid.CurrentWorksheet);

            // 默认状态：收缩为底部状态条
            CollapseDetailPanel(resetManualLock: false);

            // 监听 ViewModel 属性变化，处理计算详情面板的布局切换
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// 当 ViewModel 属性变化时，处理计算详情面板的布局
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeothermometerPageViewModel.IsDetailPanelMinimized))
            {
                SyncDetailPanelLayoutFromViewModel();
            }
            else if (e.PropertyName == nameof(GeothermometerPageViewModel.DetailScrollResetToken))
            {
                ResetCalculationDetailScroll();
            }
            else if (e.PropertyName == nameof(GeothermometerPageViewModel.GridScrollResetToken))
            {
                ResetReoGridScroll();
            }
        }

        /// <summary>
        /// 将 ReoGrid 的横向/纵向滚动条复位到起始位置
        /// </summary>
        private void ResetReoGridScroll()
        {
            MyReoGrid?.ScrollCurrentWorksheet(0, 0);

            // 延迟再次复位，避免选区变更等后续操作重新触发滚动
            MyReoGrid?.Dispatcher.BeginInvoke(() =>
            {
                MyReoGrid?.ScrollCurrentWorksheet(0, 0);
            }, DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 将计算详情区域的横向/纵向滚动条复位到起始位置
        /// </summary>
        private void ResetCalculationDetailScroll()
        {
            CalculationDetailScrollViewer?.ScrollToVerticalOffset(0);
            CalculationDetailScrollViewer?.ScrollToHorizontalOffset(0);
        }

        /// <summary>
        /// 根据 ViewModel 状态同步计算详情面板布局
        /// </summary>
        private void SyncDetailPanelLayoutFromViewModel()
        {
            if (_viewModel.IsDetailPanelMinimized)
            {
                CollapseDetailPanel(resetManualLock: false);
            }
            else
            {
                ExpandDetailPanel(resetManualLock: false);
            }
        }

        /// <summary>
        /// 展开/收缩计算详情面板（与 CIPW 逻辑一致）
        /// </summary>
        private void OnToggleDetailExpandClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel.IsDetailPanelMinimized)
            {
                _viewModel.RestoreDetailPanelCommand.Execute(null);
            }
            else
            {
                _viewModel.MinimizeDetailPanelCommand.Execute(null);
            }

            SyncDetailPanelLayoutFromViewModel();
        }

        /// <summary>
        /// 拖动分隔条结束后记录当前面板高度
        /// </summary>
        private void OnDetailSplitterDragCompleted(object? sender, DragCompletedEventArgs e)
        {
            RememberDetailPanelHeight();
        }

        /// <summary>
        /// 展开：恢复正常高度
        /// </summary>
        private void ExpandDetailPanel(bool resetManualLock)
        {
            TableRow.Height = new GridLength(1, GridUnitType.Star);
            DetailRow.MinHeight = MinDetailPanelHeight;
            DetailRow.Height = new GridLength(_normalDetailPanelHeight);
            DetailSplitter.Visibility = Visibility.Visible;
            MyReoGrid.Visibility = Visibility.Visible;
            DetailExpandIcon.Text = "\uE70D";

        }

        /// <summary>
        /// 收缩：仅保留标题栏状态条
        /// </summary>
        private void CollapseDetailPanel(bool resetManualLock)
        {
            RememberDetailPanelHeight();

            TableRow.Height = new GridLength(1, GridUnitType.Star);
            DetailRow.MinHeight = 0;
            DetailRow.Height = GridLength.Auto;
            DetailSplitter.Visibility = Visibility.Collapsed;
            MyReoGrid.Visibility = Visibility.Visible;
            DetailExpandIcon.Text = "\uE70E";

        }

        /// <summary>
        /// 记录当前计算详情面板高度，供下次展开时恢复
        /// </summary>
        private void RememberDetailPanelHeight()
        {
            if (DetailRow.Height.IsAbsolute && DetailRow.Height.Value >= MinDetailPanelHeight)
            {
                _normalDetailPanelHeight = DetailRow.Height.Value;
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

            // 选中数据行只刷新计算详情内容，不改变用户当前的展开/收缩状态。
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
