using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace GeoChemistryNexus.ViewModels
{
    public partial class AlkalinityCalculatorViewModel : ObservableObject
    {
        public const string SourceUrl = "https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/";

        [ObservableProperty]
        private double hclMolarity = 0.1;

        [ObservableProperty]
        private double sampleVolumeMl = 50;

        [ObservableProperty]
        private double hclVolumeMl = 1;

        [ObservableProperty]
        private string alkalinitySummary = string.Empty;

        public ObservableCollection<AlkalinityResultRowViewModel> Results { get; } = new();

        public AlkalinityCalculatorViewModel()
        {
            Recalculate();
        }

        partial void OnHclMolarityChanged(double value) => Recalculate();

        partial void OnSampleVolumeMlChanged(double value) => Recalculate();

        partial void OnHclVolumeMlChanged(double value) => Recalculate();

        [RelayCommand]
        private void Reset()
        {
            HclMolarity = 0.1;
            SampleVolumeMl = 50;
            HclVolumeMl = 1;
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
            var result = AlkalinityCalculatorService.Calculate(HclMolarity, SampleVolumeMl, HclVolumeMl);
            Results.Clear();

            if (!result.IsValid)
            {
                AlkalinitySummary = LanguageService.Instance["alkalinity_calc_invalid_input"]
                    ?? "请输入有效的滴定参数（水样体积须大于 0）。";
                return;
            }

            AlkalinitySummary = string.Format(
                CultureInfo.CurrentCulture,
                LanguageService.Instance["alkalinity_calc_summary"] ?? "碱度 = {0:G6} meq/L",
                result.AlkalinityMeqPerL);

            foreach (var row in result.Rows)
            {
                Results.Add(new AlkalinityResultRowViewModel(row));
            }
        }
    }

    public partial class AlkalinityResultRowViewModel : ObservableObject
    {
        public AlkalinityResultRowViewModel(AlkalinityResultRow row)
        {
            FormKey = row.FormKey;
            FormDisplay = ResolveFormDisplay(row.FormKey);
            GramsPerLiterText = Format(row.GramsPerLiter);
            MeqPerLiterText = Format(row.MeqPerLiter);
            MolarityText = Format(row.Molarity);
            PpmText = Format(row.Ppm);
        }

        public string FormKey { get; }
        public string FormDisplay { get; }
        public string GramsPerLiterText { get; }
        public string MeqPerLiterText { get; }
        public string MolarityText { get; }
        public string PpmText { get; }

        private static string ResolveFormDisplay(string formKey)
        {
            string resourceKey = formKey switch
            {
                "HCO3-" => "alkalinity_calc_as_hco3",
                "CO32-" => "alkalinity_calc_as_co3",
                "CaCO3" => "alkalinity_calc_as_caco3",
                "0.5(CaCO3)" => "alkalinity_calc_as_half_caco3",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(resourceKey))
                return formKey;

            return LanguageService.Instance[resourceKey] ?? formKey;
        }

        private static string Format(double value)
        {
            return value.ToString("G6", CultureInfo.CurrentCulture);
        }
    }
}
