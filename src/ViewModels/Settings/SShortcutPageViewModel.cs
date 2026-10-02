using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Services;
using System.ComponentModel;
using System.Collections.Generic;

namespace GeoChemistryNexus.ViewModels
{
    public partial class ShortcutItem : ObservableObject
    {
        private readonly string _nameKey;
        private readonly string? _keysResourceKey;
        private readonly string _descriptionKey;

        public string Name => LanguageService.Instance[_nameKey];
        public string Keys => _keysResourceKey == null ? _literalKeys : LanguageService.Instance[_keysResourceKey];
        public string Description => LanguageService.Instance[_descriptionKey];
        private readonly string _literalKeys;

        public ShortcutItem(string nameKey, string keys, string descriptionKey)
        {
            _nameKey = nameKey;
            _literalKeys = keys;
            _keysResourceKey = null;
            _descriptionKey = descriptionKey;

            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
        }

        public ShortcutItem(string nameKey, string keysResourceKey, string descriptionKey, bool useResourceKeyForKeys)
        {
            _nameKey = nameKey;
            _literalKeys = string.Empty;
            _keysResourceKey = useResourceKeyForKeys ? keysResourceKey : null;
            _descriptionKey = descriptionKey;

            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Keys));
                OnPropertyChanged(nameof(Description));
            }
        }
    }

    public partial class ShortcutGroup : ObservableObject
    {
        private readonly string _titleKey;
        private readonly string _descriptionKey;

        public string Title => LanguageService.Instance[_titleKey];
        public string Description => LanguageService.Instance[_descriptionKey];
        public IReadOnlyList<ShortcutItem> Items { get; }

        public ShortcutGroup(string titleKey, string descriptionKey, IReadOnlyList<ShortcutItem> items)
        {
            _titleKey = titleKey;
            _descriptionKey = descriptionKey;
            Items = items;

            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Description));
            }
        }
    }

    public partial class SShortcutPageViewModel : ObservableObject
    {
        public IReadOnlyList<ShortcutGroup> ShortcutGroups { get; }

        public SShortcutPageViewModel()
        {
            ShortcutGroups = new List<ShortcutGroup>
            {
                new ShortcutGroup(
                    "shortcut_group_plot_title",
                    "shortcut_group_plot_desc",
                    new List<ShortcutItem>
                    {
                        new ShortcutItem("shortcut_action_undo_name", "Ctrl+Z", "shortcut_action_undo_desc"),
                        new ShortcutItem("shortcut_action_redo_name", "Ctrl+Y", "shortcut_action_redo_desc"),
                        new ShortcutItem("shortcut_action_delete_selected_name", "Delete", "shortcut_action_delete_selected_desc"),
                        new ShortcutItem("shortcut_action_center_view_name", "Ctrl+0 / Ctrl+NumPad0", "shortcut_action_center_view_desc"),
                        new ShortcutItem("shortcut_action_save_plot_name", "Ctrl+S", "shortcut_action_save_plot_desc"),
                        new ShortcutItem("shortcut_action_switch_diagram_mode_name", "Alt+1", "shortcut_action_switch_diagram_mode_desc"),
                        new ShortcutItem("shortcut_action_switch_data_mode_name", "Alt+2", "shortcut_action_switch_data_mode_desc"),
                        new ShortcutItem("shortcut_action_switch_edit_mode_name", "Alt+3", "shortcut_action_switch_edit_mode_desc"),
                        new ShortcutItem("shortcut_action_cycle_plot_mode_name", "Ctrl+Tab", "shortcut_action_cycle_plot_mode_desc"),
                        new ShortcutItem("shortcut_action_middle_click_plot_mode_name", "shortcut_action_middle_click_plot_mode_keys", "shortcut_action_middle_click_plot_mode_desc", true),
                        new ShortcutItem("shortcut_action_plot_data_name", "Ctrl+Shift+Enter", "shortcut_action_plot_data_desc")
                    }),
                new ShortcutGroup(
                    "shortcut_group_add_title",
                    "shortcut_group_add_desc",
                    new List<ShortcutItem>
                    {
                        new ShortcutItem("shortcut_action_add_line_name", "Ctrl+1 / Ctrl+NumPad1", "shortcut_action_add_line_desc"),
                        new ShortcutItem("shortcut_action_add_text_name", "Ctrl+2 / Ctrl+NumPad2", "shortcut_action_add_text_desc"),
                        new ShortcutItem("shortcut_action_add_polygon_name", "Ctrl+3 / Ctrl+NumPad3", "shortcut_action_add_polygon_desc"),
                        new ShortcutItem("shortcut_action_add_arrow_name", "Ctrl+4 / Ctrl+NumPad4", "shortcut_action_add_arrow_desc"),
                        new ShortcutItem("shortcut_action_add_function_name", "Ctrl+5 / Ctrl+NumPad5", "shortcut_action_add_function_desc")
                    }),
                new ShortcutGroup(
                    "shortcut_group_template_title",
                    "shortcut_group_template_desc",
                    new List<ShortcutItem>
                    {
                        new ShortcutItem("shortcut_action_quick_favorite_name", "shortcut_action_quick_favorite_keys", "shortcut_action_quick_favorite_desc", true),
                        new ShortcutItem("shortcut_action_quick_delete_name", "shortcut_action_quick_delete_keys", "shortcut_action_quick_delete_desc", true)
                    }),
                new ShortcutGroup(
                    "shortcut_group_rich_text_title",
                    "shortcut_group_rich_text_desc",
                    new List<ShortcutItem>
                    {
                        new ShortcutItem("shortcut_action_bold_name", "Ctrl+B", "shortcut_action_bold_desc"),
                        new ShortcutItem("shortcut_action_italic_name", "Ctrl+I", "shortcut_action_italic_desc"),
                        new ShortcutItem("shortcut_action_underline_name", "Ctrl+U", "shortcut_action_underline_desc")
                    }),
                new ShortcutGroup(
                    "shortcut_group_notification_title",
                    "shortcut_group_notification_desc",
                    new List<ShortcutItem>
                    {
                        new ShortcutItem("shortcut_action_close_notification_name", "shortcut_action_close_notification_keys", "shortcut_action_close_notification_desc", true)
                    })
            };
        }
    }
}
