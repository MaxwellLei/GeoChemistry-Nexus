using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.ViewModels;

public partial class EpmaPageViewModel : ObservableObject
{
    public EpmaPageViewModel()
    {
        Workbench = new EpmaCalculationViewModel(BackToLibrary);
        Workbench.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Workbench.IsBusy)) { EnterCalculationCommand.NotifyCanExecuteChanged(); } };
        LanguageService.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != "Item[]") return;
            RefreshLibrary(); Workbench.RefreshLocalization();
        };
        RefreshLibrary();
    }
    public void LeavePage() => Workbench.Cancel();
}
