using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System.Windows;
using System.Windows.Controls;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// CipwPageView.xaml 的交互逻辑
    /// 右侧检查器的展开 / 收起由公共表格控件（ReoGridTableHost）负责，这里只转发选区变化与窗口操作。
    /// </summary>
    public partial class CipwPageView : Page
    {
        private static CipwPageView? _instance;
        private readonly CipwPageViewModel _viewModel;

        private Worksheet? _boundWorksheet;

        public CipwPageView()
        {
            InitializeComponent();
            _viewModel = new CipwPageViewModel();
            this.DataContext = _viewModel;

            // 初始化工作表
            _viewModel.InitializeWorksheet(CipwReoGrid);

            // 监听选区变化
            BindWorksheetEvents();
            CipwReoGrid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
        }

        private void BindWorksheetEvents()
        {
            _boundWorksheet?.SelectionRangeChanged -= OnSelectionRangeChanged;

            if (CipwReoGrid?.CurrentWorksheet == null)
            {
                return;
            }

            _boundWorksheet = CipwReoGrid.CurrentWorksheet;
            _boundWorksheet.SelectionRangeChanged += OnSelectionRangeChanged;
        }

        /// <summary>
        /// 当切换工作表时重新绑定选区变化事件
        /// </summary>
        private void OnCurrentWorksheetChanged(object? sender, System.EventArgs e)
        {
            BindWorksheetEvents();
        }

        /// <summary>
        /// 选区变化时更新检查器中的计算详情
        /// </summary>
        private void OnSelectionRangeChanged(object? sender, RangeEventArgs e)
        {
            if (_viewModel == null) return;

            var worksheet = CipwReoGrid.CurrentWorksheet;
            if (worksheet == null) return;

            int row = worksheet.SelectionRange.Row;
            if (row < 0) return;

            _viewModel.OnRowSelected(worksheet, row);
        }

        /// <summary>
        /// 打开CIPW算法说明窗口
        /// </summary>
        private void OnHelpClick(object? sender, RoutedEventArgs e)
        {
            var helpWindow = new CipwHelpWindow
            {
                Owner = Window.GetWindow(this)
            };
            WindowActivationHelper.AttachOwnerFocusPreservation(helpWindow, helpWindow.Owner);
            helpWindow.ShowDialog();
        }

        /// <summary>
        /// Fe3+/Fe 值失焦时校验范围 [0, 1]
        /// </summary>
        private void OnFe3FractionLostFocus(object? sender, RoutedEventArgs e)
        {
            _viewModel.ClampFe3Fraction();
        }

        public static Page GetPage()
        {
            _instance ??= new CipwPageView();
            return _instance;
        }
    }
}
