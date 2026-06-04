using GeoChemistryNexus.ViewModels;
using System.Windows.Controls;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.Views
{
    public partial class DataPreprocessingPageView : Page
    {
        private static DataPreprocessingPageView _instance;
        private readonly DataPreprocessingPageViewModel _viewModel;
        private Worksheet _boundWorksheet;

        public DataPreprocessingPageView()
        {
            InitializeComponent();
            _viewModel = new DataPreprocessingPageViewModel();
            DataContext = _viewModel;

            _viewModel.InitializeWorksheet(DataPrepGrid);
            BindWorksheetEvents();
            DataPrepGrid.CurrentWorksheetChanged += DataPrepGrid_CurrentWorksheetChanged;
        }

        public static Page GetPage()
        {
            if (_instance == null)
            {
                _instance = new DataPreprocessingPageView();
            }

            return _instance;
        }

        private void DataPrepGrid_CurrentWorksheetChanged(object sender, System.EventArgs e)
        {
            BindWorksheetEvents();
            _viewModel.UpdateWorksheetSummary(DataPrepGrid.CurrentWorksheet);
        }

        private void BindWorksheetEvents()
        {
            if (_boundWorksheet != null)
            {
                _boundWorksheet.SelectionRangeChanged -= CurrentWorksheet_SelectionRangeChanged;
                _boundWorksheet.CellDataChanged -= CurrentWorksheet_CellDataChanged;
            }

            if (DataPrepGrid?.CurrentWorksheet == null)
            {
                return;
            }

            _boundWorksheet = DataPrepGrid.CurrentWorksheet;
            _boundWorksheet.SelectionRangeChanged += CurrentWorksheet_SelectionRangeChanged;
            _boundWorksheet.CellDataChanged += CurrentWorksheet_CellDataChanged;
        }

        private void CurrentWorksheet_SelectionRangeChanged(object sender, RangeEventArgs e)
        {
            _viewModel.UpdateWorksheetSummary(DataPrepGrid.CurrentWorksheet);
            _viewModel.UpdateSelectedCellState(DataPrepGrid.CurrentWorksheet, e.Range.Row, e.Range.Col);
        }

        private void CurrentWorksheet_CellDataChanged(object sender, CellEventArgs e)
        {
            _viewModel.UpdateWorksheetSummary(DataPrepGrid.CurrentWorksheet);
            _viewModel.UpdateSelectedCellState(DataPrepGrid.CurrentWorksheet, e.Cell.Position.Row, e.Cell.Position.Col);
        }
    }
}
