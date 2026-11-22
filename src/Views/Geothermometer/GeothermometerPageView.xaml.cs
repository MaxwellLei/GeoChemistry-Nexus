using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
        private static GeothermometerPageView? commonPage = null;
        private readonly GeothermometerPageViewModel _viewModel;
        private Worksheet? _boundSelectionWorksheet;

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
            ReoGridImeHelper.Attach(MyReoGrid);
            ReoGridDoubleClickFillHelper.Attach(MyReoGrid); // 双击填充柄自动向下填充（类似 Excel）
            BindWorksheetEvents();
            MyReoGrid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
            _viewModel.AttachWorksheetRowExpansionEvents(MyReoGrid.CurrentWorksheet);

            // 监听 ViewModel 属性变化，处理滚动复位
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// 当 ViewModel 属性变化时，处理计算详情面板的布局
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeothermometerPageViewModel.DetailScrollResetToken))
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

        private void BindWorksheetEvents()
        {
            _boundSelectionWorksheet?.SelectionRangeChanged -= OnSelectionRangeChanged;

            if (MyReoGrid?.CurrentWorksheet == null)
            {
                return;
            }

            _boundSelectionWorksheet = MyReoGrid.CurrentWorksheet;
            _boundSelectionWorksheet.SelectionRangeChanged += OnSelectionRangeChanged;
        }

        /// <summary>
        /// 当切换工作表时，重新绑定选区变化事件
        /// </summary>
        private void OnCurrentWorksheetChanged(object? sender, System.EventArgs e)
        {
            BindWorksheetEvents();
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
            if (row < 0) return;

            _viewModel.RefreshSelectedCellEditor(worksheet);
            _viewModel.OnRowSelected(worksheet, row);

            // 选中数据行只刷新计算详情内容，不改变用户当前侧边栏的展开/收起状态。
        }

        /// <summary>
        /// 模板 ListBox 内置 ScrollViewer 会吞掉滚轮；转发给侧栏外层 ScrollViewer，保证悬停在选项上也能滚动。
        /// </summary>
        private void PluginListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            var parentScrollViewer = FindVisualParent<ScrollViewer>(listBox);
            if (parentScrollViewer == null)
                return;

            e.Handled = true;
            parentScrollViewer.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = parentScrollViewer
            });
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                var parent = VisualTreeHelper.GetParent(child);
                if (parent is T match)
                    return match;
                child = parent;
            }

            return null;
        }

        public static Page GetPage()
        {
            return commonPage ??= new GeothermometerPageView();
        }

        public Task CheckUpdatesIfNeededAsync()
        {
            return _viewModel.CheckUpdatesIfNeededAsync();
        }
    }
}
