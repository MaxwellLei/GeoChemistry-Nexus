using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GeoChemistryNexus.Controls.GeothermometerEditorPanels
{
    public partial class GeoTBasicInfoPanel : UserControl
    {
        public GeoTBasicInfoPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 刷新标签与能力下拉建议。可显式传入 ViewModel，避免窗口构造时 DataContext 绑定尚未生效。
        /// </summary>
        public void RefreshTagSuggestions(GeothermometerEditorViewModel? viewModel = null)
        {
            var vm = viewModel ?? DataContext as GeothermometerEditorViewModel;
            if (vm == null) return;

            // 下拉打开时不替换 ItemsSource，避免 ComboBoxItem 脱离可视树时触发绑定错误
            if (!TagCombo.IsDropDownOpen)
                TagCombo.ItemsSource = BuildTagSuggestionItems(vm);

            if (!CapabilityCombo.IsDropDownOpen)
                CapabilityCombo.ItemsSource = vm.GetCapabilitySuggestions();
        }

        private static List<TagDisplayItem> BuildTagSuggestionItems(GeothermometerEditorViewModel vm)
        {
            return vm.GetTagSuggestions()
                .Select(entry =>
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
                })
                .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 先关闭下拉再延后刷新 ItemsSource，避免选择瞬间销毁 ComboBoxItem 引发绑定错误。
        /// </summary>
        private void ScheduleSuggestionRefresh(GeothermometerEditorViewModel vm, ComboBox combo)
        {
            combo.IsDropDownOpen = false;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                combo.SelectedIndex = -1;
                combo.Text = string.Empty;
                RefreshTagSuggestions(vm);
            }), DispatcherPriority.Background);
        }

        private void TagCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is GeothermometerEditorViewModel vm &&
                vm.TryAddTag(TagCombo.Text?.Trim() ?? string.Empty))
            {
                ScheduleSuggestionRefresh(vm, TagCombo);
            }
            e.Handled = true;
        }

        private void TagCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not GeothermometerEditorViewModel vm) return;
            if (!TagCombo.IsDropDownOpen)
            {
                if (TagCombo.SelectedIndex >= 0)
                    TagCombo.SelectedIndex = -1;
                return;
            }

            if (TagCombo.SelectedItem is TagDisplayItem item &&
                vm.TryAddTag(item.DisplayName, item.OriginalObject))
            {
                ScheduleSuggestionRefresh(vm, TagCombo);
            }
        }

        private void RemoveTag_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm &&
                (sender as Button)?.Tag is GeoTTagModel item)
            {
                vm.RemoveTag(item);
                RefreshTagSuggestions(vm);
            }
        }

        private void CapabilityCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is GeothermometerEditorViewModel vm &&
                vm.TryAddCapability(CapabilityCombo.Text?.Trim() ?? string.Empty))
            {
                ScheduleSuggestionRefresh(vm, CapabilityCombo);
            }
            e.Handled = true;
        }

        private void CapabilityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not GeothermometerEditorViewModel vm) return;
            if (!CapabilityCombo.IsDropDownOpen)
            {
                if (CapabilityCombo.SelectedIndex >= 0)
                    CapabilityCombo.SelectedIndex = -1;
                return;
            }

            if (CapabilityCombo.SelectedItem is string capability &&
                vm.TryAddCapability(capability))
            {
                ScheduleSuggestionRefresh(vm, CapabilityCombo);
            }
        }

        private void RemoveCapability_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm &&
                (sender as Button)?.Tag is GeoTCapabilityModel item)
            {
                vm.RemoveCapability(item);
                RefreshTagSuggestions(vm);
            }
        }

        private class TagDisplayItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public Dictionary<string, string>? OriginalObject { get; set; }
        }
    }
}
