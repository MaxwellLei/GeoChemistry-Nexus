using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services.Epma.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services.Epma;
using unvell.ReoGrid;
namespace GeoChemistryNexus.ViewModels;

public sealed class EpmaScriptParameterItem : ObservableObject
{
    public EpmaScriptSetting Definition { get; }
    public string Label => Definition.DisplayLabel + (EpmaScriptText.Format(Definition.Unit) is { Length: > 0 } unit ? " (" + unit + ")" : "");
    public void RefreshLabel() => OnPropertyChanged(nameof(Label));
    public string Type => Definition.Type;
    public bool Editable => Definition.Editable;
    public bool IsReadOnly => !Editable;
    public ObservableCollection<EpmaScriptChoiceItem> Choices { get; } = new();
    private string _text = ""; private bool _visible = true;
    public string Text { get => _text; set { if (Editable && SetProperty(ref _text, value ?? "")) Changed?.Invoke(); } }
    public bool Visible { get => _visible; set => SetProperty(ref _visible, value); }
    public event Action? Changed;
    public EpmaScriptParameterItem(EpmaScriptSetting definition, JsonElement value) { Definition = definition; SetValue(value); }
    public void SetValue(JsonElement value) { if (!Editable) value = Definition.Value; _text = Definition.Type == "number" && value.ValueKind == JsonValueKind.Number ? value.GetDouble().ToString("R", CultureInfo.CurrentCulture) : EpmaScriptText.Scalar(value); OnPropertyChanged(nameof(Text)); }
    public JsonElement Value => Definition.Type == "number" ?
        double.TryParse(Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double n) && double.IsFinite(n) ? JsonSerializer.SerializeToElement(n) : JsonSerializer.SerializeToElement<object?>(null) :
        Text.Length > 0 ? JsonSerializer.SerializeToElement(Text) : JsonSerializer.SerializeToElement<object?>(null);
}
public sealed record EpmaScriptChoiceItem(string Value, string Label);
/// <summary>Package-driven calculation screen using the shared standard-input policy.</summary>
public partial class EpmaCalculationViewModel : ObservableObject
{
    private readonly EpmaScriptRuntime _runtime = new(); private readonly EpmaFileService _files = new();
    private EpmaWorksheetAdapter? _adapter; private EpmaInputTemplate _cache = new();
    private readonly Dictionary<string, EpmaInputTemplate> _inputCaches = new(StringComparer.Ordinal); private CancellationTokenSource? _cancel;
    private CancellationTokenSource? _displayCancel;
    private int _generation; private bool _updating; private readonly HashSet<int> _stale = new();
    public EpmaScriptPackage? Package { get; private set; }
    public EpmaScriptRun? Run { get; private set; }
    private EpmaScriptInput[] _activeInputs = [];
    private IReadOnlyList<EpmaScriptFlag> _parameterErrors = [];
    public ObservableCollection<EpmaScriptParameterItem> Parameters { get; } = new();
    public ObservableCollection<EpmaDiagnosticInfo> Diagnostics { get; } = new();
    public IReadOnlyList<EpmaScriptOutput> Fields => Run?.Fields ?? Array.Empty<EpmaScriptOutput>();
    public ObservableCollection<EpmaChoice<NumericFormatChoice>> NumericFormats { get; } = new(Enum.GetValues<NumericFormatChoice>().Select(v => new EpmaChoice<NumericFormatChoice>(v, "epma_numeric_" + v)));
    private int _selectedRow;
    private readonly Dictionary<int, string> _rowDetails = new();
    private readonly Dictionary<int, string> _rowProcesses = new();
    private string _runDetails = "";
    [ObservableProperty] private EpmaChoice<NumericFormatChoice>? _numericFormat;
    [ObservableProperty] private int _displayDecimals = 4;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _blockingText = "";
    [ObservableProperty] private string _details = "";
    [ObservableProperty] private string _processText = "";
    [ObservableProperty] private string _qcStatusText = EpmaLocalizer.Format("epma_qc_not_calculated");
    [ObservableProperty] private string _retainedInputText = "";
    public string Title => Package is null ? "" : EpmaScriptText.Format(Package.Manifest.Name) + " v" + Package.Manifest.Version;
    public IRelayCommand BackCommand { get; }
    public EpmaCalculationViewModel(Action back) { BackCommand = new RelayCommand(back); NumericFormat = NumericFormats[0]; }
    public void InitializeWorksheet(ReoGridControl grid)
    {
        _adapter = new(grid); _adapter.InputChanged += row => Invalidate(row == 0 ? null : row);
        _adapter.PasteRejected += () => StatusText = EpmaLocalizer.Format("epma_input_paste_rejected");
        _adapter.SelectionChanged += SelectRow;
        _adapter.SelectedCellChanged += RefreshSelectedCellEditor;
        if (Package is not null) RebuildInputs();
        RefreshSelectedCellEditor();
    }
    public void Activate(EpmaScriptPackage package)
    {
        IsDiagnosticsExpanded = false; IsSettingsExpanded = true; IsHelpVisible = false;
        if (Package?.Sha256 == package.Sha256) { OnPropertyChanged(nameof(Title)); RefreshParameterLayout(); return; }
        Cancel();
        if (Package is not null && _adapter is not null) { _cache.Remember(_adapter.Capture()); _inputCaches[Package.Manifest.PackageId] = _cache; }
        _cache = _inputCaches.GetValueOrDefault(package.Manifest.PackageId) ?? new EpmaInputTemplate();
        _inputCaches[package.Manifest.PackageId] = _cache;
        _updating = true; IsBusy = false; Package = package; Run = null; _stale.Clear();
        Parameters.Clear(); OnPropertyChanged(nameof(Fields)); ResetDetails();
        var defaults = _runtime.Defaults(package);
        foreach (var p in EpmaInputPolicy.Parameters(package)) { var item = new EpmaScriptParameterItem(p, defaults[p.Id]); item.Changed += ParameterChanged; Parameters.Add(item); }
        RefreshParameterLayout();
        ExampleCommand.NotifyCanExecuteChanged();
        _updating = false; StatusText = ""; BlockingText = ""; Details = ""; OnPropertyChanged(nameof(Title)); RefreshChoices(); RefreshConditions(); RebuildInputs(false); RefreshHelp();
    }
    public void RefreshLocalization()
    {
        foreach (var p in Parameters) p.RefreshLabel();
        foreach (var format in NumericFormats) format.RefreshLabel();
        RefreshParameterLayout();
        RefreshChoices(); RefreshConditions(); RefreshDisplay(); OnPropertyChanged(nameof(Title)); RefreshDiagnostics(); SelectRow(_selectedRow);
        System.Windows.Data.CollectionViewSource.GetDefaultView(NumericFormats).Refresh();
        System.Windows.Data.CollectionViewSource.GetDefaultView(Fields).Refresh();
        RefreshInputValidation(); RefreshHelp();
    }
    private Dictionary<string, JsonElement> Settings() => Parameters.Select(p => new KeyValuePair<string, JsonElement>(p.Definition.Id, p.Value))
        .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
    private void ParameterChanged()
    {
        if (_updating) return;
        var before = _activeInputs.Select(i => i.Id).ToArray();
        Invalidate(); RefreshConditions();
        if (!before.SequenceEqual(_activeInputs.Select(i => i.Id))) RebuildInputs();
        else RefreshInputValidation();
    }
    private void RefreshChoices()
    {
        bool updating = _updating; _updating = true;
        try
        {
            foreach (var p in Parameters.Where(p => p.Type == "choice"))
            {
                string selected = p.Text;
                p.Choices.Clear();
                if (p.Definition.Id is not ("ironForm" or "ironMethod")) p.Choices.Add(new("", EpmaLocalizer.Format("epma_choose")));
                foreach (var c in p.Definition.Choices) p.Choices.Add(new(c.Value, c.DisplayLabel));
                p.Text = selected;
            }
        }
        finally { _updating = updating; }
    }
    private void RefreshConditions()
    {
        var current = Settings();
        foreach (var p in Parameters) p.Visible = Package is not null && EpmaInputPolicy.ParameterVisible(Package, p.Definition, current);
        _activeInputs = Package is null ? [] : EpmaInputPolicy.ActiveInputs(Package.Manifest, current);
        RefreshParameterVisibility();
    }
    private void RebuildInputs(bool capture = true)
    {
        if (_adapter is null || Package is null) { RefreshInputValidation(); return; }
        if (capture) _cache.Remember(_adapter.Capture());
        var headers = _activeInputs.Select(i => i.Id).ToArray();
        _adapter.SetInputUnits(_activeInputs.Select(i => EpmaScriptText.Format(i.Unit)));
        _adapter.Load(_cache.Build(headers));
        RetainedInputText = string.Join(", ", _cache.RetainedColumns(headers));
        SelectRow(_adapter.SelectedInputRow);
        if (Run is not null) RefreshDisplay();
        RefreshInputValidation();
    }
    private void RefreshInputValidation()
    {
        _parameterErrors = Package is null ? [] : _runtime.ValidateParameterValues(Package, Settings());
        BlockingText = string.Join("\n", _parameterErrors.Select(f => f.Text));
        CalculateCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
    }
    private void Invalidate(int? row = null)
    {
        _generation++; _cancel?.Cancel(); if (Run is null) return;
        if (row is { } r) { if (Run.Rows.Any(result => result.InputRow == r && result.Result.Status != "Empty")) _stale.Add(r); }
        else foreach (var result in Run.Rows.Where(result => result.Result.Status != "Empty")) _stale.Add(result.InputRow);
        StatusText = EpmaLocalizer.Format("epma_stale"); RefreshDiagnostics(); SelectRow(_selectedRow);
    }
    private void CancelOperation() { _generation++; _cancel?.Cancel(); }
    [RelayCommand] public void Cancel() { CancelOperation(); _displayCancel?.Cancel(); }
    private bool CanCalculate() => !IsBusy && Package is not null && _adapter is not null && !_parameterErrors.Any(f => f.Severity == "Error");
    private bool CanExport() => !IsBusy && Run is not null;
    partial void OnIsBusyChanged(bool value) { ExampleCommand.NotifyCanExecuteChanged(); CalculateCommand.NotifyCanExecuteChanged(); ExportCommand.NotifyCanExecuteChanged(); RefreshQcPresentation(); }
    partial void OnDisplayDecimalsChanged(int value) => RefreshDisplay();
    partial void OnNumericFormatChanged(EpmaChoice<NumericFormatChoice>? value) { if (!_updating) Invalidate(); }
    [RelayCommand(CanExecute = nameof(CanCalculate))]
    private async Task Calculate()
    {
        IsSettingsExpanded = false;
        Cancel(); _cancel?.Dispose(); _cancel = new(); var operation = _cancel; int generation = _generation; var token = operation.Token; IsBusy = true;
        try
        {
            var run = await _runtime.CalculateAsync(Package!, _adapter!.Capture(), Settings(), NumericFormat?.Value ?? NumericFormatChoice.DecimalPoint, token);
            if (generation != _generation || token.IsCancellationRequested) return;
            Run = run; OnPropertyChanged(nameof(Fields)); _stale.Clear(); BlockingText = ""; PrepareDetails(); SelectRow(DefaultDetailRow(run)); RefreshDisplay();
            StatusText = EpmaLocalizer.Format("epma_calculated", run.Rows.Count(r => r.Result.Status == "Computed"));
        }
        catch (OperationCanceledException) { if (generation == _generation) StatusText = EpmaLocalizer.Format("epma_cancelled"); }
        catch (Exception ex) { if (generation == _generation) { BlockingText = ex.Message; StatusText = EpmaLocalizer.Format("epma_calculation_failed"); } }
        finally { if (ReferenceEquals(operation, _cancel)) IsBusy = false; }
    }
    private void RefreshDisplay() => _ = RefreshDisplayAsync();
    private async Task RefreshDisplayAsync()
    {
        var run = Run; if (run is null || _adapter is null) return;
        int decimals = DisplayDecimals;
        _adapter.Display(run, decimals);
        _displayCancel?.Cancel(); _displayCancel?.Dispose(); _displayCancel = null;
        if (!run.Rows.Any(row => row.Result.Values.Any(pair => pair.Value.ValueKind == JsonValueKind.String))) return;
        _displayCancel = new(); var displayToken = _displayCancel.Token;
        try
        {
            var displays = await Task.Run(() => run.Rows.Where(r => r.Result.Status == "Computed").ToDictionary(r => r.InputRow, r =>
                {
                    displayToken.ThrowIfCancellationRequested(); try { return _runtime.Render(run, r, Math.Clamp(decimals, 0, 10), displayToken); }
                    catch (OperationCanceledException) { displayToken.ThrowIfCancellationRequested(); return (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(); }
                    catch { return (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(); }
                }), displayToken);
            if (!ReferenceEquals(run.Input, Run?.Input) || displayToken.IsCancellationRequested || decimals != DisplayDecimals) return;
            _adapter.UpdateDisplayText(run, displays);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusText = ex.Message; }
    }
    private int DefaultDetailRow(EpmaScriptRun run)
    {
        int selected = _adapter?.SelectedInputRow ?? _selectedRow;
        return run.Rows.Any(r => r.InputRow == selected && r.Result.Status != "Empty") ? selected : run.Rows.FirstOrDefault(r => r.Result.Status != "Empty")?.InputRow ?? 0;
    }
    private void ResetDetails()
    {
        _rowDetails.Clear(); _rowProcesses.Clear(); _runDetails = ""; Diagnostics.Clear(); Details = ""; ProcessText = "";
        QcStatusText = EpmaLocalizer.Format("epma_qc_not_calculated");
        RefreshQcPresentation();
    }
    private void PrepareDetails()
    {
        _rowDetails.Clear(); _rowProcesses.Clear(); RefreshDiagnostics(); var run = Run; if (run is null) return;
        _runDetails = EpmaResultPresentation.Provenance(new (string, string)[]
        {
            ("Package", run.Package.Manifest.PackageId + " v" + run.Package.Manifest.Version),
            ("SHA256", run.Package.Sha256), ("Format", run.Package.Manifest.FormatVersion.ToString()),
            ("Engine", run.EngineVersion), ("Calculated", run.CalculatedAt.ToString("O")),
            ("Parameters", JsonSerializer.Serialize(run.Parameters)),
        });
    }
    private void SelectRow(int inputRow)
    {
        _selectedRow = inputRow; Details = ""; ProcessText = "";
        if (Run is null) return;
        var row = Run.Rows.FirstOrDefault(r => r.InputRow == inputRow);
        if (row is null || row.Result.Status == "Empty") return;
        if (!_rowDetails.TryGetValue(inputRow, out var details))
        {
            details = _runDetails + "\n" + EpmaResultPresentation.Provenance(new (string, string)[]
            {
                ("Input row", inputRow.ToString()), ("Status", row.Result.Status),
                ("Field sources", JsonSerializer.Serialize(row.Result.FieldSources)),
            });
            _rowDetails[inputRow] = details;
        }
        Details = details;
        if (!_rowProcesses.TryGetValue(inputRow, out var process))
            _rowProcesses[inputRow] = process = EpmaResultPresentation.Process(row.Result.Trace);
        ProcessText = process;
    }
    private void RefreshDiagnostics()
    {
        Diagnostics.Clear();
        var run = Run;
        if (run is null) { QcStatusText = EpmaLocalizer.Format("epma_qc_not_calculated"); RefreshQcPresentation(); return; }
        var sampleInput = run.Package.Manifest.Inputs.FirstOrDefault(i => i.Id.Equals("sample", StringComparison.OrdinalIgnoreCase));
        foreach (var row in run.Rows.Where(r => r.Result.Status != "Empty"))
        {
            string sample = "";
            if (sampleInput is not null && row.Inputs.TryGetValue(sampleInput.Id, out var cell)) sample = cell.Raw;
            bool stale = _stale.Contains(row.InputRow);
            var flags = row.Flags.ToArray();
            foreach (var f in flags) Diagnostics.Add(new(row.InputRow, sample, f.Severity, f.Code, f.Target, f.ActualText, f.ExpectedText, f.Source ?? "", f.Text, stale));
            if (row.Result.Status == "Skipped" && flags.Length == 0)
                Diagnostics.Add(new(row.InputRow, sample, "Error", "EPMA_ROW_SKIPPED", "Row", "", "", "", EpmaLocalizer.Format("epma_qc_skipped_row"), stale));
            if (row.Result.Status != "Computed") continue;
            foreach (var missing in row.Result.UndefinedReasons)
            {
                var field = run.Fields.Single(f => f.Id == missing.Key);
                if (missing.Value == "NotApplicable" || flags.Any(f => f.Target == field.Id)) continue;
                Diagnostics.Add(new(row.InputRow, sample, "Info", "EPMA_RESULT_UNAVAILABLE", field.Id, "", "", "",
                    EpmaLocalizer.Format("epma_result_unavailable", field.DisplayLabel, missing.Value), stale));
            }
        }
        QcStatusText = EpmaDiagnosticSummary.Format(run.Rows.Count(r => r.Result.Status != "Empty"), Diagnostics, _stale.Count);
        RefreshQcPresentation();
    }
    public void FocusDiagnostic(EpmaDiagnosticInfo diagnostic) => _adapter?.SelectInputRow(diagnostic.InputRow);
    private bool CanLoadExamples() => !IsBusy && Package is not null && _adapter is not null;
    [RelayCommand(CanExecute = nameof(CanLoadExamples))]
    private void Example()
    {
        if (Package is null || _adapter is null) return;
        Cancel(); _updating = true;
        var settings = _runtime.Defaults(Package).ToDictionary(p => p.Key, p => p.Value);
        foreach (var p in Package.Examples.Parameters) settings[p.Key] = p.Value;
        foreach (var p in Parameters) p.SetValue(settings[p.Definition.Id]);
        _updating = false; RefreshConditions(); _cache.Clear();
        _cache.Remember(EpmaScriptRuntime.ExampleInput(Package));
        Run = null; OnPropertyChanged(nameof(Fields)); _stale.Clear(); ResetDetails(); RebuildInputs(false);
        StatusText = EpmaLocalizer.Format("epma_examples_loaded", Package.Examples.Inputs.Length);
    }
    [RelayCommand] private void Clear() { Cancel(); IsBusy = false; _cache.Clear(); Run = null; OnPropertyChanged(nameof(Fields)); _stale.Clear(); RebuildInputs(false); ResetDetails(); StatusText = ""; }
    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task Export()
    {
        var run = Run; if (run is null) return; string? path = await FileHelper.GetSaveFilePath2Async(EpmaLocalizer.Format("epma_export"), "CSV|*.csv|Excel|*.xlsx", ".csv", "EPMA_Results.csv");
        if (string.IsNullOrEmpty(path)) return;
        CancelOperation(); _cancel?.Dispose(); _cancel = new(); var operation = _cancel; int generation = _generation; IsBusy = true;
        try { await _files.WriteRunAsync(path, run, new HashSet<int>(_stale), operation.Token); if (generation == _generation) MessageHelper.ExportSuccess(EpmaLocalizer.Format("export_successful"), path); }
        catch (OperationCanceledException) when (operation.IsCancellationRequested) { }
        catch (Exception ex) { if (generation == _generation) StatusText = ex.Message; }
        finally
        {
            if (ReferenceEquals(operation, _cancel)) IsBusy = false;
        }
    }
}
