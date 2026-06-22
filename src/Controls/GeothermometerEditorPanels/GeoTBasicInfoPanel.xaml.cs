using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoChemistryNexus.Controls.GeothermometerEditorPanels
{
    public partial class GeoTBasicInfoPanel : UserControl
    {
        public GeoTBasicInfoPanel()
        {
            InitializeComponent();
        }

        public void RefreshTagSuggestions()
        {
            if (DataContext is not GeothermometerEditorViewModel vm) return;

            var list = vm.GetTagSuggestions();
            TagCombo.ItemsSource = list.Select(entry =>
            {
                string displayName = AppCultureRegistry.GetLocalizedValue(
                    entry,
                    LanguageService.CurrentLanguage,
                    AppCultureRegistry.DefaultAppLanguage);
                if (string.IsNullOrWhiteSpace(displayName) &&
                    entry.TryGetValue("zh-CN", out var zh) &&
                    !string.IsNullOrWhiteSpace(zh))
                {
                    displayName = zh;
                }

                return new TagDisplayItem
                {
                    DisplayName = displayName ?? string.Empty,
                    OriginalObject = entry
                };
            }).ToList();
        }

        private void TagCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is GeothermometerEditorViewModel vm &&
                vm.TryAddTag(TagCombo.Text?.Trim() ?? string.Empty))
            {
                TagCombo.Text = string.Empty;
            }
            e.Handled = true;
        }

        private void TagCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not GeothermometerEditorViewModel vm) return;
            if (!TagCombo.IsDropDownOpen)
            {
                TagCombo.SelectedIndex = -1;
                return;
            }

            if (TagCombo.SelectedItem is TagDisplayItem item &&
                vm.TryAddTag(item.DisplayName, item.OriginalObject))
            {
                TagCombo.SelectedIndex = -1;
                TagCombo.Text = string.Empty;
            }

            RefreshTagSuggestions();
        }

        private void RemoveTag_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm &&
                (sender as Button)?.Tag is GeoTTagModel item)
            {
                vm.RemoveTag(item);
            }
        }

        private class TagDisplayItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public Dictionary<string, string>? OriginalObject { get; set; }
        }
    }
}
