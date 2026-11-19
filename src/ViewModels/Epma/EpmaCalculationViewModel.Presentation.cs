using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels;

public partial class EpmaCalculationViewModel
{
    [ObservableProperty] private bool _isDiagnosticsExpanded;
    [ObservableProperty] private bool _isSettingsExpanded = true;

    public string SettingsToggleText => EpmaLocalizer.Format(IsSettingsExpanded ? "epma_collapse_settings" : "epma_expand_settings");
    partial void OnIsSettingsExpandedChanged(bool value) => OnPropertyChanged(nameof(SettingsToggleText));
    [RelayCommand] private void ToggleSettings() => IsSettingsExpanded = !IsSettingsExpanded;
    [RelayCommand] private void ToggleDiagnostics() => IsDiagnosticsExpanded = !IsDiagnosticsExpanded;
    [RelayCommand] private void ResetWorksheetLayout() => _adapter?.ResetLayout();

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

    // These names arrange controls; reporting visibility uses the shared input convention.
    private static string ParameterGroup(EpmaScriptParameterItem item) => item.Definition.Id switch
    {
        "ironForm" or "ironMethod" or "fe3Ratio" => "iron",
        "basis" or "target" => "normalization",
        "detectionLimitStrategy" => "input",
        _ => "other",
    };

    public IEnumerable<EpmaScriptParameterItem> IronParameters => Parameters.Where(p => ParameterGroup(p) == "iron");
    public IEnumerable<EpmaScriptParameterItem> IronModeParameters => IronParameters.Where(p => p.Definition.Id is "ironForm" or "ironMethod");
    public IEnumerable<EpmaScriptParameterItem> IronValueParameters => IronParameters.Where(p => p.Definition.Id is not ("ironForm" or "ironMethod"));
    public bool HasIronValueParameters => IronValueParameters.Any(p => p.Visible);
    public IEnumerable<EpmaScriptParameterItem> NormalizationParameters => Parameters.Where(p => ParameterGroup(p) == "normalization");
    public IEnumerable<EpmaScriptParameterItem> InputParameters => Parameters.Where(p => ParameterGroup(p) == "input");
    public IEnumerable<EpmaScriptParameterItem> OtherParameters => Parameters.Where(p => ParameterGroup(p) == "other");
    public bool HasNoIronParameters => !IronParameters.Any(p => p.Visible);
    public bool HasIronParameters => !HasNoIronParameters;
    public bool HasFixedRules => !NormalizationParameters.Any(p => p.Visible);
    public bool HasOtherParameters => OtherParameters.Any(p => p.Visible);

    public string PackageName => Package is null ? "" : EpmaScriptText.Format(Package.Manifest.Name);
    public string PackageVersion => Package is null ? "" : "v" + Package.Manifest.Version;
    public string TemplateDescription => Package is null ? "" : EpmaScriptText.Format(Package.Manifest.Description);
    public string TemplateRuleSummary => TemplateDescription.Split('\n', '，', ',')[0].Trim() is { Length: > 0 } summary
        ? summary : EpmaLocalizer.Format("epma_template_rules_hint");

    public bool HasRun => Run is not null;
    public int ErrorCount => Diagnostics.Count(d => d.Level == "Error");
    public int WarningCount => Diagnostics.Count(d => d.Level == "Warning");
    public int InformationCount => Diagnostics.Count(d => d.Level == "Info");
    public bool HasStaleResults => _stale.Count > 0;
    public bool IsQcClear => HasRun && !IsBusy && !HasStaleResults && Diagnostics.Count == 0
        && Run!.Rows.Any(r => r.Result.Status == "Computed");
    public string QcEmptyTitle => EpmaLocalizer.Format(IsBusy ? "epma_qc_in_progress"
        : HasStaleResults ? "epma_stale"
        : !HasRun ? "epma_qc_not_calculated"
        : IsQcClear ? "epma_qc_clear" : "epma_no_data");

    private void RefreshParameterLayout()
    {
        OnPropertyChanged(nameof(IronParameters));
        OnPropertyChanged(nameof(IronModeParameters));
        OnPropertyChanged(nameof(IronValueParameters));
        OnPropertyChanged(nameof(SettingsToggleText));
        OnPropertyChanged(nameof(NormalizationParameters));
        OnPropertyChanged(nameof(InputParameters));
        OnPropertyChanged(nameof(OtherParameters));
        RefreshParameterVisibility();
        OnPropertyChanged(nameof(PackageName));
        OnPropertyChanged(nameof(PackageVersion));
        OnPropertyChanged(nameof(TemplateDescription));
        OnPropertyChanged(nameof(TemplateRuleSummary));
    }

    private void RefreshParameterVisibility()
    {
        OnPropertyChanged(nameof(HasNoIronParameters));
        OnPropertyChanged(nameof(HasIronParameters));
        OnPropertyChanged(nameof(HasIronValueParameters));
        OnPropertyChanged(nameof(HasFixedRules));
        OnPropertyChanged(nameof(HasOtherParameters));
    }

    private void RefreshQcPresentation()
    {
        OnPropertyChanged(nameof(HasRun));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(InformationCount));
        OnPropertyChanged(nameof(HasStaleResults));
        OnPropertyChanged(nameof(IsQcClear));
        OnPropertyChanged(nameof(QcEmptyTitle));
    }
}
