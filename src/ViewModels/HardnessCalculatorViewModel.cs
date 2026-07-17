using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace GeoChemistryNexus.ViewModels
{
    public partial class HardnessCalculatorViewModel : ObservableObject
    {
        public const string SourceUrl = "https://muse.union.edu/hollochk/kurt-hollocher/geochemistry/";

        [ObservableProperty]
        private double mgPpm = 5;

        [ObservableProperty]
        private double caPpm = 23;

        [ObservableProperty]
        private string hardnessSummary = string.Empty;

        [ObservableProperty]
        private string classificationText = string.Empty;

        [ObservableProperty]
        private string equationText = string.Empty;

        public ObservableCollection<HardnessClassRowViewModel> ClassificationRows { get; } = new();

        public HardnessCalculatorViewModel()
        {
            BuildClassificationRows();
            Recalculate();
        }

        partial void OnMgPpmChanged(double value) => Recalculate();

        partial void OnCaPpmChanged(double value) => Recalculate();

        [RelayCommand]
        private void Reset()
        {
            MgPpm = 5;
            CaPpm = 23;
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
            var result = HardnessCalculatorService.Calculate(MgPpm, CaPpm);

            EquationText = string.Format(
                CultureInfo.CurrentCulture,
                LanguageService.Instance["hardness_calc_equation"]
                    ?? "Equation: (Mg × {0:0.00}) + (Ca × {1:0.00}) = hardness as CaCO3",
                HardnessCalculatorService.FactorMg,
                HardnessCalculatorService.FactorCa);

            if (!result.IsValid)
            {
                HardnessSummary = LanguageService.Instance["hardness_calc_invalid_input"]
                    ?? "Enter non-negative Mg and Ca concentrations.";
                ClassificationText = string.Empty;
                foreach (var row in ClassificationRows)
                    row.IsActive = false;
                return;
            }

            HardnessSummary = string.Format(
                CultureInfo.CurrentCulture,
                LanguageService.Instance["hardness_calc_summary"] ?? "Hardness = {0:0.0} ppm as CaCO3",
                result.HardnessAsCaCo3Ppm);

            ClassificationText = ResolveClassDisplay(result.ClassificationKey);

            foreach (var row in ClassificationRows)
            {
                row.IsActive = string.Equals(row.ClassKey, result.ClassificationKey, StringComparison.Ordinal);
            }
        }

        private void BuildClassificationRows()
        {
            ClassificationRows.Clear();
            ClassificationRows.Add(new HardnessClassRowViewModel("Soft", "hardness_calc_class_soft", "0–60 ppm"));
            ClassificationRows.Add(new HardnessClassRowViewModel("ModeratelyHard", "hardness_calc_class_moderately_hard", "61–120 ppm"));
            ClassificationRows.Add(new HardnessClassRowViewModel("Hard", "hardness_calc_class_hard", "121–180 ppm"));
            ClassificationRows.Add(new HardnessClassRowViewModel("VeryHard", "hardness_calc_class_very_hard", ">180 ppm"));
        }

        private static string ResolveClassDisplay(string classKey)
        {
            string resourceKey = classKey switch
            {
                "Soft" => "hardness_calc_class_soft",
                "ModeratelyHard" => "hardness_calc_class_moderately_hard",
                "Hard" => "hardness_calc_class_hard",
                "VeryHard" => "hardness_calc_class_very_hard",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(resourceKey))
                return classKey;

            return LanguageService.Instance[resourceKey] ?? classKey;
        }
    }

    public partial class HardnessClassRowViewModel : ObservableObject
    {
        public HardnessClassRowViewModel(string classKey, string displayResourceKey, string rangeText)
        {
            ClassKey = classKey;
            RangeText = rangeText;
            DisplayName = LanguageService.Instance[displayResourceKey] ?? classKey;
        }

        public string ClassKey { get; }
        public string DisplayName { get; }
        public string RangeText { get; }

        [ObservableProperty]
        private bool isActive;
    }
}
