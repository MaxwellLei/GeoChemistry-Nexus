using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;
namespace GeoChemistryNexus.ViewModels;

/// <summary>Server update flow of the EPMA library: check the catalog, download or update official packages.</summary>
public partial class EpmaPageViewModel
{
    private static bool _hasAutoCheckedUpdate;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanManageTemplates))] [NotifyCanExecuteChangedFor(nameof(EnterCalculationCommand))]
    private bool _isUpdateBusy;
    [ObservableProperty] private bool _isUpdateOverlayVisible;
    [ObservableProperty] private bool _isUpdateProgressIndeterminate = true;
    [ObservableProperty] private double _updateProgress;
    [ObservableProperty] private string _updateStatusText = "";
    [ObservableProperty] private string _updateOverlayTitle = "";
    /// <summary>批量下载 / 更新进行中，遮罩上显示“取消”按钮。</summary>
    [ObservableProperty] private bool _isUpdateCancelable;
    private CancellationTokenSource? _updateCts;

    [RelayCommand]
    private void CancelUpdate() => _updateCts?.Cancel();

    public int PendingUpdateCount => _libraryItems.Count(i => i.NeedsDownload);
    public bool HasPendingUpdates => PendingUpdateCount > 0;
    public string PendingUpdateText => EpmaLocalizer.Format("epma_update_download_all", PendingUpdateCount);
    private void NotifyPendingUpdatesChanged()
    {
        OnPropertyChanged(nameof(PendingUpdateCount));
        OnPropertyChanged(nameof(HasPendingUpdates));
        OnPropertyChanged(nameof(PendingUpdateText));
    }

    private void ShowUpdateOverlay(bool indeterminate, string title, string status = "", double progress = 0)
    {
        IsUpdateProgressIndeterminate = indeterminate;
        UpdateProgress = progress;
        UpdateOverlayTitle = title;
        UpdateStatusText = status;
        IsUpdateOverlayVisible = true;
    }
    private void HideUpdateOverlay()
    {
        IsUpdateOverlayVisible = false;
        IsUpdateCancelable = false;
        IsUpdateProgressIndeterminate = true;
        UpdateProgress = 0;
    }

    /// <summary>Runs once per launch when the user enabled automatic checks; failures are reported like the manual check (same as diagram and geothermobarometer).</summary>
    public async Task CheckUpdatesIfNeededAsync()
    {
        if (_hasAutoCheckedUpdate) return;
        // A missing key (older config files) means the default: enabled.
        if (bool.TryParse(ConfigHelper.GetConfig(ConfigKeys.AutoCheckEpmaUpdate), out bool autoCheck) && !autoCheck) return;
        _hasAutoCheckedUpdate = true;
        await CheckUpdatesCoreAsync(silent: true);
    }

    [RelayCommand] private Task CheckUpdates() => CheckUpdatesCoreAsync(silent: false);

    private async Task CheckUpdatesCoreAsync(bool silent)
    {
        if (IsUpdateBusy || Workbench.IsBusy) return;
        IsUpdateBusy = true;
        string checking = EpmaLocalizer.Format("epma_update_checking");
        if (!silent) ShowUpdateOverlay(true, checking);
        try
        {
            // 先不下载清单做一次检查：清单已是最新时只请求一次 index；清单有更新时再决定是否下载
            var result = await EpmaUpdateService.CheckForUpdatesAsync(downloadListIfOutdated: false);
            if (result.Status == EpmaUpdateCheckStatus.Success && result.ListOutdated)
            {
                if (!silent)
                {
                    HideUpdateOverlay();
                    if (!await MessageHelper.ShowAsyncDialog(EpmaLocalizer.Format("epma_update_new_list_confirm"),
                            EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("Confirm"))) return;
                    ShowUpdateOverlay(true, checking);
                }

                result = await EpmaUpdateService.CheckForUpdatesAsync();
            }

            if (result.Status == EpmaUpdateCheckStatus.Failed)
            {
                MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {result.ErrorMessage}");
                return;
            }

            RefreshLibrary();

            string summary = EpmaLocalizer.Format("epma_update_list_synced", result.NotInstalledCount, result.OutdatedCount);
            if (result.RemovalCount > 0) summary += " " + EpmaLocalizer.Format("epma_update_removed", result.RemovalCount);
            if (result.RequiresAppUpgradeCount > 0) summary += " " + EpmaLocalizer.Format("epma_update_needs_app", result.RequiresAppUpgradeCount);

            if (result.ListDownloaded || result.CatalogChanged)
            {
                // 自动检查：与图解一致，用轻量提示给出 新增 / 可更新 / 下架 数量
                if (silent)
                    MessageHelper.Info(string.Format(
                        LanguageService.GetString("template_list_sync_light_tip", "列表已更新，新增 {0} / 可更新 {1} / 下架 {2}"),
                        result.AddedCount, result.OutdatedCount, result.RemovalCount));
                else
                    MessageHelper.Success(summary);
            }
            else if (!silent)
            {
                if (result.HasPendingActions || result.RequiresAppUpgradeCount > 0) MessageHelper.Info(summary);
                else MessageHelper.Info(EpmaLocalizer.Format("epma_update_already_latest"));
            }
        }
        catch (Exception ex)
        {
            MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {ex.Message}");
        }
        finally
        {
            HideUpdateOverlay();
            IsUpdateBusy = false;
        }
    }

    /// <summary>Card / detail button: download a not-yet-installed official template or replace an outdated one.</summary>
    [RelayCommand]
    private async Task DownloadSelected(EpmaLibraryItem? item)
    {
        item ??= SelectedTemplate;
        if (item is null || item.IsCustom || !item.NeedsDownload || IsUpdateBusy || Workbench.IsBusy) return;
        if (item.IsUpdateAvailable && !await MessageHelper.ShowAsyncDialog(
                EpmaLocalizer.Format("epma_update_overwrite_confirm", item.Name),
                EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("Confirm"))) return;

        IsUpdateBusy = true;
        ShowUpdateOverlay(false, EpmaLocalizer.Format("epma_update_downloading"), item.Name);
        try
        {
            var entry = EpmaUpdateService.FindLocalListEntry(item.Id);
            if (entry == null)
            {
                var (fresh, error) = await EpmaUpdateService.FetchFreshEntryAsync(item.Id);
                if (fresh == null)
                {
                    MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {error ?? item.Id}");
                    return;
                }
                entry = fresh;
            }

            UpdateProgress = 30;
            var result = await EpmaUpdateService.DownloadWithRefreshAsync(entry);
            UpdateProgress = 100;
            RefreshLibrary();
            if (result.Success) MessageHelper.Success(EpmaLocalizer.Format("epma_update_download_done", 1));
            else MessageHelper.Error(EpmaLocalizer.Format("epma_update_download_failed", result.PackageId, result.ErrorMessage));
        }
        catch (Exception ex) { MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {ex.Message}"); }
        finally
        {
            HideUpdateOverlay();
            IsUpdateBusy = false;
        }
    }

    /// <summary>Toolbar button: download every missing official template and update the outdated ones.</summary>
    [RelayCommand]
    private async Task DownloadAllPending()
    {
        if (IsUpdateBusy || Workbench.IsBusy) return;
        static bool Downloadable(EpmaIndexEntry e) => !EpmaScriptPackage.RequiresAppUpgrade(e.Version, e.Requires);
        var newEntries = EpmaUpdateService.GetNotInstalledEntries().Where(Downloadable)
            .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        var outdatedEntries = EpmaUpdateService.GetOutdatedEntries().Where(Downloadable)
            .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        var entries = newEntries.Concat(outdatedEntries).GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        int incompatible = EpmaUpdateService.GetRequiresAppUpgradeCount();
        if (entries.Count == 0)
        {
            // 与图解一致：只剩需要升级软件的模板时给出提示
            if (incompatible > 0)
                MessageHelper.Warning(string.Format(
                    LanguageService.GetString("update_blocked_need_app_upgrade", "有 {0} 个模板有新版本，当前软件不兼容，请升级软件后再更新。"), incompatible));
            RefreshLibrary();
            return;
        }

        // 下载前总是确认一次（只有新模板时也一样，与图解、温压计一致）
        string confirmMessage = outdatedEntries.Count == 0
            ? string.Format(LanguageService.GetString("batch_download_confirm_message", "检测到 {0} 个未下载的新模板，是否批量下载？"), entries.Count)
            : newEntries.Count == 0
                ? EpmaLocalizer.Format("epma_update_overwrite_all_confirm", entries.Count)
                : string.Format(LanguageService.GetString("batch_download_update_all_confirm",
                    "检测到 {0} 个未下载的模板和 {1} 个有新版本的模板，是否全部下载 / 更新？更新会覆盖本地模板。"), newEntries.Count, outdatedEntries.Count);
        if (!await MessageHelper.ShowAsyncDialog(confirmMessage, EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("Confirm"))) return;

        _updateCts?.Dispose();
        _updateCts = new CancellationTokenSource();
        var token = _updateCts.Token;
        IsUpdateBusy = true;
        ShowUpdateOverlay(false, EpmaLocalizer.Format("epma_update_downloading"));
        IsUpdateCancelable = true;
        try
        {
            var progress = new Progress<(int current, int total, string name)>(p =>
            {
                UpdateProgress = p.total == 0 ? 0 : (p.current - 1) * 100.0 / p.total;
                UpdateStatusText = $"{p.current}/{p.total}  {p.name}";
            });
            var result = await EpmaUpdateService.DownloadAndReloadAsync(entries, progress, token);
            UpdateProgress = 100;
            RefreshLibrary();
            if (result.Cancelled)
            {
                if (result.SuccessCount > 0) MessageHelper.Success(EpmaLocalizer.Format("epma_update_download_done", result.SuccessCount));
                MessageHelper.Info(LanguageService.GetString(newEntries.Count == 0 ? "batch_update_cancelled" : "batch_download_cancelled", "已取消"));
            }
            else ReportBatchResult(result, incompatible);
        }
        catch (Exception ex) { MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {ex.Message}"); }
        finally
        {
            HideUpdateOverlay();
            IsUpdateBusy = false;
        }
    }

    /// <summary>Developer action: re-download one installed official template from the server, replacing local changes.</summary>
    public async Task ForceUpdateLibraryTemplateAsync(EpmaLibraryItem item)
    {
        if (item.IsCustom || !item.IsInstalled || IsUpdateBusy || Workbench.IsBusy || !AppCultureRegistry.IsDeveloperModeEnabled()) return;
        if (!await MessageHelper.ShowAsyncDialog(EpmaLocalizer.Format("epma_update_overwrite_confirm", item.Name),
                EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("Confirm"))) return;

        IsUpdateBusy = true;
        ShowUpdateOverlay(true, EpmaLocalizer.Format("epma_update_downloading"), item.Name);
        try
        {
            var (entry, error) = await EpmaUpdateService.FetchFreshEntryAsync(item.Id);
            if (entry == null)
            {
                MessageHelper.Error(error is null
                    ? EpmaLocalizer.Format("force_update_template_not_found")
                    : $"{EpmaLocalizer.Format("epma_update_check_failed")}: {error}");
                return;
            }

            var result = await EpmaUpdateService.DownloadAsync(entry);
            RefreshLibrary();
            if (result.Success) MessageHelper.Success(EpmaLocalizer.Format("epma_update_download_done", 1));
            else MessageHelper.Error(EpmaLocalizer.Format("epma_update_download_failed", result.PackageId, result.ErrorMessage));
        }
        catch (Exception ex) { MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {ex.Message}"); }
        finally
        {
            HideUpdateOverlay();
            IsUpdateBusy = false;
        }
    }

    private static void ReportBatchResult(EpmaBatchDownloadResult result, int incompatibleCount = 0)
    {
        var message = new StringBuilder(EpmaLocalizer.Format("epma_update_download_done", result.SuccessCount));
        // 与图解一致：汇总里提示因版本过高、需升级软件后才能更新的数量
        if (incompatibleCount > 0)
            message.Append('\n').Append(string.Format(
                LanguageService.GetString("update_incompatible_need_app_upgrade_count", "另有 {0} 个因版本过高，当前软件不兼容，请升级软件后再更新。"),
                incompatibleCount));
        foreach (var failure in result.Failures.Take(10))
            message.Append('\n').Append(EpmaLocalizer.Format("epma_update_download_failed", failure.PackageId, failure.ErrorMessage));
        if (result.Failures.Count > 10) message.Append("\n...");

        if (result.Failures.Count == 0 && incompatibleCount == 0) MessageHelper.Success(message.ToString());
        else if (result.Failures.Count == 0) MessageHelper.Warning(message.ToString());
        else if (result.SuccessCount == 0) MessageHelper.Error(message.ToString());
        else MessageHelper.Warning(message.ToString());
    }
}
