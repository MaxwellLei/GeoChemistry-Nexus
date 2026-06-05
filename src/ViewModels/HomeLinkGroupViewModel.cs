using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Models;
using System.Collections.ObjectModel;

namespace GeoChemistryNexus.ViewModels
{
    public partial class HomeLinkGroupViewModel : ObservableObject
    {
        [ObservableProperty]
        private string groupId = string.Empty;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private bool isPersonal;

        [ObservableProperty]
        private bool isVisible = true;

        public ObservableCollection<HomeAppItem> Items { get; } = new();
    }
}
