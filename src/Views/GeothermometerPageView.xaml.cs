using GeoChemistryNexus.ViewModels;
using System.ComponentModel;
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
        /// 保存正常状态下的 MaxHeight，用于从最大化恢复
        /// </summary>
        private const double NormalMaxHeight = 260;

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

            // 监听 ViewModel 属性变化，处理最大化/最小化时的布局切换
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// 当 ViewModel 属性变化时，处理计算详情面板的布局
        /// </summary>
        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeothermometerPageViewModel.IsDetailPanelMaximized))
            {
                UpdateDetailPanelLayout();
            }
            else if (e.PropertyName == nameof(GeothermometerPageViewModel.IsDetailPanelMinimized))
            {
                UpdateDetailPanelLayout();
            }
            else if (e.PropertyName == nameof(GeothermometerPageViewModel.HasCalculationData))
            {
                // 无数据时恢复默认布局
                if (!_viewModel.HasCalculationData)
                {
                    RestoreDefaultLayout();
                }
            }
        }

        /// <summary>
        /// 根据当前状态更新计算详情面板的布局
        /// </summary>
        private void UpdateDetailPanelLayout()
        {
            if (_viewModel.IsDetailPanelMaximized)
            {
                // 最大化：隐藏表格，详情面板撑满
                TableRow.Height = new GridLength(0);
                DetailRow.Height = new GridLength(1, GridUnitType.Star);
                DetailPanelBorder.MaxHeight = double.PositiveInfinity;
                MyReoGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                // 还原：恢复默认布局
                RestoreDefaultLayout();
            }
        }

        /// <summary>
        /// 恢复默认布局（表格占主要空间，详情面板自适应高度）
        /// </summary>
        private void RestoreDefaultLayout()
        {
            TableRow.Height = new GridLength(1, GridUnitType.Star);
            DetailRow.Height = GridLength.Auto;
            DetailPanelBorder.MaxHeight = NormalMaxHeight;
            MyReoGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 当切换工作表时，重新绑定选区变化事件
        /// </summary>
        private void OnCurrentWorksheetChanged(object sender, System.EventArgs e)
        {
            var worksheet = MyReoGrid.CurrentWorksheet;
            if (worksheet != null)
            {
                worksheet.SelectionRangeChanged += OnSelectionRangeChanged;
            }
        }

        /// <summary>
        /// 当选区变化时，提取选中行数据并计算中间步骤
        /// </summary>
        private void OnSelectionRangeChanged(object sender, RangeEventArgs e)
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
        }

        public static Page GetPage()
        {
            if (commonPage == null)
            {
                commonPage = new GeothermometerPageView();
            }
            return commonPage;
        }
    }
}
