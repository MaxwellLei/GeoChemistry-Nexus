using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels;

public partial class EpmaCalculationViewModel
{
    /// <summary>Result values are always shown with this many decimals.</summary>
    private const int DisplayDecimals = 3;

    [RelayCommand] private void ResetWorksheetLayout() => _adapter?.ResetLayout();

    #region Right-hand inspector (same ReoGridTableHost inspector and step list as the geothermometers)

    /// <summary>Whether the worksheet's right-hand inspector is open; two-way bound to <c>IsInspectorOpen</c>. Closed by default.</summary>
    [ObservableProperty] private bool _isInspectorOpen;
    /// <summary>The selected row has a calculated result, so the inspector can show its details.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportInspectorDetailsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyInspectorDetailsCommand))]
    private bool _hasInspectorData;
    [ObservableProperty] private string _selectedRowInfo = "";
    [ObservableProperty] private bool _isSelectedRowStale;
    /// <summary>The package's <c>calculateDetailed</c> is running for the selected row.</summary>
    [ObservableProperty] private bool _isInspectorLoading;

    /// <summary>
    /// Lines of the selected row: first the diagnostics that explain its status dot (built from the QC flags, always shown),
    /// then the package's own <c>calculateDetailed</c> lines, or the result columns when the package has none.
    /// </summary>
    public ObservableCollection<CalculationStep> InspectorItems { get; } = new();

    // The two blocks keep their own group-collapse state, so lines of the package that come before its first heading
    // are not swallowed by a collapsed diagnostics group.
    private List<CalculationStep> _diagnosticSteps = new();
    private List<CalculationStep> _detailSteps = new();

    // What the lines currently shown (or being loaded) belong to; selecting the same thing again changes nothing.
    private EpmaScriptRun? _detailRun;
    private int _detailRow = -1, _detailDecimals = -1;
    private string _detailLanguage = "";
    private CancellationTokenSource? _detailCts;
    private readonly Dictionary<(int Row, int Decimals, string Language), List<CalculationStep>> _detailCache = new();
    private const int DetailCacheLimit = 64;
    private const string ErrorBackground = "#FFF5F5";
    private const string OkBackground = "#F1F8F1";

    /// <summary>Exports the inspector lines of the selected row as CSV (same button as the geothermometer inspector).</summary>
    [RelayCommand(CanExecute = nameof(HasInspectorData))]
    private Task ExportInspectorDetailsAsync() => InspectorDetailsExporter.ExportStepsCsvAsync(InspectorItems, SelectedRowInfo);

    /// <summary>Copies the inspector lines of the selected row to the clipboard.</summary>
    [RelayCommand(CanExecute = nameof(HasInspectorData))]
    private void CopyInspectorDetails() => InspectorDetailsExporter.CopyStepsToClipboard(InspectorItems);

    /// <summary>Folds / unfolds a group when its heading is clicked (same command name the shared step list binds to).</summary>
    [RelayCommand]
    private void ToggleCalculationGroup(CalculationStep? separator)
    {
        if (separator is not { IsSeparator: true }) return;
        separator.IsCollapsed = !separator.IsCollapsed;
        CalculationStepHelper.ApplyGroupVisibility(_diagnosticSteps);
        CalculationStepHelper.ApplyGroupVisibility(_detailSteps);
    }

    public event EventHandler? RowStatusChanged;

    /// <summary>Row-header dot for a worksheet row (zero-based). Only visible rows are asked, so this must stay cheap.</summary>
    public WorksheetRowStatus GetRowStatus(int row)
    {
        int inputRow = row + 1;
        if (_rowsByInput.TryGetValue(inputRow, out var result) && result.Result.Status != "Empty")
        {
            if (_stale.Contains(inputRow)) return WorksheetRowStatus.Stale;
            if (result.Result.Status != "Computed" || result.HighestSeverity == "Error") return WorksheetRowStatus.Error;
            return result.HighestSeverity == "Warning" ? WorksheetRowStatus.Warning : WorksheetRowStatus.Ok;
        }
        if (_adapter?.HasInput(row) != true) return WorksheetRowStatus.None;
        // Data typed after the last run has no result yet; before any run it is simply not calculated.
        return Run is null ? WorksheetRowStatus.NotCalculated : WorksheetRowStatus.Stale;
    }

    /// <summary>Forgets the lines of the previous run; called whenever the results are replaced or cleared.</summary>
    private void ResetInspectorDetails()
    {
        CancelDetailLoad();
        _detailCache.Clear();
        _detailRun = null;
        _detailRow = -1;
    }

    private void CancelDetailLoad()
    {
        _detailCts?.Cancel();
        _detailCts = null;
        IsInspectorLoading = false;
    }

