using GeoChemistryNexus.ViewModels;
using System.Windows;
using System.Windows.Controls;
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
        /// 是否处于最大化状态
        /// </summary>
        private bool _isMaximized = false;

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
            DiagnosticRowDef.Height = GridLength.Auto;
            _viewModel.IsDiagnosticPanelExpanded = false;
            ExpandIcon.Text = "\uE70E";

            // 监听选区变化
            CipwReoGrid.CurrentWorksheet.SelectionRangeChanged += OnSelectionRangeChanged;
            CipwReoGrid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
        }

        /// <summary>
        /// 当切换工作表时重新绑定选区变化事件
        /// </summary>
        private void OnCurrentWorksheetChanged(object sender, System.EventArgs e)
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
        private void OnSelectionRangeChanged(object sender, RangeEventArgs e)
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
            if (!_userManuallyCollapsed && !_isMaximized && _viewModel.HasDiagnosticData)
            {
                if (!_viewModel.IsDiagnosticPanelExpanded)
                {
                    DiagnosticRowDef.Height = new GridLength(_normalPanelHeight);
                    _viewModel.IsDiagnosticPanelExpanded = true;
                    ExpandIcon.Text = "\uE70D";
                }
            }
        }

        /// <summary>
        /// 最大化/还原诊断面板
        /// 最大化：占据大部分空间（数据表格保留小部分可见）
        /// 还原：恢复之前的高度
        /// </summary>
        private void OnMaximizePanelClick(object sender, RoutedEventArgs e)
        {
            if (_isMaximized)
            {
                // 还原
                GridRowDef.Height = new GridLength(1, GridUnitType.Star);
                DiagnosticRowDef.Height = new GridLength(_normalPanelHeight);
                _isMaximized = false;
                _viewModel.IsDiagnosticMaximized = false;
                _viewModel.IsDiagnosticPanelExpanded = true;
                _userManuallyCollapsed = false;
                MaximizeIcon.Text = "\uE740";
                ExpandIcon.Text = "\uE70D";
            }
            else
            {
                // 最大化：先确保面板内容可见
                if (!_viewModel.IsDiagnosticPanelExpanded)
                {
                    _viewModel.IsDiagnosticPanelExpanded = true;
                }
                // 记录当前高度
                if (DiagnosticRowDef.Height.IsAbsolute && DiagnosticRowDef.Height.Value > 40)
                {
                    _normalPanelHeight = DiagnosticRowDef.Height.Value;
                }
                // 数据表格保留小部分，诊断面板占大部分（1:3 比例）
                GridRowDef.Height = new GridLength(1, GridUnitType.Star);
                DiagnosticRowDef.Height = new GridLength(3, GridUnitType.Star);
                _isMaximized = true;
                _viewModel.IsDiagnosticMaximized = true;
                _userManuallyCollapsed = false;
                MaximizeIcon.Text = "\uE73F";
                ExpandIcon.Text = "\uE70D";
            }
        }

        /// <summary>
        /// 展开/收缩诊断面板
        /// 收缩：面板缩为底部状态条（仅标题栏可见），并锁定不自动展开
        /// 展开：恢复正常高度，解除锁定
        /// </summary>
        private void OnToggleExpandClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsDiagnosticPanelExpanded)
            {
                // 用户手动收缩 → 锁定
                if (_isMaximized)
                {
                    // 先退出最大化
                    GridRowDef.Height = new GridLength(1, GridUnitType.Star);
                    _isMaximized = false;
                    _viewModel.IsDiagnosticMaximized = false;
                    MaximizeIcon.Text = "\uE740";
                }
                else if (DiagnosticRowDef.Height.IsAbsolute && DiagnosticRowDef.Height.Value > 40)
                {
                    _normalPanelHeight = DiagnosticRowDef.Height.Value;
                }
                DiagnosticRowDef.Height = GridLength.Auto;
                _viewModel.IsDiagnosticPanelExpanded = false;
                _userManuallyCollapsed = true;
                ExpandIcon.Text = "\uE70E";
            }
            else
            {
                // 用户手动展开 → 解除锁定
                DiagnosticRowDef.Height = new GridLength(_normalPanelHeight);
                _viewModel.IsDiagnosticPanelExpanded = true;
                _userManuallyCollapsed = false;
                ExpandIcon.Text = "\uE70D";
            }
        }

        /// <summary>
        /// 打开CIPW算法说明窗口
        /// </summary>
        private void OnHelpClick(object sender, RoutedEventArgs e)
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
        private void OnFe3FractionLostFocus(object sender, RoutedEventArgs e)
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
