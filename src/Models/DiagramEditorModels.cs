using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 选择已有分类结构对话框中的树节点。
    /// </summary>
    public class CategoryStructureSelectNode
    {
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<CategoryStructureSelectNode> Children { get; } = new();
        public bool IsSelectable { get; set; }
        public LocalizedString? SourceNodeList { get; set; }
        public int PrefixLength { get; set; }
    }

    public class CategoryPartModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public Dictionary<string, string>? LocalizedNames { get; set; }
    }

    public class LanguageTagModel : ObservableObject
    {
        private bool _isDefault;

        public string Text { get; set; } = string.Empty;

        public string DisplayText => AppCultureRegistry.GetDisplayName(Text);

        public void NotifyDisplayTextChanged()
        {
            OnPropertyChanged(nameof(DisplayText));
        }

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
