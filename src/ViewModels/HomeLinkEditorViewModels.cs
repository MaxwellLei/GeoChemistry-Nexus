using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace GeoChemistryNexus.ViewModels
{
    public partial class HomeLinkEntryEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string id = string.Empty;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private string url = string.Empty;

        [ObservableProperty]
        private string icon = "\uE774";
    }

    public partial class HomeLinkGroupEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string id = string.Empty;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private int sortOrder;

        public ObservableCollection<HomeLinkEntryEditorViewModel> Links { get; } = new();
    }
}
