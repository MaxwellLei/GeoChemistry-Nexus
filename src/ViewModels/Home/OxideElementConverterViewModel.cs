using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace GeoChemistryNexus.ViewModels
{
    public partial class OxideElementConverterViewModel : ObservableObject
    {
        // ────────────────── 条目列表 ──────────────────
        public ObservableCollection<OxideElementEntry> Entries { get; } = new(
            OxideElementConverterService.Entries);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InputLabel))]
        [NotifyPropertyChangedFor(nameof(OutputLabel))]
        private OxideElementEntry? selectedEntry;

        partial void OnSelectedEntryChanged(OxideElementEntry? value) => Recalculate();

        // ────────────────── 换算方向 ──────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InputLabel))]
        [NotifyPropertyChangedFor(nameof(OutputLabel))]
        [NotifyPropertyChangedFor(nameof(IsElemToOxide))]
        [NotifyPropertyChangedFor(nameof(IsOxideToElem))]
        private ConversionMode conversionMode = ConversionMode.ElementToOxide;

        partial void OnConversionModeChanged(ConversionMode value) => Recalculate();

        public bool IsElemToOxide
        {
            get => ConversionMode == ConversionMode.ElementToOxide;
            set
            {
                if (value) ConversionMode = ConversionMode.ElementToOxide;
            }
        }

        public bool IsOxideToElem
        {
            get => ConversionMode == ConversionMode.OxideToElement;
            set
            {
                if (value) ConversionMode = ConversionMode.OxideToElement;
            }
        }

        // ────────────────── 输入 ──────────────────
        [ObservableProperty]
        private double inputValue = 1.0;

        partial void OnInputValueChanged(double value) => Recalculate();

        // ────────────────── 单位选择 ──────────────────
        /// <summary>true = wt%，false = ppm</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UnitLabel))]
        [NotifyPropertyChangedFor(nameof(IsPpm))]
        private bool isWtPercent = true;

        partial void OnIsWtPercentChanged(bool value) => Recalculate();

        public bool IsPpm
        {
            get => !IsWtPercent;
            set
            {
                if (value) IsWtPercent = false;
                else IsWtPercent = true;
            }
        }

        public string UnitLabel => IsWtPercent
            ? (LanguageService.Instance["oxide_element_conv_unit_wt_pct"] ?? "wt%")
            : (LanguageService.Instance["oxide_element_conv_unit_ppm"] ?? "ppm");

        // ────────────────── 输出区 ──────────────────
        [ObservableProperty]
        private string resultText = string.Empty;

        [ObservableProperty]
        private string factorText = string.Empty;

        [ObservableProperty]
        private string atomicWeightText = string.Empty;

        [ObservableProperty]
        private string oxideMwText = string.Empty;

        [ObservableProperty]
        private string formulaText = string.Empty;

        [ObservableProperty]
        private bool isResultValid;

        // 输入/输出框标签（随方向切换）
        public string InputLabel => ConversionMode == ConversionMode.ElementToOxide
            ? (SelectedEntry?.ElementSymbol ?? LanguageService.Instance["oxide_element_conv_col_element"] ?? "Element")
            : (SelectedEntry?.OxideFormula  ?? LanguageService.Instance["oxide_element_conv_col_oxide"] ?? "Oxide");

        public string OutputLabel => ConversionMode == ConversionMode.ElementToOxide
            ? (SelectedEntry?.OxideFormula  ?? LanguageService.Instance["oxide_element_conv_col_oxide"] ?? "Oxide")
            : (SelectedEntry?.ElementSymbol ?? LanguageService.Instance["oxide_element_conv_col_element"] ?? "Element");

        // ────────────────── 参考表 ──────────────────
        public ObservableCollection<OxideElementRefRowViewModel> ReferenceRows { get; } = new();

        // ────────────────── 构造 ──────────────────
        public OxideElementConverterViewModel()
        {
            // 默认选中 SiO₂（第一项）
            SelectedEntry = Entries.FirstOrDefault();
            BuildReferenceRows();
            Recalculate();
        }

        // ────────────────── 命令 ──────────────────
        [RelayCommand]
        private void Reset()
        {
            InputValue      = 1.0;
            ConversionMode  = ConversionMode.ElementToOxide;
            IsWtPercent     = true;
            SelectedEntry   = Entries.FirstOrDefault();
        }

        [RelayCommand]
        private void SwapMode()
        {
            ConversionMode = ConversionMode == ConversionMode.ElementToOxide
                ? ConversionMode.OxideToElement
                : ConversionMode.ElementToOxide;
        }

        // ────────────────── 核心计算 ──────────────────
        private void Recalculate()
        {
            if (SelectedEntry == null || InputValue < 0)
            {
                IsResultValid   = false;
                ResultText      = LanguageService.Instance["oxide_element_conv_invalid_input"]
                                  ?? "Please enter a non-negative value.";
                FactorText      = string.Empty;
                AtomicWeightText = string.Empty;
                OxideMwText     = string.Empty;
                FormulaText     = string.Empty;
                return;
            }

            var result = OxideElementConverterService.Calculate(SelectedEntry, InputValue, ConversionMode);

            if (!result.IsValid)
            {
                IsResultValid = false;
                ResultText    = LanguageService.Instance["oxide_element_conv_invalid_input"]
                                ?? "Please enter a non-negative value.";
                return;
            }

            IsResultValid    = true;
            double output    = result.OutputValue;

            // 如果用户选择 ppm，输入和输出都按 ×10000 处理
            double displayOut = IsWtPercent ? output : output * 10000.0;
            ResultText        = displayOut.ToString("G6", CultureInfo.CurrentCulture);

            FactorText        = result.Factor.ToString("G6", CultureInfo.CurrentCulture);
            AtomicWeightText  = SelectedEntry.AtomicWeight.ToString("G5", CultureInfo.CurrentCulture);
            OxideMwText       = SelectedEntry.OxideMolarMass.ToString("G6", CultureInfo.CurrentCulture);
            FormulaText       = ConversionMode == ConversionMode.ElementToOxide
                ? $"{SelectedEntry.ElementSymbol} → {SelectedEntry.OxideFormula}"
                : $"{SelectedEntry.OxideFormula} → {SelectedEntry.ElementSymbol}";

            // 同步参考表中当前选中行高亮
            foreach (var row in ReferenceRows)
                row.IsActive = ReferenceEquals(row.Entry, SelectedEntry);
        }

        private void BuildReferenceRows()
        {
            ReferenceRows.Clear();
            foreach (var entry in OxideElementConverterService.Entries)
                ReferenceRows.Add(new OxideElementRefRowViewModel(entry));
        }
    }

    // ─────────────── 参考表行 VM ───────────────
    public partial class OxideElementRefRowViewModel : ObservableObject
    {
        public OxideElementEntry Entry { get; }

        public string ElementSymbol    => Entry.ElementSymbol;
        public string OxideFormula     => Entry.OxideFormula;
        public string AtomicWeightText => Entry.AtomicWeight.ToString("G5",  CultureInfo.CurrentCulture);
        public string ToOxideText      => Entry.FactorElemToOxide.ToString("G5", CultureInfo.CurrentCulture);
        public string ToElemText       => Entry.FactorOxideToElem.ToString("G5", CultureInfo.CurrentCulture);

        [ObservableProperty]
        private bool isActive;

        public OxideElementRefRowViewModel(OxideElementEntry entry)
        {
            Entry = entry;
        }
    }
}
