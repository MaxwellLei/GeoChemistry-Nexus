using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services;
using System;
using System.Diagnostics;
using System.Globalization;

namespace GeoChemistryNexus.ViewModels
{
    public partial class BlackBodyRadiationCalculatorViewModel : ObservableObject
    {
        public const string SourceUrl = "https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/";

        [ObservableProperty]
        private double temperatureKelvin = BlackBodyRadiationCalculatorService.DefaultTemperatureK;

        [ObservableProperty]
        private string stefanBoltzmannText = string.Empty;

        [ObservableProperty]
        private string wienText = string.Empty;

        [ObservableProperty]
        private string spectrumPeakText = string.Empty;

        [ObservableProperty]
        private string statusText = string.Empty;

        /// <summary>光谱重算版本号，供视图订阅后刷新 ScottPlot。</summary>
        [ObservableProperty]
        private int spectrumVersion;

        public double[] WavelengthUm { get; private set; } = Array.Empty<double>();
        public double[] NormalizedPowerPercent { get; private set; } = Array.Empty<double>();
        public double PeakWavelengthUm { get; private set; }
        public double SpectrumPeakWavelengthUm { get; private set; }

        private BlackBodyRadiationResult? _lastResult;

        public BlackBodyRadiationCalculatorViewModel()
        {
            Recalculate();
        }

        partial void OnTemperatureKelvinChanged(double value) => Recalculate();

        /// <summary>语言切换后刷新结果文案（不重算光谱网格）。</summary>
        public void RefreshLocalizedTexts()
        {
            ApplyLocalizedTexts(_lastResult);
            SpectrumVersion++;
        }

        [RelayCommand]
        private void Reset()
        {
            TemperatureKelvin = BlackBodyRadiationCalculatorService.DefaultTemperatureK;
        }

        [RelayCommand]
        private void OpenSource()
        {
            try
            {
                Process.Start(new ProcessStartInfo(SourceUrl) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // 忽略无法打开浏览器的情况
            }
        }

        private void Recalculate()
        {
            var result = BlackBodyRadiationCalculatorService.Calculate(TemperatureKelvin);
            _lastResult = result.IsValid ? result : null;

            if (!result.IsValid)
            {
                ApplyLocalizedTexts(null);
                WavelengthUm = Array.Empty<double>();
                NormalizedPowerPercent = Array.Empty<double>();
                PeakWavelengthUm = 0;
                SpectrumPeakWavelengthUm = 0;
                SpectrumVersion++;
                return;
            }

            int n = result.Spectrum.Count;
            var xs = new double[n];
            var ys = new double[n];
            for (int i = 0; i < n; i++)
            {
                xs[i] = result.Spectrum[i].WavelengthUm;
                ys[i] = result.Spectrum[i].NormalizedPowerPercent;
            }

            WavelengthUm = xs;
            NormalizedPowerPercent = ys;
            PeakWavelengthUm = result.PeakWavelengthUm;
            SpectrumPeakWavelengthUm = result.SpectrumPeakWavelengthUm;
            ApplyLocalizedTexts(result);
            SpectrumVersion++;
        }

        private void ApplyLocalizedTexts(BlackBodyRadiationResult? result)
        {
            if (result == null || !result.IsValid)
            {
                StatusText = LanguageService.Instance["black_body_radiation_calc_invalid_input"]
                    ?? "Enter a temperature greater than 0 K.";
                StefanBoltzmannText = string.Empty;
                WienText = string.Empty;
                SpectrumPeakText = string.Empty;
                return;
            }

            StatusText = string.Empty;

            StefanBoltzmannText = string.Format(
                CultureInfo.CurrentCulture,
                LanguageService.Instance["black_body_radiation_calc_stefan_result"]
                    ?? "Total radiant power = {0:G6} W/m²",
                result.TotalRadiantPowerWm2);

            WienText = string.Format(
                CultureInfo.CurrentCulture,
                LanguageService.Instance["black_body_radiation_calc_wien_result"]
                    ?? "Peak wavelength λ_max = {0:G6} μm ({1:G6} nm)",
                result.PeakWavelengthUm,
                result.PeakWavelengthNm);

            SpectrumPeakText = string.Format(
                CultureInfo.CurrentCulture,
                LanguageService.Instance["black_body_radiation_calc_spectrum_peak"]
                    ?? "Spectrum peak (grid) = {0:G6} μm ({1:G6} nm)",
                result.SpectrumPeakWavelengthUm,
                result.SpectrumPeakWavelengthNm);
        }
    }
}
