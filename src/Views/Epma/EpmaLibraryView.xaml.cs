using System.Windows.Controls;

namespace GeoChemistryNexus.Views;

using System.Windows;
using System.Windows.Input;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Services.Epma;
using GeoChemistryNexus.Helpers;

public partial class EpmaLibraryView : UserControl
{
    public EpmaLibraryView()
    {
        InitializeComponent();
        NewCalculationButton.Command = LibraryEmptyState.CreateCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => OpenEditor(null));
        // The catalog is shared (settings can reset it), so re-read the cached snapshot whenever the library is shown.
        Loaded += (_, _) => (DataContext as EpmaPageViewModel)?.RefreshLibrary();
    }
    private void CreateTemplateClick(object sender, System.Windows.RoutedEventArgs e)
    {
        OpenEditor(null);
    }
    private void OpenEditor(EpmaLibraryItem? item)
    {
        if (DataContext is not EpmaPageViewModel viewModel || !viewModel.CanManageTemplates) return;
        EpmaInstalledTemplate? original = null;
        if (item is not null && !viewModel.TryLoadTemplate(item, out original)) return;
        var editor = new EpmaTemplateEditorWindow(original) { Owner = Window.GetWindow(this) };
        editor.ShowDialog();
        if (editor.Editor.CreatedTemplate is { } template) viewModel.AcceptSavedTemplate(template, item is null);
    }
    private void TemplateRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem container) { container.IsSelected = true; container.Focus(); }
    }
    private void TemplateContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not ListBoxItem { DataContext: EpmaLibraryItem item } container || DataContext is not EpmaPageViewModel viewModel) return;
        container.IsSelected = true;
        var menu = new ContextMenu();
        var favoriteEntry = new MenuItem { Header = EpmaLocalizer.Format(viewModel.IsFavorite(item) ? "epma_library_favorite_remove" : "epma_library_favorite_add") };
        favoriteEntry.Click += async (_, _) => await viewModel.ToggleFavorite(item);
        menu.Items.Add(favoriteEntry);
        menu.Items.Add(new Separator());
        foreach (var action in EpmaTemplateMenuPolicy.Actions(item.IsCustom, AppCultureRegistry.IsDeveloperModeEnabled()))
        {
            string key = action switch
            {
                EpmaTemplateAction.Edit => "epma_library_edit", EpmaTemplateAction.Delete => "epma_library_delete",
                EpmaTemplateAction.Export => "epma_library_export", EpmaTemplateAction.PromoteOfficial => "epma_library_promote",
                _ => "epma_library_remove_official"
            };
            // Not-yet-downloaded official templates have no package to edit, export or demote.
            var entry = new MenuItem { Header = EpmaLocalizer.Format(key), IsEnabled = item.IsInstalled && viewModel.CanManageTemplates };
            if (action == EpmaTemplateAction.Delete) entry.Tag = "Danger";
            if (action == EpmaTemplateAction.PromoteOfficial) entry.Click += async (_, _) => await viewModel.PromoteLibraryTemplateAsync(item);
            if (action == EpmaTemplateAction.RemoveOfficial) entry.Click += async (_, _) => await viewModel.DemoteLibraryTemplateAsync(item);
            if (action == EpmaTemplateAction.Edit) entry.Click += (_, _) => OpenEditor(item);
            if (action == EpmaTemplateAction.Delete) entry.Click += async (_, _) => await viewModel.DeleteLibraryTemplateAsync(item);
            if (action == EpmaTemplateAction.Export) entry.Click += async (_, _) => await viewModel.ExportLibraryTemplateAsync(item, Window.GetWindow(this));
            menu.Items.Add(entry);
        }
        container.ContextMenu = menu;
    }
}
