using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using ScottPlot;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class BlackBodyRadiationCalculatorWidget : UserControl
    {
        private BlackBodyRadiationCalculatorViewModel? _viewModel;

        public BlackBodyRadiationCalculatorWidget()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
            AttachViewModel(DataContext as BlackBodyRadiationCalculatorViewModel);
            RefreshPlot();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            LanguageService.Instance.PropertyChanged -= OnLanguageChanged;
            DetachViewModel();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(e.NewValue as BlackBodyRadiationCalculatorViewModel);
            if (IsLoaded)
                RefreshPlot();
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            _viewModel?.RefreshLocalizedTexts();
            RefreshPlot();
        }

        private void AttachViewModel(BlackBodyRadiationCalculatorViewModel? vm)
        {
            if (ReferenceEquals(_viewModel, vm))
                return;

            DetachViewModel();
            _viewModel = vm;
            if (_viewModel != null)
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void DetachViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel = null;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BlackBodyRadiationCalculatorViewModel.SpectrumVersion))
                RefreshPlot();
        }

        private void RefreshPlot()
        {
            if (_viewModel == null || SpectrumPlot == null)
                return;

            var plot = SpectrumPlot.Plot;
            plot.Clear();

            var xs = _viewModel.WavelengthUm;
            var ys = _viewModel.NormalizedPowerPercent;
            if (xs.Length > 0 && ys.Length == xs.Length)
            {
                var scatter = plot.Add.ScatterLine(xs, ys);
                scatter.Color = ScottPlot.Color.FromHex("#0078D4");
                scatter.LineWidth = 2;

                double peakX = _viewModel.SpectrumPeakWavelengthUm;
                if (peakX > 0)
                {
                    var vLine = plot.Add.VerticalLine(peakX);
                    vLine.Color = ScottPlot.Color.FromHex("#C42B1C");
                    vLine.LineWidth = 1.5f;
                    vLine.LinePattern = LinePattern.Dashed;
                }
            }

            string xLabel = LanguageService.Instance["black_body_radiation_calc_axis_wavelength"]
                ?? "Wavelength (μm)";
            string yLabel = LanguageService.Instance["black_body_radiation_calc_axis_normalized"]
                ?? "Normalized power (%)";
            string fontName = Fonts.Detect(xLabel + yLabel);

            plot.Axes.Bottom.Label.Text = xLabel;
            plot.Axes.Bottom.Label.FontName = fontName;
            plot.Axes.Bottom.TickLabelStyle.FontName = fontName;

            plot.Axes.Left.Label.Text = yLabel;
            plot.Axes.Left.Label.FontName = fontName;
            plot.Axes.Left.TickLabelStyle.FontName = fontName;

            if (xs.Length > 0)
                plot.Axes.SetLimits(0, xs[^1], 0, 110);
            else
                plot.Axes.SetLimits(0, 10, 0, 110);

            SpectrumPlot.Refresh();
        }
    }
}
