using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    public class CategoryPartModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public Dictionary<string, string>? LocalizedNames { get; set; }
    }

    public class LanguageTagModel : ObservableObject
    {
        private bool _isDefault;

        public string Text { get; set; } = string.Empty;

        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }
    }

    public class PlotTypeOption
    {
        public string Key { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SelectedLabel { get; set; } = string.Empty;
    }
}