    private void RefreshInspector()
    {
        var run = Run;
        if (run is null || !_rowsByInput.TryGetValue(_selectedRow, out var row) || row.Result.Status == "Empty")
        {
            CancelDetailLoad();
            _detailRun = null; _detailRow = -1;
            _diagnosticSteps = new(); _detailSteps = new();
            InspectorItems.Clear();
            HasInspectorData = false; IsSelectedRowStale = false; SelectedRowInfo = "";
            return;
        }
        HasInspectorData = true;
        IsSelectedRowStale = _stale.Contains(_selectedRow);
        string sample = SampleName(run, row);
        SelectedRowInfo = sample.Length > 0 ? sample : EpmaLocalizer.Format("epma_column_InputRow") + " " + _selectedRow;

        // Typing in the sheet also lands here; only another row, run or language needs new lines.
        int decimals = Math.Clamp(DisplayDecimals, 0, 10);
        string language = LanguageService.CurrentLanguage;
        if (ReferenceEquals(_detailRun, run) && _detailRow == row.InputRow && _detailDecimals == decimals && _detailLanguage == language) return;
        CancelDetailLoad();
        _detailRun = run; _detailRow = row.InputRow; _detailDecimals = decimals; _detailLanguage = language;

        _diagnosticSteps = BuildDiagnostics(row);
        _detailSteps = new();
        if (row.Result.Status == "Computed")
        {
            var key = (row.InputRow, decimals, language);
            if (_detailCache.TryGetValue(key, out var cached)) _detailSteps = cached;
            else if (!EpmaScriptRuntime.MayDescribe(run.Package)) _detailSteps = Remember(key, BuildResultSteps(run, row, decimals));
            else _ = LoadDetailsAsync(run, row, decimals, language);
        }
        PublishInspectorItems();
    }

    private void PublishInspectorItems()
    {
        InspectorItems.Clear();
        foreach (var step in _diagnosticSteps) InspectorItems.Add(step);
        foreach (var step in _detailSteps) InspectorItems.Add(step);
    }

    private List<CalculationStep> Remember((int Row, int Decimals, string Language) key, List<CalculationStep> steps)
    {
        if (_detailCache.Count >= DetailCacheLimit) _detailCache.Clear();
        _detailCache[key] = steps;
        return steps;
    }

    /// <summary>Runs the package's <c>calculateDetailed</c> for one row off the UI thread; a quick row change cancels the previous run.</summary>
    private async Task LoadDetailsAsync(EpmaScriptRun run, EpmaScriptRow row, int decimals, string language)
    {
        var cts = new CancellationTokenSource();
        _detailCts = cts;
        var token = cts.Token;
        IsInspectorLoading = true;
        List<CalculationStep>? steps = null;
        bool failed = false;
        try
        {
            // Let the loading bar render once before the script runs.
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);
            token.ThrowIfCancellationRequested();
            steps = await Task.Run(() => EpmaScriptRuntime.Describe(run, row, decimals, token), token) ?? BuildResultSteps(run, row, decimals);
        }
        catch (OperationCanceledException) { steps = null; }
        catch (Exception ex) { failed = true; steps = new() { ErrorStep(ex.Message) }; }

        bool current = !token.IsCancellationRequested && ReferenceEquals(Run, run)
            && _detailRow == row.InputRow && _detailDecimals == decimals && _detailLanguage == language;
        if (ReferenceEquals(_detailCts, cts)) { IsInspectorLoading = false; _detailCts = null; }
        cts.Dispose();
        if (steps is null || !current) return;

