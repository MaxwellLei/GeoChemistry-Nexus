using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services.Epma;
namespace GeoChemistryNexus.ViewModels;

/// <summary>Card model over a catalog record; the package itself is only read when a template is opened.</summary>
public sealed class EpmaLibraryItem
{
    public EpmaLibraryItem(EpmaTemplateEntity entity)
    {
        Entity = entity;
        Details = EpmaTemplateDetails.FromEntity(entity);
    }
    public EpmaTemplateEntity Entity { get; }
    public EpmaTemplateDetails Details { get; }
    public string Id => Entity.PackageId;
    public bool IsCustom => Entity.IsCustom;
    public bool IsFavorite => Entity.IsFavorite;
    public bool IsGeneral => Id.Equals(EpmaTemplateCatalog.GeneralPackageId, StringComparison.OrdinalIgnoreCase);
    public string Name => Details.Name;
    public string Version => Details.Version;
    public string Description
    {
        get
        {
            string introduction = DetailDescription.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return introduction.Length > 160 ? introduction[..160] + "…" : introduction;
        }
    }
    public string DetailDescription => Details.DetailDescription;
    public string Origin => EpmaLocalizer.Format(IsCustom ? "epma_library_personal" : "epma_library_official");
    /// <summary>Short source label ("Online" / "My") shown next to entries in the recent list.</summary>
    public string RecentOrigin => EpmaLocalizer.Format(IsCustom ? "epma_library_recent_my" : "epma_library_recent_online");
    public IReadOnlyList<string> Tags => Details.Tags;
    public string Author => Details.Author;
    public string UpdateDate => Details.UpdateDate;

