using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
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
        IsUpdateProgressIndeterminate = true;
        UpdateProgress = 0;
    }

    /// <summary>Runs once per launch when the user enabled automatic checks; failures stay silent.</summary>
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
                if (!silent) MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {result.ErrorMessage}");
                return;
            }

            RefreshLibrary();

            string summary = EpmaLocalizer.Format("epma_update_list_synced", result.NotInstalledCount, result.OutdatedCount);
            if (result.RemovalCount > 0) summary += " " + EpmaLocalizer.Format("epma_update_removed", result.RemovalCount);
            if (result.RequiresAppUpgradeCount > 0) summary += " " + EpmaLocalizer.Format("epma_update_needs_app", result.RequiresAppUpgradeCount);

            if (result.ListDownloaded || result.CatalogChanged)
            {
                if (silent) MessageHelper.Info(summary); else MessageHelper.Success(summary);
            }
            else if (!silent)
            {
                if (result.HasPendingActions || result.RequiresAppUpgradeCount > 0) MessageHelper.Info(summary);
                else MessageHelper.Info(EpmaLocalizer.Format("epma_update_already_latest"));
            }
        }
        catch (Exception ex)
        {
            if (!silent) MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {ex.Message}");
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
        var entries = EpmaUpdateService.GetNotInstalledEntries().Concat(EpmaUpdateService.GetOutdatedEntries())
            .Where(e => e.FormatVersion <= EpmaScriptPackage.CurrentFormatVersion)
            .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        if (entries.Count == 0) { RefreshLibrary(); return; }

        if (_libraryItems.Any(i => i.IsUpdateAvailable) && !await MessageHelper.ShowAsyncDialog(
                EpmaLocalizer.Format("epma_update_overwrite_all_confirm", entries.Count),
                EpmaLocalizer.Format("Cancel"), EpmaLocalizer.Format("Confirm"))) return;

        IsUpdateBusy = true;
        ShowUpdateOverlay(false, EpmaLocalizer.Format("epma_update_downloading"));
        try
        {
            var progress = new Progress<(int current, int total, string name)>(p =>
            {
                UpdateProgress = p.total == 0 ? 0 : (p.current - 1) * 100.0 / p.total;
                UpdateStatusText = $"{p.current}/{p.total}  {p.name}";
            });
            var result = await EpmaUpdateService.DownloadAndReloadAsync(entries, progress);
            UpdateProgress = 100;
            RefreshLibrary();
            ReportBatchResult(result);
        }
        catch (Exception ex) { MessageHelper.Error($"{EpmaLocalizer.Format("epma_update_check_failed")}: {ex.Message}"); }
        finally
        {
            HideUpdateOverlay();
            IsUpdateBusy = false;
        }
    }

    private static void ReportBatchResult(EpmaBatchDownloadResult result)
    {
        var message = new StringBuilder(EpmaLocalizer.Format("epma_update_download_done", result.SuccessCount));
        foreach (var failure in result.Failures.Take(10))
            message.Append('\n').Append(EpmaLocalizer.Format("epma_update_download_failed", failure.PackageId, failure.ErrorMessage));
        if (result.Failures.Count > 10) message.Append("\n...");

        if (result.Failures.Count == 0) MessageHelper.Success(message.ToString());
        else if (result.SuccessCount == 0) MessageHelper.Error(message.ToString());
        else MessageHelper.Warning(message.ToString());
    }
}
