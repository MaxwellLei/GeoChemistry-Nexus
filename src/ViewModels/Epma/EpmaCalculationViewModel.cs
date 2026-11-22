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

/// <summary>Package-driven calculation screen using the shared standard-input policy.</summary>
public partial class EpmaCalculationViewModel : ObservableObject, IWorksheetRowStatusProvider
{
    private readonly EpmaFileService _files = new();
    private EpmaWorksheetAdapter? _adapter; private EpmaInputTemplate _cache = new();
    private readonly Dictionary<string, EpmaInputTemplate> _inputCaches = new(StringComparer.Ordinal); private CancellationTokenSource? _cancel;
    private CancellationTokenSource? _displayCancel;
    private int _generation; private readonly HashSet<int> _stale = new();
    public EpmaScriptPackage? Package { get; private set; }
    private EpmaScriptRun? _run;
    private Dictionary<int, EpmaScriptRow> _rowsByInput = new();
    public EpmaScriptRun? Run
    {
        get => _run;
        private set
        {
            _run = value;
            // Row-header dots ask for every visible row on each repaint, so look rows up by input row.
            _rowsByInput = value is null ? new() : value.Rows.ToDictionary(r => r.InputRow);
        }
    }
    private int _selectedRow;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _blockingText = "";
    [ObservableProperty] private string _retainedInputText = "";
    public string Title => Package is null ? "" : EpmaScriptText.Format(Package.Manifest.Name) + " v" + Package.Manifest.Version;
    public IRelayCommand BackCommand { get; }
    public EpmaCalculationViewModel(Action back) { BackCommand = new RelayCommand(back); }
    public void InitializeWorksheet(ReoGridControl grid)
    {
        _adapter = new(grid); _adapter.InputChanged += row => Invalidate(row == 0 ? null : row);
        _adapter.PasteRejected += () => MessageHelper.Warning(EpmaLocalizer.Format("epma_input_paste_rejected"));
        _adapter.SelectionChanged += SelectRow;
        _adapter.SelectedCellChanged += RefreshSelectedCellEditor;
        _adapter.SetRowStatusProvider(this);
        if (Package is not null) RebuildInputs();
        RefreshSelectedCellEditor();
    }
    public void Activate(EpmaScriptPackage package)
    {
        IsHelpVisible = false;
        if (Package?.Sha256 == package.Sha256) { OnPropertyChanged(nameof(Title)); RefreshParameterLayout(); return; }
        Cancel();
        if (Package is not null && _adapter is not null) { _cache.Remember(_adapter.Capture()); _inputCaches[Package.Manifest.PackageId] = _cache; }
        _cache = _inputCaches.GetValueOrDefault(package.Manifest.PackageId) ?? new EpmaInputTemplate();
        _inputCaches[package.Manifest.PackageId] = _cache;
        IsBusy = false; Package = package; Run = null; _stale.Clear();
        ResetDetails();
        RefreshParameterLayout();
        ExampleCommand.NotifyCanExecuteChanged();
        BlockingText = ""; OnPropertyChanged(nameof(Title)); RebuildInputs(false); RefreshHelp();
    }
    public void RefreshLocalization()
    {
        RefreshParameterLayout();
        RefreshDisplay(); OnPropertyChanged(nameof(Title)); SelectRow(_selectedRow);
        RefreshInputValidation(); RefreshHelp();
    }
    private void RebuildInputs(bool capture = true)
    {
        if (_adapter is null || Package is null) { RefreshInputValidation(); return; }
        if (capture) _cache.Remember(_adapter.Capture());
        var inputs = Package.Manifest.Inputs;
        var headers = inputs.Select(i => i.Id).ToArray();
        _adapter.SetInputHeaders(inputs.Select(i => EpmaScriptText.Format(i.Unit)), inputs.Select(i => i.DisplayGroup));
        _adapter.Load(_cache.Build(headers));
        RetainedInputText = string.Join(", ", _cache.RetainedColumns(headers));
        SelectRow(_adapter.SelectedInputRow);
        if (Run is not null) RefreshDisplay();
        RefreshInputValidation();
    }
    private void RefreshInputValidation()
    {
        CalculateCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
    }
    private void Invalidate(int? row = null)
    {
        _generation++; _cancel?.Cancel();
        if (Run is not null)
        {
            if (row is { } r) { if (_rowsByInput.TryGetValue(r, out var edited) && edited.Result.Status != "Empty") _stale.Add(r); }
            else foreach (var result in Run.Rows.Where(result => result.Result.Status != "Empty")) _stale.Add(result.InputRow);
        }
        // Row dots also react to input typed before the first calculation (grey "not calculated" dot).
        RowStatusChanged?.Invoke(this, EventArgs.Empty);
        if (Run is not null) SelectRow(_selectedRow);
    }
    private void CancelOperation() { _generation++; _cancel?.Cancel(); }
    [RelayCommand] public void Cancel() { CancelOperation(); _displayCancel?.Cancel(); }
    private bool CanCalculate() => !IsBusy && Package is not null && _adapter is not null;
    private bool CanExport() => !IsBusy && Run is not null;
    partial void OnIsBusyChanged(bool value) { ExampleCommand.NotifyCanExecuteChanged(); CalculateCommand.NotifyCanExecuteChanged(); ExportCommand.NotifyCanExecuteChanged(); RefreshQcPresentation(); }
    [RelayCommand(CanExecute = nameof(CanCalculate))]
    private async Task Calculate()
    {
        Cancel(); _cancel?.Dispose(); _cancel = new(); var operation = _cancel; int generation = _generation; var token = operation.Token; IsBusy = true;
        try
        {
            var run = await EpmaScriptRuntime.CalculateAsync(Package!, _adapter!.Capture(), NumericFormatChoice.DecimalPoint, token);
            if (generation != _generation || token.IsCancellationRequested) return;
            Run = run; _stale.Clear(); BlockingText = ""; ResetInspectorDetails(); RefreshQcPresentation(); SelectRow(DefaultDetailRow(run)); RefreshDisplay();
            RowStatusChanged?.Invoke(this, EventArgs.Empty);
            MessageHelper.Success(EpmaLocalizer.Format("epma_calculated", run.Rows.Count(r => r.Result.Status == "Computed")));
        }
        catch (OperationCanceledException) { if (generation == _generation) MessageHelper.Info(EpmaLocalizer.Format("epma_cancelled")); }
        catch (Exception ex) { if (generation == _generation) { BlockingText = ex.Message; MessageHelper.Error(EpmaLocalizer.Format("epma_calculation_failed") + ": " + ex.Message); } }
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
                    displayToken.ThrowIfCancellationRequested(); try { return EpmaScriptRuntime.Render(run, r, Math.Clamp(decimals, 0, 10), displayToken); }
                    catch (OperationCanceledException) { displayToken.ThrowIfCancellationRequested(); return (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(); }
                    catch { return (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(); }
                }), displayToken);
            if (!ReferenceEquals(run.Input, Run?.Input) || displayToken.IsCancellationRequested) return;
            _adapter.UpdateDisplayText(run, displays);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { MessageHelper.Error(ex.Message); }
    }
    private int DefaultDetailRow(EpmaScriptRun run)
    {
        int selected = _adapter?.SelectedInputRow ?? _selectedRow;
        return run.Rows.Any(r => r.InputRow == selected && r.Result.Status != "Empty") ? selected : run.Rows.FirstOrDefault(r => r.Result.Status != "Empty")?.InputRow ?? 0;
    }
    private void ResetDetails()
    {
        ResetInspectorDetails();
        RefreshQcPresentation();
        RowStatusChanged?.Invoke(this, EventArgs.Empty);
        RefreshInspector();
    }
    private void SelectRow(int inputRow)
    {
        _selectedRow = inputRow;
        RefreshInspector();
    }
    private bool CanLoadExamples() => !IsBusy && Package is not null && _adapter is not null;
    [RelayCommand(CanExecute = nameof(CanLoadExamples))]
    private void Example()
    {
        if (Package is null || _adapter is null) return;
        Cancel(); _cache.Clear();
        _cache.Remember(EpmaScriptRuntime.ExampleInput(Package));
        Run = null; _stale.Clear(); ResetDetails(); RebuildInputs(false);
        MessageHelper.Info(EpmaLocalizer.Format("epma_examples_loaded", Package.Examples.Inputs.Length));
    }
    [RelayCommand] private void Clear() { Cancel(); IsBusy = false; _cache.Clear(); Run = null; _stale.Clear(); RebuildInputs(false); ResetDetails(); }
    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task Export()
    {
        var run = Run; if (run is null) return; string? path = await FileHelper.GetSaveFilePath2Async(EpmaLocalizer.Format("epma_export"), "CSV|*.csv|Excel|*.xlsx", ".csv", "EPMA_Results.csv");
        if (string.IsNullOrEmpty(path)) return;
        CancelOperation(); _cancel?.Dispose(); _cancel = new(); var operation = _cancel; int generation = _generation; IsBusy = true;
        try { await _files.WriteRunAsync(path, run, new HashSet<int>(_stale), operation.Token); if (generation == _generation) MessageHelper.ExportSuccess(EpmaLocalizer.Format("export_successful"), path); }
        catch (OperationCanceledException) when (operation.IsCancellationRequested) { }
        catch (Exception ex) { if (generation == _generation) MessageHelper.Error(ex.Message); }
        finally
        {
            if (ReferenceEquals(operation, _cancel)) IsBusy = false;
        }
    }
}