    /// <summary>Only templates whose package is stored locally can be opened, edited or exported.</summary>
    public bool IsInstalled => Entity.IsInstalled;
    public bool IsNotInstalled => Entity.IsNotInstalled;
    public bool IsUpdateAvailable => Entity.IsUpdateAvailable;
    public bool RequiresAppUpgrade => Entity.RequiresAppUpgrade;
    /// <summary>Can be fetched or refreshed from the server with the current app version.</summary>
    public bool NeedsDownload => IsNotInstalled || IsUpdateAvailable;
    public bool HasStatus => NeedsDownload || RequiresAppUpgrade;
    public string StatusText => EpmaLocalizer.Format(IsNotInstalled ? "epma_status_not_downloaded"
        : IsUpdateAvailable ? "epma_status_update_available"
        : RequiresAppUpgrade ? (IsInstalled ? "status_update_requires_app_upgrade" : "status_requires_app_upgrade")
        : "epma_library_official");
    /// <summary>Longer explanation shown in the detail panel when the server version needs a newer app.</summary>
    public bool HasUpgradeHint => RequiresAppUpgrade;
    public string UpgradeHintText => EpmaLocalizer.Format(IsInstalled ? "status_update_requires_app_upgrade_hint" : "status_requires_app_upgrade_hint");
    public string DownloadActionText => EpmaLocalizer.Format(IsNotInstalled ? "epma_update_download" : "epma_update_update");
}
public partial class EpmaPageViewModel
{
    private EpmaLibraryItem[] _libraryItems = [];
    [ObservableProperty][NotifyPropertyChangedFor(nameof(CanManageTemplates))] private bool _isLibraryMutationBusy;
    public bool CanManageTemplates => !IsLibraryMutationBusy && !IsUpdateBusy && !ImportTemplateCommand.IsRunning && !ReloadTemplatesCommand.IsRunning;
    public ObservableCollection<EpmaLibraryItem> FilteredLibraryItems { get; } = new();
    public ObservableCollection<EpmaLibraryItem> RecentLibraryItems { get; } = new();
    private readonly EpmaRecentTemplates _recentTemplates = new(EpmaCompositionRoot.HistoryDirectory);
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRecommendedLibrary))]
    private bool _isMyLibrary;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRecommendedLibrary))]
    private bool _isFavoriteLibrary;
    public bool IsRecommendedLibrary { get => !IsMyLibrary && !IsFavoriteLibrary; set { if (value) { IsMyLibrary = false; IsFavoriteLibrary = false; } } }
    public int RecommendedCount => _libraryItems.Count(i => !i.IsCustom);
    public bool HasRecentTemplates => RecentLibraryItems.Count > 0;
    private bool IsPlainEmptyList => string.IsNullOrWhiteSpace(LibrarySearchText) && (IsMyLibrary || IsFavoriteLibrary);
    public string LibraryEmptyMessage => EpmaLocalizer.Format(IsPlainEmptyList ? (IsFavoriteLibrary ? "epma_library_favorites_empty" : "epma_library_my_empty") : "epma_library_no_matches");
    public bool ShowLibraryEmptyContact => !IsPlainEmptyList;
    public bool ShowLibraryEmptyActions => IsPlainEmptyList && IsMyLibrary;
    public bool IsFavorite(EpmaLibraryItem item) => item.IsFavorite;
    public async Task ToggleFavorite(EpmaLibraryItem item)
    {
        try
        {
            await EpmaCompositionRoot.Templates.SetFavoriteAsync(item.Entity, !item.IsFavorite);
            RefreshLibrary();
        }
        catch (Exception ex) { MessageHelper.Warning(EpmaLocalizer.Format("epma_library_failed") + " " + ex.Message); }
    }
    private bool _refreshingLibrary;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EnterCalculationCommand))]
    private EpmaLibraryItem? _selectedTemplate;
    [ObservableProperty] private string _librarySearchText = "";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsCalculationOpen))] private bool _isLibraryOpen = true;
    private readonly HashSet<EpmaPackageIssue> _reportedLibraryIssues = [];
    public EpmaCalculationViewModel Workbench { get; }
    public bool IsCalculationOpen => !IsLibraryOpen;
    public bool HasLibraryMatches => FilteredLibraryItems.Count > 0;
    partial void OnLibrarySearchTextChanged(string value) => FilterLibrary();
    partial void OnIsMyLibraryChanged(bool value) { if (value && IsFavoriteLibrary) IsFavoriteLibrary = false; FilterLibrary(); }
    partial void OnIsFavoriteLibraryChanged(bool value) { if (value && IsMyLibrary) IsMyLibrary = false; FilterLibrary(); }

    public void RefreshLibrary()
    {
        var selected = SelectedTemplate;
        _refreshingLibrary = true;
        try
        {
            var snapshot = EpmaCompositionRoot.Templates.Current;
            _libraryItems = snapshot.OfficialTemplates.Concat(snapshot.CustomTemplates).Select(entity => new EpmaLibraryItem(entity)).ToArray();
            SelectedTemplate = _libraryItems.FirstOrDefault(i => SameTemplate(i, selected));
            RecentLibraryItems.Clear();
            foreach (var recent in _recentTemplates.Items)
            {
                var item = _libraryItems.FirstOrDefault(i => i.Id.Equals(recent.Id, StringComparison.OrdinalIgnoreCase) && i.IsCustom == recent.IsCustom);
                if (item is not null) RecentLibraryItems.Add(item);
            }
            OnPropertyChanged(nameof(HasRecentTemplates));
            OnPropertyChanged(nameof(RecommendedCount));
            NotifyPendingUpdatesChanged();
        }
        finally { _refreshingLibrary = false; }
        FilterLibrary();
        ReportLibraryIssues();
    }
    public void ReportLibraryIssues()
    {
        // Catalog inspection also runs in headless acceptance tools; only a UI host can show notifications.
        if (System.Windows.Application.Current is null) return;
        var issues = EpmaCompositionRoot.Templates.Current.Issues;
        _reportedLibraryIssues.IntersectWith(issues);
        var fresh = issues.Where(issue => _reportedLibraryIssues.Add(issue)).ToArray();
        if (fresh.Length > 0) MessageHelper.Warning(string.Join("\n", fresh.Select(issue => issue.File + ": " + issue.Message)));
    }
    private void FilterLibrary()
    {
        if (_refreshingLibrary) return;
        string query = LibrarySearchText.Trim();
        bool favorites = IsFavoriteLibrary;
        var items = _libraryItems.Where(i => favorites ? IsFavorite(i) : i.IsCustom == IsMyLibrary)
            .Where(i => query.Length == 0 || (i.Name + " " + i.Id + " " + i.DetailDescription + " " + string.Join(" ", i.Tags)).Contains(query, StringComparison.CurrentCultureIgnoreCase));
        items = items.OrderByDescending(i => i.IsGeneral).ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase);
        var selected = SelectedTemplate; FilteredLibraryItems.Clear(); foreach (var item in items) FilteredLibraryItems.Add(item);
        SelectedTemplate = FilteredLibraryItems.FirstOrDefault(i => SameTemplate(i, selected)) ?? FilteredLibraryItems.FirstOrDefault();
        OnPropertyChanged(nameof(HasLibraryMatches));
        OnPropertyChanged(nameof(LibraryEmptyMessage));
        OnPropertyChanged(nameof(ShowLibraryEmptyContact));
        OnPropertyChanged(nameof(ShowLibraryEmptyActions));
    }
    private static bool SameTemplate(EpmaLibraryItem item, EpmaLibraryItem? selected) => selected is not null && item.Id.Equals(selected.Id, StringComparison.OrdinalIgnoreCase) && item.IsCustom == selected.IsCustom;

    /// <summary>Opens the stored package of a card; shows the error and refreshes when the record is stale or has no package.</summary>
    public bool TryLoadTemplate(EpmaLibraryItem item, out EpmaInstalledTemplate? template)
    {
        if (EpmaCompositionRoot.Templates.TryLoadInstalled(item.Entity, out template, out string? error)) return true;
        MessageHelper.Error(EpmaLocalizer.Format("epma_library_failed") + " " + error);
        RefreshLibrary();
        return false;
    }
    public void AcceptCreatedTemplate(EpmaInstalledTemplate template)
        => AcceptSavedTemplate(template, true);
    public void AcceptSavedTemplate(EpmaInstalledTemplate template, bool isNew)
    {
        IsFavoriteLibrary = false; IsMyLibrary = template.IsCustom; LibrarySearchText = ""; RefreshLibrary();
        SelectedTemplate = FilteredLibraryItems.Single(i => i.Id.Equals(template.Package.Manifest.PackageId, StringComparison.OrdinalIgnoreCase));
        MessageHelper.Success(EpmaLocalizer.Format(isNew ? "epma_library_created" : "epma_library_updated", SelectedTemplate.Name));
    }
    public async System.Threading.Tasks.Task DeleteLibraryTemplateAsync(EpmaLibraryItem item)
    {
        if (!item.IsCustom || !CanManageTemplates) return;
        IsLibraryMutationBusy = true;
        try
        {
            if (!await MessageHelper.ShowAsyncDialog(EpmaLocalizer.Format("epma_library_delete_confirm", item.Name), EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("epma_library_delete"))) return;
            // 不解析包：高版本（本程序无法读取）的个人模板也必须能删除
            await EpmaCompositionRoot.Templates.DeleteAsync(item.Entity);
            try { _recentTemplates.Remove(item.Id, true); }
            catch (Exception ex) { MessageHelper.Warning(EpmaLocalizer.Format("epma_library_history_failed") + " " + ex.Message); }
            RefreshLibrary(); MessageHelper.Success(EpmaLocalizer.Format("epma_library_deleted", item.Name));
        }
        catch (Exception ex) { MessageHelper.Error(EpmaLocalizer.Format("epma_library_failed") + " " + ex.Message); }
        finally { IsLibraryMutationBusy = false; }
    }
    public async System.Threading.Tasks.Task ExportLibraryTemplateAsync(EpmaLibraryItem item, System.Windows.Window? owner)
    {
        if (!CanManageTemplates) return;
        IsLibraryMutationBusy = true;
        try
        {
            if (!TryLoadTemplate(item, out var template)) return;
            string? path = await FileHelper.GetSaveFilePath2Async(EpmaLocalizer.Format("epma_library_export"), "EPMA template package|*" + EpmaScriptPackage.Extension, "zip", item.Id + EpmaScriptPackage.Extension, owner);
            if (string.IsNullOrEmpty(path)) return;
            path = TemplatePackageFileExtensions.WithPrimarySuffix(path, EpmaScriptPackage.Extension);
            await EpmaCompositionRoot.Templates.ExportAsync(template!, path);
            MessageHelper.ExportSuccess(EpmaLocalizer.Format("epma_library_exported", item.Name), path, owner);
        }
        catch (Exception ex) { MessageHelper.Error(EpmaLocalizer.Format("epma_library_failed") + " " + ex.Message); }
        finally { IsLibraryMutationBusy = false; }
    }

    /// <summary>Developer action: a personal template becomes an official (publishable) one.</summary>
    public async System.Threading.Tasks.Task PromoteLibraryTemplateAsync(EpmaLibraryItem item)
    {
        if (!item.IsCustom || !CanManageTemplates || !AppCultureRegistry.IsDeveloperModeEnabled()) return;
        IsLibraryMutationBusy = true;
        try
        {
            // 标识的随机部分不变，只把前缀换成 official_
            string officialId = EpmaTemplateCatalog.GetConvertedPackageId(item.Id, true);
            string message = EpmaLocalizer.Format("epma_library_promote_confirm", item.Name);
            if (EpmaCompositionRoot.Templates.Exists(officialId, true)) message += "\n" + EpmaLocalizer.Format("epma_library_promote_replace", officialId);
            if (!await MessageHelper.ShowAsyncDialog(message, EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("epma_library_promote"))) return;
            await EpmaCompositionRoot.Templates.PromoteToOfficialAsync(item.Entity);
            try { _recentTemplates.Remove(item.Id, true); }
            catch (Exception ex) { MessageHelper.Warning(EpmaLocalizer.Format("epma_library_history_failed") + " " + ex.Message); }
            ShowTemplateAfterMutation(officialId, false);
            MessageHelper.Success(EpmaLocalizer.Format("epma_library_promoted", item.Name));
        }
        catch (Exception ex) { MessageHelper.Error(EpmaLocalizer.Format("epma_library_failed") + " " + ex.Message); }
        finally { IsLibraryMutationBusy = false; }
    }

    /// <summary>Developer action: an installed official template becomes a personal one and leaves the official catalog.</summary>
    public async System.Threading.Tasks.Task DemoteLibraryTemplateAsync(EpmaLibraryItem item)
    {
        if (item.IsCustom || !item.IsInstalled || !CanManageTemplates || !AppCultureRegistry.IsDeveloperModeEnabled()) return;
        IsLibraryMutationBusy = true;
        try
        {
            // 标识的随机部分不变，只把前缀换成 custom_
            string customId = EpmaTemplateCatalog.GetConvertedPackageId(item.Id, false);
            string message = EpmaLocalizer.Format("epma_library_demote_confirm", item.Name);
            if (EpmaCompositionRoot.Templates.Exists(customId, false)) message += "\n" + EpmaLocalizer.Format("epma_library_demote_replace", customId);
            if (!await MessageHelper.ShowAsyncDialog(message, EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("epma_library_remove_official"))) return;
            await EpmaCompositionRoot.Templates.DemoteToCustomAsync(item.Entity);
            try { _recentTemplates.Remove(item.Id, false); }
            catch (Exception ex) { MessageHelper.Warning(EpmaLocalizer.Format("epma_library_history_failed") + " " + ex.Message); }
            // 移除在线模板后不跳转，停留在当前列表
            RefreshLibrary();
            MessageHelper.Success(EpmaLocalizer.Format("epma_library_demoted", item.Name));
        }
        catch (Exception ex) { MessageHelper.Error(EpmaLocalizer.Format("epma_library_failed") + " " + ex.Message); }
        finally { IsLibraryMutationBusy = false; }
    }
    private void ShowTemplateAfterMutation(string packageId, bool isCustom)
    {
        IsFavoriteLibrary = false; IsMyLibrary = isCustom; LibrarySearchText = ""; RefreshLibrary();
        SelectedTemplate = FilteredLibraryItems.FirstOrDefault(i => i.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase) && i.IsCustom == isCustom) ?? SelectedTemplate;
    }
    /// <summary>左侧资源库面板切换来源：Recommended / Mine / Favorite。</summary>
    [RelayCommand]
    private void SelectLibrarySource(string? source)
    {
        switch (source)
        {
            case "Mine": IsMyLibrary = true; break;
            case "Favorite": IsFavoriteLibrary = true; break;
            default: IsRecommendedLibrary = true; break;
        }
    }
    [RelayCommand]
    private void OpenRecentTemplate(EpmaLibraryItem? item)
    {
        if (item is null || Workbench.IsBusy) return;
        IsFavoriteLibrary = false; IsMyLibrary = item.IsCustom; LibrarySearchText = "";
        SelectedTemplate = FilteredLibraryItems.FirstOrDefault(i => SameTemplate(i, item));
        if (CanEnterCalculation()) EnterCalculation();
    }
    private bool CanEnterCalculation() => SelectedTemplate is { IsInstalled: true } && !Workbench.IsBusy && !IsUpdateBusy;
    [RelayCommand(CanExecute = nameof(CanEnterCalculation))]
    private void EnterCalculation()
    {
        if (SelectedTemplate is not { } selected) return;
        if (!TryLoadTemplate(selected, out var template)) return;
        Workbench.Activate(template!.Package);
        try { _recentTemplates.Record(selected.Id, selected.IsCustom); }
        catch (Exception ex) { MessageHelper.Warning(EpmaLocalizer.Format("epma_library_history_failed") + " " + ex.Message); }
        IsLibraryOpen = false;
        EnterCalculationCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand] private void BackToLibrary() { Workbench.Cancel(); IsLibraryOpen = true; RefreshLibrary(); EnterCalculationCommand.NotifyCanExecuteChanged(); }
    [RelayCommand]
    private async System.Threading.Tasks.Task ReloadTemplates()
    {
        try { await System.Threading.Tasks.Task.Run(EpmaCompositionRoot.Templates.Reload); RefreshLibrary(); }
        catch (Exception ex) { MessageHelper.Error(EpmaLocalizer.Format("epma_library_failed") + " " + ex.Message); }
    }
    [RelayCommand(IncludeCancelCommand = true)]
    private async System.Threading.Tasks.Task ImportTemplate(CancellationToken cancellationToken)
    {
        string? path = await FileHelper.GetFilePathAsync(FileDialogFilterHelper.ImportEpmaPackages);
        if (string.IsNullOrEmpty(path)) return;
        if (!TemplatePackageFileExtensions.IsEpmaPackagePath(path))
        {
            MessageHelper.Error($"{Path.GetFileName(path)}: {EpmaLocalizer.Format("file_drop_invalid_format")}");
            return;
        }
        await ImportTemplateFileAsync(path, cancellationToken);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ImportTemplateFromPaths(object? parameter)
    {
        var paths = ExtractDroppedTemplatePaths(parameter);
        if (paths.Count == 0 || IsLibraryMutationBusy) return;

        IsLibraryMutationBusy = true;
        try
        {
            foreach (string path in paths)
                await ImportTemplateFileAsync(path, CancellationToken.None);
        }
        finally
        {
            IsLibraryMutationBusy = false;
        }
    }

    private async System.Threading.Tasks.Task ImportTemplateFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var imported = await EpmaCompositionRoot.Templates.ImportAsync(path, (existing, incoming) =>
                MessageHelper.ShowAsyncDialog(EpmaLocalizer.Format("epma_package_replace_confirm", incoming.Manifest.PackageId,
                    EpmaScriptText.Format(existing.Package.Manifest.Name), existing.Package.Manifest.Version,
                    EpmaScriptText.Format(incoming.Manifest.Name), incoming.Manifest.Version),
                    EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("epma_package_replace")), cancellationToken);
            IsFavoriteLibrary = false;
            IsMyLibrary = imported.IsCustom;
            RefreshLibrary();
            LibrarySearchText = "";
            SelectedTemplate = FilteredLibraryItems.Single(i => i.Id.Equals(imported.Package.Manifest.PackageId, StringComparison.OrdinalIgnoreCase));
            MessageHelper.Success(EpmaLocalizer.Format("epma_library_imported", SelectedTemplate.Name));
        }
        catch (OperationCanceledException) { MessageHelper.Info(EpmaLocalizer.Format("epma_cancelled")); }
        catch (Exception ex) { MessageHelper.Error(EpmaLocalizer.Format("epma_library_failed") + " " + ex.Message); }
    }

    private static IReadOnlyList<string> ExtractDroppedTemplatePaths(object? parameter)
    {
        IEnumerable<string> paths = parameter switch
        {
            string single when !string.IsNullOrWhiteSpace(single) => new[] { single },
            string[] many => many,
            IEnumerable<string> many => many,
            _ => Array.Empty<string>()
        };

        var existing = paths
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .ToArray();

        var accepted = existing
            .Where(TemplatePackageFileExtensions.IsEpmaPackagePath)
            .ToArray();

        // 后缀不符（含其他模块的包）的文件逐个提示，避免静默忽略
        foreach (string rejected in existing.Except(accepted))
            MessageHelper.Error($"{Path.GetFileName(rejected)}: {EpmaLocalizer.Format("file_drop_invalid_format")}");

        return accepted;
    }
}
