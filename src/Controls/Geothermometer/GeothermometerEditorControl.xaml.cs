using GeoChemistryNexus.Controls.GeothermometerEditorPanels;
using GeoChemistryNexus.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GeoChemistryNexus.Controls
{
    public partial class GeothermometerEditorControl : UserControl
    {
        private GeothermometerEditorViewModel? _viewModel;
        private bool _suppressNavSync;

        public GeothermometerEditorControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public GeoTBasicInfoPanel BasicInfoPanel => BasicPanel;

        public GeoTHelpDocPanel HelpDocPanel => HelpPanel;

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            _viewModel = e.NewValue as GeothermometerEditorViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                SyncNavFromStep(_viewModel.CurrentStep);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeothermometerEditorViewModel.CurrentStep) && _viewModel != null)
                SyncNavFromStep(_viewModel.CurrentStep);
        }

        private void NavSection_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressNavSync) return;
            if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int step) && _viewModel != null)
                _viewModel.NavigateToSectionCommand.Execute(step);
        }

        private void SyncNavFromStep(int step)
        {
            _suppressNavSync = true;
            switch (step)
            {
                case 0:
                    NavBasic.IsChecked = true;
                    break;
                case 1:
                    NavScript.IsChecked = true;
                    break;
                case 2:
                    NavHelp.IsChecked = true;
                    break;
            }
            _suppressNavSync = false;
        }
    }
}