        _detailSteps = failed ? steps : Remember((row.InputRow, decimals, language), steps);
        PublishInspectorItems();
    }

    private static CalculationStep ErrorStep(string message) => new()
    {
        Name = SeverityGlyph("Error") + " " + EpmaLocalizer.Format("epma_calculation_failed"),
        Description = message,
        BackgroundColor = ErrorBackground,
    };

    /// <summary>
    /// The lines that explain the row's status dot, one heading per severity. This is the only part the program writes itself;
    /// code / source of a flag stay in the exported record.
    /// </summary>
    private static List<CalculationStep> BuildDiagnostics(EpmaScriptRow row)
    {
        var flags = row.Flags.ToArray();
        var groups = new (string Severity, List<CalculationStep> Lines)[] { ("Error", new()), ("Warning", new()), ("Info", new()) };
        foreach (var flag in flags)
            groups.First(g => g.Severity == (flag.Severity is "Error" or "Warning" ? flag.Severity : "Info")).Lines.Add(FlagStep(flag));
        if (row.Result.Status == "Skipped" && flags.Length == 0)
            groups[0].Lines.Add(new CalculationStep
            {
                Name = SeverityGlyph("Error") + " " + EpmaLocalizer.Format("epma_qc_skipped_row"),
                BackgroundColor = ErrorBackground,
            });

        var steps = new List<CalculationStep>();
        foreach (var (severity, lines) in groups)
        {
            if (lines.Count == 0) continue;
            steps.Add(new CalculationStep { Name = SeverityLabel(severity), IsSeparator = true });
            steps.AddRange(lines);
        }
        if (steps.Count == 0)
        {
            steps.Add(new CalculationStep { Name = EpmaLocalizer.Format("sheet_group_diagnostics"), IsSeparator = true });
            steps.Add(new CalculationStep { Name = SeverityGlyph("Ok") + " " + EpmaLocalizer.Format("epma_qc_clear"), BackgroundColor = OkBackground });
        }
        CalculationStepHelper.ApplyGroupVisibility(steps);
        return steps;
    }

    private static CalculationStep FlagStep(EpmaScriptFlag flag)
    {
        var step = new CalculationStep { Name = SeverityGlyph(flag.Severity) + " " + flag.Text, Description = FlagDetails(flag) };
        if (flag.Severity == "Error") step.BackgroundColor = ErrorBackground;
        else if (flag.Severity == "Warning") step.IsHighlight = true;
        return step;
    }

    /// <summary>Target / actual / expected value of a flag, when the script supplied them.</summary>
    private static string FlagDetails(EpmaScriptFlag flag)
    {
        var details = new List<string>();
        if (flag.Target.Length > 0 && flag.Target != "Row") details.Add(EpmaLocalizer.Format("epma_column_Target") + " " + flag.Target);
        if (flag.ActualText.Length > 0) details.Add(EpmaLocalizer.Format("epma_column_Actual") + " " + flag.ActualText);
        if (flag.ExpectedText.Length > 0) details.Add(EpmaLocalizer.Format("epma_column_Expectation") + " " + flag.ExpectedText);
        return string.Join("  ·  ", details);
    }

    /// <summary>
    /// What the inspector shows for a package without <c>calculateDetailed</c> (like the geothermometers' single T line):
    /// the result columns of the row, under their column group headings.
    /// </summary>
    private static List<CalculationStep> BuildResultSteps(EpmaScriptRun run, EpmaScriptRow row, int decimals)
    {
        var steps = new List<CalculationStep>();
        string group = "";
        foreach (var field in run.Fields)
        {
            string heading = field.DisplayGroup;
            if (heading.Length > 0 && heading != group) steps.Add(new CalculationStep { Name = heading, IsSeparator = true });
            group = heading;

            row.Result.Values.TryGetValue(field.Id, out var value);
            string text = value.ValueKind switch
            {
                JsonValueKind.Undefined or JsonValueKind.Null => "—",
                JsonValueKind.Number => EpmaResultPresentation.Number(value.GetDouble(), decimals),
                _ => EpmaScriptText.Scalar(value),
            };
            string unit = field.DisplayUnit;
            steps.Add(new CalculationStep
            {
                Name = field.DisplayLabel,
                Value = unit.Length > 0 && text != "—" ? text + " " + unit : text,
                Description = field.DisplayDescription,
            });
        }
        CalculationStepHelper.ApplyGroupVisibility(steps);
        return steps;
    }

    private static string SampleName(EpmaScriptRun run, EpmaScriptRow row)
    {
        var sampleInput = run.Package.Manifest.Inputs.FirstOrDefault(i => i.Id == EpmaScriptPackage.SampleInputId);
        return sampleInput is not null && row.Inputs.TryGetValue(sampleInput.Id, out var cell) ? cell.Raw.Trim() : "";
    }

    private static string SeverityGlyph(string severity) => severity switch
    {
        "Error" => "✕", "Warning" => "⚠", "Ok" => "✓", _ => "ℹ",
    };

    private static string SeverityLabel(string severity) => severity switch
    {
        "Error" => EpmaLocalizer.Format("epma_errors"),
        "Warning" => EpmaLocalizer.Format("epma_warnings"),
        "Info" => EpmaLocalizer.Format("epma_information"),
        _ => severity,
    };

    #endregion

    [ObservableProperty] private string _selectedCellAddress = "--";
    [ObservableProperty] private string _selectedCellContent = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedCellReadOnly))]
    private bool _isSelectedCellEditable;
    public bool IsSelectedCellReadOnly => !IsSelectedCellEditable;
    private bool _updatingSelectedCellEditor;

    private void RefreshSelectedCellEditor()
    {
        var cell = _adapter?.GetSelectedCell() ?? (Address: "--", Content: "", CanEdit: false);
        _updatingSelectedCellEditor = true;
        try
        {
            SelectedCellAddress = cell.Address;
            IsSelectedCellEditable = cell.CanEdit;
            SelectedCellContent = cell.Content;
        }
        finally { _updatingSelectedCellEditor = false; }
    }

    partial void OnSelectedCellContentChanged(string value)
    {
        if (_updatingSelectedCellEditor) return;
        _adapter?.SetSelectedCellContent(value);
        RefreshSelectedCellEditor();
    }

    public string PackageName => Package is null ? "" : EpmaScriptText.Format(Package.Manifest.Name);
    public string PackageVersion => Package is null ? "" : "v" + Package.Manifest.Version;
    public bool HasRun => Run is not null;
    /// <summary>Shown in the inspector while there is no row detail to display.</summary>
    public string InspectorHint => EpmaLocalizer.Format(IsBusy ? "epma_qc_in_progress" : !HasRun ? "epma_qc_not_calculated" : "epma_no_data");

    private void RefreshParameterLayout()
    {
        OnPropertyChanged(nameof(PackageName));
        OnPropertyChanged(nameof(PackageVersion));
    }

    private void RefreshQcPresentation()
    {
        OnPropertyChanged(nameof(HasRun));
        OnPropertyChanged(nameof(InspectorHint));
    }
}
