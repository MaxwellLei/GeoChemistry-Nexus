using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System.Windows;
using System.Windows.Controls;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// NiggliPageView.xaml 的交互逻辑
    /// 右侧检查器的展开 / 收起由公共表格控件（ReoGridTableHost）负责，这里只转发选区变化与窗口操作。
    /// </summary>
    public partial class NiggliPageView : Page
    {
        private static NiggliPageView? _instance;
        private readonly NiggliPageViewModel _viewModel;

        private Worksheet? _boundWorksheet;

        public NiggliPageView()
        {
            InitializeComponent();
            _viewModel = new NiggliPageViewModel();
            this.DataContext = _viewModel;

            // 初始化工作表
            _viewModel.InitializeWorksheet(NiggliReoGrid);

            // 监听选区变化
            BindWorksheetEvents();
            NiggliReoGrid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
        }

        private void BindWorksheetEvents()
        {
            if (_boundWorksheet != null)
            {
                _boundWorksheet.SelectionRangeChanged -= OnSelectionRangeChanged;
            }

            if (NiggliReoGrid?.CurrentWorksheet == null)
            {
                return;
            }

            _boundWorksheet = NiggliReoGrid.CurrentWorksheet;
            _boundWorksheet.SelectionRangeChanged += OnSelectionRangeChanged;
        }

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

            var worksheet = NiggliReoGrid.CurrentWorksheet;
            if (worksheet == null) return;

            int row = worksheet.SelectionRange.Row;
            if (row < 0) return;

            _viewModel.OnRowSelected(worksheet, row);
        }

        private void OnHelpClick(object? sender, RoutedEventArgs e)
        {
            var helpWindow = new NiggliHelpWindow
            {
                Owner = Window.GetWindow(this)
            };
            WindowActivationHelper.AttachOwnerFocusPreservation(helpWindow, helpWindow.Owner);
            helpWindow.ShowDialog();
        }

        private void OnFe3FractionLostFocus(object? sender, RoutedEventArgs e)
        {
            _viewModel.ClampFe3Fraction();
        }

        public static Page GetPage()
        {
            if (_instance == null)
            {
                _instance = new NiggliPageView();
            }
            return _instance;
        }
    }
}
