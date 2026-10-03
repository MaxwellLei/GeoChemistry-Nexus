using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// NiggliPageView.xaml 的交互逻辑
    /// </summary>
    public partial class NiggliPageView : Page
    {
        private static NiggliPageView? _instance;
        private readonly NiggliPageViewModel _viewModel;

        private double _normalPanelHeight = 200;
        private const double MinDetailPanelHeight = 80;
        private bool _userManuallyCollapsed = false;
        private Worksheet? _boundWorksheet;

        public NiggliPageView()
        {
            InitializeComponent();
            _viewModel = new NiggliPageViewModel();
            this.DataContext = _viewModel;

            // 初始化工作表
            _viewModel.InitializeWorksheet(NiggliReoGrid);

            // 默认状态：收缩为底部状态条
            CollapseDiagnosticPanel(saveHeight: false);

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

        private void OnSelectionRangeChanged(object? sender, RangeEventArgs e)
        {
            if (_viewModel == null) return;

            var worksheet = NiggliReoGrid.CurrentWorksheet;
            if (worksheet == null) return;

            int row = worksheet.SelectionRange.Row;
            if (row < 1) return;

            _viewModel.OnRowSelected(worksheet, row);

            if (!_userManuallyCollapsed && _viewModel.HasDiagnosticData && !_viewModel.IsDiagnosticPanelExpanded)
            {
                ExpandDiagnosticPanel();
            }
        }

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

        private void OnDetailSplitterDragCompleted(object? sender, DragCompletedEventArgs e)
        {
            RememberDetailPanelHeight();
        }

        private void ExpandDiagnosticPanel()
        {
            DiagnosticRowDef.MinHeight = MinDetailPanelHeight;
            DiagnosticRowDef.Height = new GridLength(_normalPanelHeight);
            DetailSplitter.Visibility = Visibility.Visible;
            _viewModel.IsDiagnosticPanelExpanded = true;
            ExpandIcon.Text = "\uE70D";
        }

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

        private void RememberDetailPanelHeight()
        {
            if (DiagnosticRowDef.Height.IsAbsolute && DiagnosticRowDef.Height.Value >= MinDetailPanelHeight)
            {
                _normalPanelHeight = DiagnosticRowDef.Height.Value;
            }
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
