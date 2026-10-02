using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models.PeriodicTable;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace GeoChemistryNexus.ViewModels
{
    public partial class PeriodicTableWidgetViewModel : ObservableObject
    {
        private readonly Dictionary<string, ElementCellViewModel> _cellsBySymbol = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _activeGroupSymbols = new(StringComparer.OrdinalIgnoreCase);

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string? activeGroupKey;

        [ObservableProperty]
        private ElementCellViewModel? selectedElement;

        [ObservableProperty]
        private string detailTitle = string.Empty;

        [ObservableProperty]
        private string detailSubtitle = string.Empty;

        [ObservableProperty]
        private string atomicNumberText = "—";

        [ObservableProperty]
        private string atomicWeightText = "—";

        [ObservableProperty]
        private string electronicConfigurationText = "—";

        [ObservableProperty]
        private string abundanceCrustText = "—";

        [ObservableProperty]
        private string abundanceSourceText = string.Empty;

        [ObservableProperty]
        private string goldschmidtText = "—";

        [ObservableProperty]
        private string geochemicalClassText = "—";

        [ObservableProperty]
        private string oxidationStatesText = "—";

        [ObservableProperty]
        private bool hasSelection;

        public ObservableCollection<ElementCellViewModel> Elements { get; } = new();
        public ObservableCollection<ElementGroupItemViewModel> Groups { get; } = new();
        public ObservableCollection<OxideRowViewModel> Oxides { get; } = new();

        public PeriodicTableWidgetViewModel()
        {
            LoadGroups();
            LoadElements();
            AbundanceSourceText = BuildAbundanceSourceText();
            ClearSelection();
        }

        partial void OnSearchTextChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrEmpty(ActiveGroupKey))
                ClearGroupFilterOnly();

            ApplySearchAndHighlight();
        }

        [RelayCommand]
        private void SelectElement(ElementCellViewModel? cell)
        {
            if (cell == null || cell.IsPlaceholder)
                return;

            foreach (var item in Elements)
                item.IsSelected = ReferenceEquals(item, cell);

            SelectedElement = cell;
            HasSelection = true;
            UpdateDetailPanel(cell.Record);
        }

        [RelayCommand]
        private void ToggleGroup(ElementGroupItemViewModel? group)
        {
            if (group == null)
                return;

            if (string.Equals(ActiveGroupKey, group.Key, StringComparison.OrdinalIgnoreCase))
            {
                ActiveGroupKey = null;
                _activeGroupSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var g in Groups)
                    g.IsActive = false;
            }
            else
            {
                ActiveGroupKey = group.Key;
                _activeGroupSymbols = ResolveGroupSymbols(group);
                foreach (var g in Groups)
                    g.IsActive = string.Equals(g.Key, group.Key, StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(SearchText))
                    SearchText = string.Empty;
            }

            ClearSelection();
            ApplySearchAndHighlight();
        }

        [RelayCommand]
        private void ClearGroup()
        {
            ClearGroupFilterOnly();
            ClearSelection();
            ApplySearchAndHighlight();
        }

        private void ClearGroupFilterOnly()
        {
            ActiveGroupKey = null;
            _activeGroupSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in Groups)
                g.IsActive = false;
        }

        private void ClearSelection()
        {
            foreach (var item in Elements)
                item.IsSelected = false;

            SelectedElement = null;
            HasSelection = false;

            var dash = "—";
            DetailTitle = LanguageService.Instance["periodic_table_hint_select"]
                ?? "Click an element to view properties";
            DetailSubtitle = string.Empty;
            AtomicNumberText = dash;
            AtomicWeightText = dash;
            ElectronicConfigurationText = dash;
            AbundanceCrustText = dash;
            GoldschmidtText = dash;
            GeochemicalClassText = dash;
            OxidationStatesText = dash;
            Oxides.Clear();
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        [RelayCommand]
        private void CopyAtomicWeight()
        {
            if (SelectedElement?.Record.AtomicWeight is not double aw)
            {
                MessageHelper.Info(LanguageService.Instance["periodic_table_copy_failed"] ?? "Nothing to copy.");
                return;
            }

            CopyText(FormatNumber(aw));
        }

        [RelayCommand]
        private void CopyOxide(OxideRowViewModel? oxide)
        {
            if (oxide == null)
            {
                MessageHelper.Info(LanguageService.Instance["periodic_table_copy_failed"] ?? "Nothing to copy.");
                return;
            }

            CopyText($"{oxide.Formula}\t{oxide.MolecularWeightText}");
        }

        [RelayCommand]
        private void CopyOxideWeight(OxideRowViewModel? oxide)
        {
            if (oxide == null)
            {
                MessageHelper.Info(LanguageService.Instance["periodic_table_copy_failed"] ?? "Nothing to copy.");
                return;
            }

            CopyText(oxide.MolecularWeightText);
        }

        [RelayCommand]
        private void FindNextFromSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            var matches = Elements
                .Where(e => !e.IsPlaceholder && e.MatchesSearch(SearchText))
                .OrderBy(e => e.AtomicNumber)
                .ToList();

            if (matches.Count == 0)
            {
                MessageHelper.Info(LanguageService.Instance["periodic_table_search_empty"] ?? "No matching element.");
                return;
            }

            int currentIndex = SelectedElement == null
                ? -1
                : matches.FindIndex(m => m.AtomicNumber == SelectedElement.AtomicNumber);
            var next = matches[(currentIndex + 1) % matches.Count];
            SelectElement(next);
        }

        private void LoadGroups()
        {
            Groups.Clear();
            foreach (var def in PeriodicTableDataService.Groups)
            {
                Groups.Add(new ElementGroupItemViewModel(def));
            }
        }

        private void LoadElements()
        {
            Elements.Clear();
            _cellsBySymbol.Clear();

            foreach (var record in PeriodicTableDataService.Elements)
            {
                var (row, col) = PeriodicTableDataService.GetDisplayPosition(record);
                if (row <= 0 || col <= 0)
                    continue;

                // 主表 period 1–7 → Grid 行 0–6；行 7 为分隔；镧系/锕系 → 8/9
                int gridRow = row <= 7 ? row - 1 : row;
                int gridColumn = col - 1;
                var cell = new ElementCellViewModel(record, gridRow, gridColumn);
                Elements.Add(cell);
                _cellsBySymbol[record.Symbol] = cell;
            }
        }

        private HashSet<string> ResolveGroupSymbols(ElementGroupItemViewModel group)
        {
            var def = PeriodicTableDataService.FindGroup(group.Key);
            if (def == null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!def.IsDerived)
                return new HashSet<string>(def.Symbols, StringComparer.OrdinalIgnoreCase);

            if (def.DerivedKind == ElementGroupDerivedKind.TransitionMetals)
            {
                // d 区过渡金属，排除镧系与锕系
                return Elements
                    .Where(e => !e.IsPlaceholder &&
                                e.AtomicNumber is (< 57 or > 71) and (< 89 or > 103) &&
                                (string.Equals(e.Record.Series, "Transition metals", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(e.Record.Block, "d", StringComparison.OrdinalIgnoreCase)))
                    .Select(e => e.Symbol)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            if (def.DerivedKind == ElementGroupDerivedKind.Goldschmidt &&
                !string.IsNullOrWhiteSpace(def.GoldschmidtKey))
            {
                return Elements
                    .Where(e => !e.IsPlaceholder && MatchesGoldschmidt(e.Record.GoldschmidtClass, def.GoldschmidtKey))
                    .Select(e => e.Symbol)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static bool MatchesGoldschmidt(string? raw, string key)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string normalized = PeriodicTableDataService.NormalizeGoldschmidtClass(raw);
            return string.Equals(normalized, key, StringComparison.OrdinalIgnoreCase) ||
                   (string.Equals(key, "lithophile", StringComparison.OrdinalIgnoreCase) &&
                    (string.Equals(raw, "litophile", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(raw, "lithophile", StringComparison.OrdinalIgnoreCase)));
        }

        private void ApplySearchAndHighlight()
        {
            bool hasGroup = _activeGroupSymbols.Count > 0;
            bool hasSearch = !hasGroup && !string.IsNullOrWhiteSpace(SearchText);

            foreach (var cell in Elements)
            {
                if (cell.IsPlaceholder)
                    continue;

                if (hasGroup)
                {
                    bool inGroup = _activeGroupSymbols.Contains(cell.Symbol);
                    cell.IsGroupHighlighted = inGroup;
                    cell.IsSearchMatch = false;
                    cell.IsDimmed = !inGroup;
                }
                else if (hasSearch)
                {
                    bool inSearch = cell.MatchesSearch(SearchText);
                    cell.IsGroupHighlighted = false;
                    cell.IsSearchMatch = inSearch;
                    cell.IsDimmed = !inSearch;
                }
                else
                {
                    cell.IsGroupHighlighted = false;
                    cell.IsSearchMatch = false;
                    cell.IsDimmed = false;
                }
            }
        }

        private void UpdateDetailPanel(ElementGeochemRecord record)
        {
            DetailTitle = $"{record.Symbol} — {record.Name}";
            DetailSubtitle = string.Format(
                CultureInfo.CurrentCulture,
                LanguageService.Instance["periodic_table_detail_subtitle"] ?? "Z = {0} · {1}-block",
                record.AtomicNumber,
                record.Block ?? "?");

            AtomicNumberText = record.AtomicNumber.ToString(CultureInfo.InvariantCulture);
            AtomicWeightText = record.AtomicWeight is double aw ? FormatNumber(aw) : "—";
            ElectronicConfigurationText = string.IsNullOrWhiteSpace(record.ElectronicConfiguration)
                ? "—"
                : record.ElectronicConfiguration;

            if (record.AbundanceCrustMgPerKg is double crust)
            {
                AbundanceCrustText = string.Format(
                    CultureInfo.CurrentCulture,
                    LanguageService.Instance["periodic_table_abundance_value"] ?? "{0} mg/kg (ppm)",
                    FormatNumber(crust));
            }
            else
            {
                AbundanceCrustText = LanguageService.Instance["periodic_table_na"] ?? "N/A";
            }

            AbundanceSourceText = BuildAbundanceSourceText();
            GoldschmidtText = string.IsNullOrWhiteSpace(record.GoldschmidtClass)
                ? (LanguageService.Instance["periodic_table_na"] ?? "N/A")
                : PeriodicTableDataService.NormalizeGoldschmidtClass(record.GoldschmidtClass);

            GeochemicalClassText = string.IsNullOrWhiteSpace(record.GeochemicalClass)
                ? (LanguageService.Instance["periodic_table_na"] ?? "N/A")
                : record.GeochemicalClass;

            OxidationStatesText = FormatOxidationStates(record);
            Oxides.Clear();
            foreach (var oxide in PeriodicTableDataService.BuildCommonOxides(record))
                Oxides.Add(new OxideRowViewModel(oxide));
        }

        private static string FormatOxidationStates(ElementGeochemRecord record)
        {
            if (record.OxidationStates == null || record.OxidationStates.Count == 0)
                return LanguageService.Instance["periodic_table_na"] ?? "N/A";

            var main = record.OxidationStates
                .Where(o => string.Equals(o.Category, "main", StringComparison.OrdinalIgnoreCase))
                .Select(o => FormatOxidation(o.State))
                .ToList();

            if (main.Count == 0)
            {
                return string.Join(", ",
                    record.OxidationStates.Select(o => FormatOxidation(o.State)));
            }

            string mainText = string.Join(", ", main);
            var extended = record.OxidationStates
                .Where(o => !string.Equals(o.Category, "main", StringComparison.OrdinalIgnoreCase))
                .Select(o => FormatOxidation(o.State))
                .ToList();

            if (extended.Count == 0)
                return mainText + " (main)";

            return $"{mainText} (main); {string.Join(", ", extended)}";
        }

        private static string FormatOxidation(int state)
        {
            if (state > 0) return $"+{state}";
            return state.ToString(CultureInfo.InvariantCulture);
        }

        private string BuildAbundanceSourceText()
        {
            var citation = PeriodicTableDataService.Catalog.Source?.AbundanceCrustCitation;
            if (string.IsNullOrWhiteSpace(citation))
                citation = "CRC Handbook/ Haynes (via mendeleev)";

            return string.Format(
                CultureInfo.CurrentCulture,
                LanguageService.Instance["periodic_table_abundance_source"] ?? "Data source: {0}",
                citation);
        }

        private void CopyText(string text)
        {
            try
            {
                Clipboard.SetText(text);
                MessageHelper.Success(LanguageService.Instance["periodic_table_copied"] ?? "Copied to clipboard.");
            }
            catch (Exception)
            {
                MessageHelper.Info(LanguageService.Instance["periodic_table_copy_failed"] ?? "Copy failed.");
            }
        }

        private static string FormatNumber(double value)
        {
            if (Math.Abs(value) >= 1000 || (Math.Abs(value) > 0 && Math.Abs(value) < 0.001))
                return value.ToString("G6", CultureInfo.InvariantCulture);
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }

    public partial class ElementCellViewModel : ObservableObject
    {
        private static readonly Brush DefaultBrush = CreateBrush("#F8FAFC");
        private static readonly Brush SelectedBrush = CreateBrush("#0078D4");
        private static readonly Brush SearchFillBrush = CreateBrush("#DCFCE7");
        private static readonly Brush HighlightFillBrush = CreateBrush("#FEF3C7");
        private static readonly Brush DimBrush = CreateBrush("#F1F5F9");
        private static readonly Brush DefaultBorder = CreateBrush("#D8E0EA");
        private static readonly Brush SelectedBorder = CreateBrush("#005A9E");
        private static readonly Brush SearchBorder = CreateBrush("#16A34A");
        private static readonly Brush HighlightBorder = CreateBrush("#D97706");
        private static readonly Brush DimBorder = CreateBrush("#E2E8F0");

        public ElementCellViewModel(ElementGeochemRecord record, int displayRow, int displayColumn)
        {
            Record = record;
            DisplayRow = displayRow;
            DisplayColumn = displayColumn;
            Symbol = record.Symbol;
            Name = record.Name;
            AtomicNumber = record.AtomicNumber;
            AtomicNumberText = record.AtomicNumber.ToString(CultureInfo.InvariantCulture);
            SeriesBrush = ResolveSeriesBrush(ResolveSeriesName(record));
            RefreshAppearance();
        }

        public ElementGeochemRecord Record { get; }
        public int DisplayRow { get; }
        public int DisplayColumn { get; }
        public string Symbol { get; }
        public string Name { get; }
        public int AtomicNumber { get; }
        public string AtomicNumberText { get; }
        public bool IsPlaceholder => false;
        public Brush SeriesBrush { get; }

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private bool isGroupHighlighted;

        [ObservableProperty]
        private bool isSearchMatch;

        [ObservableProperty]
        private bool isDimmed;

        [ObservableProperty]
        private Brush backgroundBrush = DefaultBrush;

        [ObservableProperty]
        private Brush foregroundBrush = Brushes.Black;

        [ObservableProperty]
        private Brush borderBrush = DefaultBorder;

        [ObservableProperty]
        private Thickness borderThickness = new(1);

        [ObservableProperty]
        private double opacity = 1.0;

        partial void OnIsSelectedChanged(bool value) => RefreshAppearance();
        partial void OnIsGroupHighlightedChanged(bool value) => RefreshAppearance();
        partial void OnIsSearchMatchChanged(bool value) => RefreshAppearance();
        partial void OnIsDimmedChanged(bool value) => RefreshAppearance();

        public bool MatchesSearch(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string q = query.Trim();
            if (int.TryParse(q, NumberStyles.Integer, CultureInfo.InvariantCulture, out int z) && z == AtomicNumber)
                return true;

            return Symbol.StartsWith(q, StringComparison.OrdinalIgnoreCase) ||
                   Name.StartsWith(q, StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshAppearance()
        {
            if (IsSelected)
            {
                BackgroundBrush = SelectedBrush;
                ForegroundBrush = Brushes.White;
                BorderBrush = SelectedBorder;
                BorderThickness = new Thickness(2);
                Opacity = 1.0;
                return;
            }

            if (IsSearchMatch)
            {
                BackgroundBrush = SearchFillBrush;
                ForegroundBrush = CreateBrush("#14532D");
                BorderBrush = SearchBorder;
                BorderThickness = new Thickness(2);
                Opacity = 1.0;
                return;
            }

            if (IsGroupHighlighted)
            {
                BackgroundBrush = HighlightFillBrush;
                ForegroundBrush = CreateBrush("#1F2937");
                BorderBrush = HighlightBorder;
                BorderThickness = new Thickness(2);
                Opacity = 1.0;
                return;
            }

            BackgroundBrush = IsDimmed ? DimBrush : SeriesBrush;
            ForegroundBrush = CreateBrush("#0F172A");
            BorderBrush = IsDimmed ? DimBorder : DefaultBorder;
            BorderThickness = new Thickness(1);
            Opacity = IsDimmed ? 0.32 : 1.0;
        }

        private static string? ResolveSeriesName(ElementGeochemRecord record)
        {
            if (record.AtomicNumber is >= 57 and <= 71)
                return "Lanthanides";
            if (record.AtomicNumber is >= 89 and <= 103)
                return "Actinides";
            return record.Series;
        }

        private static Brush ResolveSeriesBrush(string? series)
        {
            return series switch
            {
                "Nonmetals" => CreateBrush("#E7F6EC"),
                "Noble gases" => CreateBrush("#E8F1FB"),
                "Alkali metals" => CreateBrush("#FDECEC"),
                "Alkaline earth metals" => CreateBrush("#FFF4E5"),
                "Metalloids" => CreateBrush("#F3EAF8"),
                "Halogens" => CreateBrush("#E6F7F8"),
                "Poor metals" => CreateBrush("#EEF1F4"),
                "Transition metals" => CreateBrush("#E9EDF8"),
                "Lanthanides" => CreateBrush("#FCE8F0"),
                "Actinides" => CreateBrush("#F8EBE6"),
                _ => DefaultBrush
            };
        }

        private static Brush CreateBrush(string hex)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
            if (brush.CanFreeze)
                brush.Freeze();
            return brush;
        }
    }

    public partial class ElementGroupItemViewModel : ObservableObject
    {
        public ElementGroupItemViewModel(ElementGroupDefinition definition)
        {
            Key = definition.Key;
            TitleKey = definition.TitleKey;
            Title = LanguageService.Instance[definition.TitleKey] ?? definition.Key;
        }

        public string Key { get; }
        public string TitleKey { get; }
        public string Title { get; }

        [ObservableProperty]
        private bool isActive;
    }

    public partial class OxideRowViewModel : ObservableObject
    {
        public OxideRowViewModel(OxideFormulaInfo info)
        {
            Formula = info.Formula;
            MolecularWeightText = info.MolecularWeight.ToString("0.####", CultureInfo.InvariantCulture);
            var label = LanguageService.Instance["periodic_table_mw_label"] ?? "MW = ";
            MolecularWeightDisplay = label + MolecularWeightText;
        }

        public string Formula { get; }
        public string MolecularWeightText { get; }
        public string MolecularWeightDisplay { get; }
    }
}
