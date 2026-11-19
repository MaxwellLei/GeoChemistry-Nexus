using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels;

/// <summary>Help document of the open calculation package, shown below the toolbar of the calculation page.</summary>
public partial class EpmaCalculationViewModel
{
    [ObservableProperty] private bool _isHelpVisible;

    /// <summary>RTF of the package help document for the software language; null when the package has none.</summary>
    public string? HelpRtf => Package is null ? null : EpmaHelpDocuments.Resolve(Package.Manifest.Help);
    public bool HasHelpDocument => !string.IsNullOrEmpty(HelpRtf);

    [RelayCommand] private void ToggleHelp() => IsHelpVisible = !IsHelpVisible;

    private void RefreshHelp()
    {
        OnPropertyChanged(nameof(HelpRtf));
        OnPropertyChanged(nameof(HasHelpDocument));
    }
}
