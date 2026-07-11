using GeoChemistryNexus.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// CipwPageView.xaml 的交互逻辑
    /// </summary>
    public partial class CipwPageView : Page
    {
        private static CipwPageView _instance;
        private readonly CipwPageViewModel _viewModel;

        /// <summary>
        /// 面板正常展开时的记录高度
        /// </summary>
        private double _normalPanelHeight = 180;

        /// <summary>
        /// 诊断详情面板最小高度
        /// </summary>
        private const double MinDetailPanelHeight = 80;

        /// <summary>
        /// 用户是否手动收缩了面板（锁定收缩状态）
        /// </summary>
        private bool _userManuallyCollapsed = false;

        public CipwPageView()
        {
            InitializeComponent();
            _viewModel = new CipwPageViewModel();
            this.DataContext = _viewModel;

            // 初始化工作表
            _viewModel.InitializeWorksheet(CipwReoGrid);

            // 默认状态：收缩为状态条
            CollapseDiagnosticPanel(saveHeight: false);

            // 监听选区变化
            CipwReoGrid.CurrentWorksheet.SelectionRangeChanged += OnSelectionRangeChanged;
            CipwReoGrid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
        }

        /// <summary>
        /// 当切换工作表时重新绑定选区变化事件
        /// </summary>
        private void OnCurrentWorksheetChanged(object? sender, System.EventArgs e)
        {
            var worksheet = CipwReoGrid.CurrentWorksheet;
            if (worksheet != null)
            {
                worksheet.SelectionRangeChanged += OnSelectionRangeChanged;
            }
        }

        /// <summary>
        /// 选区变化时更新诊断数据
        /// 如果用户没有手动收缩面板，选中有数据的行时自动展开
        /// </summary>
        private void OnSelectionRangeChanged(object? sender, RangeEventArgs e)
        {
            if (_viewModel == null) return;

            var worksheet = CipwReoGrid.CurrentWorksheet;
            if (worksheet == null) return;

            var selRange = worksheet.SelectionRange;
            int row = selRange.Row;

            // 跳过表头行
            if (row < 1) return;

            _viewModel.OnRowSelected(worksheet, row);

            // 如果用户没有手动锁定收缩，且当前行有诊断数据，自动展开面板
            if (!_userManuallyCollapsed && _viewModel.HasDiagnosticData && !_viewModel.IsDiagnosticPanelExpanded)
            {
                ExpandDiagnosticPanel();
            }
        }

        /// <summary>
        /// 展开/收缩诊断面板
        /// 收缩：面板缩为底部状态条（仅标题栏可见），并锁定不自动展开
        /// 展开：恢复正常高度，解除锁定
        /// </summary>
        private void OnToggleExpandClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel.IsDiagnosticPanelExpanded)
            {
                CollapseDiagnosticPanel(saveHeight: true);
                _userManuallyCollapsed = true;
            }
            else
            {
                ExpandDiagnosticPanel();
                _userManuallyCollapsed = false;
            }
        }

        /// <summary>
        /// 拖动分隔条结束后记录当前面板高度
        /// </summary>
        private void OnDetailSplitterDragCompleted(object? sender, DragCompletedEventArgs e)
        {
            RememberDetailPanelHeight();
        }

        /// <summary>
        /// 展开诊断面板并显示可拖动分隔条
        /// </summary>
        private void ExpandDiagnosticPanel()
        {
            DiagnosticRowDef.MinHeight = MinDetailPanelHeight;
            DiagnosticRowDef.Height = new GridLength(_normalPanelHeight);
            DetailSplitter.Visibility = Visibility.Visible;
            _viewModel.IsDiagnosticPanelExpanded = true;
            ExpandIcon.Text = "\uE70D";
        }

        /// <summary>
        /// 收缩诊断面板并隐藏分隔条
        /// </summary>
        private void CollapseDiagnosticPanel(bool saveHeight)
        {
            if (saveHeight)
            {
                RememberDetailPanelHeight();
            }

            DiagnosticRowDef.MinHeight = 0;
            DiagnosticRowDef.Height = GridLength.Auto;
            DetailSplitter.Visibility = Visibility.Collapsed;
            _viewModel.IsDiagnosticPanelExpanded = false;
            ExpandIcon.Text = "\uE70E";
        }

        /// <summary>
        /// 记录当前诊断面板高度，供下次展开时恢复
        /// </summary>
        private void RememberDetailPanelHeight()
        {
            if (DiagnosticRowDef.Height.IsAbsolute && DiagnosticRowDef.Height.Value >= MinDetailPanelHeight)
            {
                _normalPanelHeight = DiagnosticRowDef.Height.Value;
            }
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
            if (_instance == null)
            {
                _instance = new CipwPageView();
            }
            return _instance;
        }
    }
}
