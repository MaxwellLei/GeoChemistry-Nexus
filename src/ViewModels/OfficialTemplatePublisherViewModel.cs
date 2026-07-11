using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Views.Widgets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public partial class OfficialTemplatePublisherViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isDeveloperMode;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isPublishing;

        [ObservableProperty]
        private double publishProgress;

        [ObservableProperty]
        private string publishProgressText = string.Empty;

        public bool IsNotBusy => !IsBusy;

        private int _publishExportStepCount;
        private int _publishExportStepIndex;

        private const double PublishExportPhaseStart = 5;
        private const double PublishExportPhaseEnd = 50;
        private const double PublishUploadPhaseStart = 50;
        private const double PublishUploadPhaseEnd = 95;

        [ObservableProperty]
        private bool publishDiagrams;

        [ObservableProperty]
        private bool publishGeothermometers;

        [ObservableProperty]
        private bool publishHomeLinks;

        [ObservableProperty]
        private bool publishAnnouncement;

        [ObservableProperty]
        private string announcementText = string.Empty;

        [ObservableProperty]
        private bool announcementHasRemoteChanges;

        [ObservableProperty]
        private string minimumSupportedVersionText = string.Empty;

        [ObservableProperty]
        private bool minimumSupportedVersionHasRemoteChanges;

        [ObservableProperty]
        private string latestAppVersionText = string.Empty;

        [ObservableProperty]
        private bool latestAppVersionHasRemoteChanges;

        [ObservableProperty]
        private int homeLinkGroupCount;

        [ObservableProperty]
        private int homeLinkCount;

        [ObservableProperty]
        private bool homeLinksHasRemoteChanges;

        [ObservableProperty]
        private HomeLinkGroupEditorViewModel? selectedHomeLinkGroup;

        [ObservableProperty]
        private HomeLinkEntryEditorViewModel? selectedHomeLink;

        [ObservableProperty]
        private string secretId = string.Empty;

        [ObservableProperty]
        private string secretKey = string.Empty;

        [ObservableProperty]
        private string region = OfficialContentEndpoints.DefaultRegion;

        [ObservableProperty]
        private string bucket = OfficialContentEndpoints.DefaultBucket;

        [ObservableProperty]
        private string stagingDirectory = string.Empty;

        [ObservableProperty]
        private string logText = string.Empty;

        [ObservableProperty]
        private int diagramNewCount;

        [ObservableProperty]
        private int diagramUpdatedCount;

        [ObservableProperty]
        private int diagramRemovedCount;

        [ObservableProperty]
        private int diagramUnchangedCount;

        [ObservableProperty]
        private int geoNewCount;

        [ObservableProperty]
        private int geoUpdatedCount;

        [ObservableProperty]
        private int geoRemovedCount;

        [ObservableProperty]
        private int geoUnchangedCount;

        public ObservableCollection<string> DiagramPreviewLines { get; } = new();
        public ObservableCollection<string> GeothermometerPreviewLines { get; } = new();
        public ObservableCollection<string> HomeLinksPreviewLines { get; } = new();
        public ObservableCollection<string> AnnouncementPreviewLines { get; } = new();
        public ObservableCollection<HomeLinkGroupEditorViewModel> HomeLinkGroups { get; } = new();

        public ContentLanguageContext HomeLinksLanguageContext { get; } = new();

        public ContentLanguageContext PlotCategoriesLanguageContext { get; } = new();

        public ContentLanguageContext GeoTMineralCategoriesLanguageContext { get; } = new();

        public IReadOnlyList<string> PlotCategoryLevelKeys { get; } = new[] { "Level1", "Level2", "Level3" };

        public ObservableCollection<LocalizedCategoryEntryViewModel> PlotCategoryEntries { get; } = new();

        public ObservableCollection<LocalizedCategoryEntryViewModel> GeoTMineralEntries { get; } = new();

        public IReadOnlyList<CultureOption> HomeLinksLanguageOptions { get; } = AppCultureRegistry.GetContentOptions();

        [ObservableProperty]
        private string selectedHomeLinksLanguage = AppCultureRegistry.DefaultContentLanguage;

        [ObservableProperty]
        private string selectedPlotCategoriesLanguage = AppCultureRegistry.DefaultContentLanguage;

        [ObservableProperty]
        private string selectedGeoTMineralCategoriesLanguage = AppCultureRegistry.DefaultContentLanguage;

        [ObservableProperty]
        private string selectedPlotCategoryLevel = "Level1";

        [ObservableProperty]
        private LocalizedCategoryEntryViewModel? selectedPlotCategoryEntry;

        [ObservableProperty]
        private LocalizedCategoryEntryViewModel? selectedGeoTMineralEntry;

        [ObservableProperty]
        private int plotCategoryEntryCount;

        [ObservableProperty]
        private int geoTMineralEntryCount;

        private readonly Dictionary<string, ObservableCollection<LocalizedCategoryEntryViewModel>> _plotCategoryEntriesByLevel = new();

        partial void OnSelectedHomeLinksLanguageChanged(string value)
        {
            HomeLinksLanguageContext.ContentLanguage = value;
        }

        partial void OnSelectedPlotCategoriesLanguageChanged(string value)
        {
            PlotCategoriesLanguageContext.ContentLanguage = value;
        }

        partial void OnSelectedGeoTMineralCategoriesLanguageChanged(string value)
        {
            GeoTMineralCategoriesLanguageContext.ContentLanguage = value;
        }

        partial void OnSelectedPlotCategoryLevelChanged(string value)
        {
            RefreshPlotCategoryEntriesForSelectedLevel();
        }

        private string _remoteAnnouncementText = string.Empty;
        private string _remoteMinimumSupportedVersion = string.Empty;
        private string _remoteLatestAppVersion = string.Empty;

        private PublishPreview? _diagramPreview;
        private PublishPreview? _geothermometerPreview;

        public Action<string>? ShowSuccessMessage { get; set; }
        public Action<string>? ShowWarningMessage { get; set; }
        public Action<string>? ShowErrorMessage { get; set; }
        public Func<string, string, string, Task<bool>>? ShowConfirmDialogAsync { get; set; }
        public Window? OwnerWindow { get; set; }

        private void ShowSuccess(string message)
        {
            if (ShowSuccessMessage != null)
            {
                ShowSuccessMessage(message);
                return;
            }

            MessageHelper.Success(message);
        }

        private void ShowWarning(string message)
        {
            if (ShowWarningMessage != null)
            {
                ShowWarningMessage(message);
                return;
            }

            MessageHelper.Warning(message);
        }

        private void ShowError(string message)
        {
            if (ShowErrorMessage != null)
            {
                ShowErrorMessage(message);
                return;
            }

            MessageHelper.Error(message);
        }

        private Task<bool> ShowConfirmAsync(string message, string cancelText, string confirmText)
        {
            if (ShowConfirmDialogAsync != null)
                return ShowConfirmDialogAsync(message, cancelText, confirmText);

            return MessageHelper.ShowAsyncDialog(message, cancelText, confirmText);
        }

        public OfficialTemplatePublisherViewModel()
        {
            if (bool.TryParse(ConfigHelper.GetConfig("developer_mode"), out bool devMode))
                IsDeveloperMode = devMode;

            var settings = CosPublishSettingsService.Load();
            SecretId = settings.SecretId ?? string.Empty;
            Region = string.IsNullOrWhiteSpace(settings.Region) ? OfficialContentEndpoints.DefaultRegion : settings.Region;
            Bucket = string.IsNullOrWhiteSpace(settings.Bucket) ? OfficialContentEndpoints.DefaultBucket : settings.Bucket;
            StagingDirectory = settings.StagingDirectory ?? ConfigHelper.GetConfig("publish_staging_dir") ?? string.Empty;

            Log(LanguageService.Instance["official_publisher_ready"] ?? "Official publisher ready.");
            if (!IsDeveloperMode)
                Log(LanguageService.Instance["official_publisher_dev_mode_required"] ?? "Developer mode is required.");

            SelectedHomeLinksLanguage = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            HomeLinksLanguageContext.ContentLanguage = SelectedHomeLinksLanguage;
            SelectedPlotCategoriesLanguage = SelectedHomeLinksLanguage;
            PlotCategoriesLanguageContext.ContentLanguage = SelectedPlotCategoriesLanguage;
            SelectedGeoTMineralCategoriesLanguage = SelectedHomeLinksLanguage;
            GeoTMineralCategoriesLanguageContext.ContentLanguage = SelectedGeoTMineralCategoriesLanguage;
            LoadHomeLinksEditor();
            LoadPlotCategoriesEditor();
            LoadGeoTMineralCategoriesEditor();
            _ = InitializeHomeLinksEditorAsync();
            _ = LoadAnnouncementFromServerAsync();
        }

        private async Task InitializeHomeLinksEditorAsync()
        {
            try
            {
                if (File.Exists(HomeLinksPublishService.GetPublisherCatalogPath()))
                    return;

                await HomeLinksCatalogService.SyncFromServerAsync();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    PopulateHomeLinksEditor(HomeLinksPublishService.LoadPublisherCatalogFromSyncedLocal()));
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        [RelayCommand]
        private async Task LoadAnnouncementFromServerAsync()
        {
            try
            {
                var info = await LoadRemoteServerInfoAsync();
                _remoteAnnouncementText = info?.Announcement ?? string.Empty;
                _remoteMinimumSupportedVersion = info?.MinimumSupportedVersion ?? string.Empty;
                _remoteLatestAppVersion = info?.LatestAppVersion ?? string.Empty;
                AnnouncementText = _remoteAnnouncementText;
                MinimumSupportedVersionText = _remoteMinimumSupportedVersion;
                LatestAppVersionText = _remoteLatestAppVersion;
                UpdateServerConfigChangeState();
                Log(LanguageService.Instance["official_publisher_announcement_loaded"] ?? "Announcement loaded from server.");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void UpdateServerConfigChangeState()
        {
            AnnouncementHasRemoteChanges = !string.Equals(
                AnnouncementText?.Trim(),
                _remoteAnnouncementText?.Trim(),
                StringComparison.Ordinal);

            MinimumSupportedVersionHasRemoteChanges = !string.Equals(
                MinimumSupportedVersionText?.Trim(),
                _remoteMinimumSupportedVersion?.Trim(),
                StringComparison.OrdinalIgnoreCase);

            LatestAppVersionHasRemoteChanges = !string.Equals(
                LatestAppVersionText?.Trim(),
                _remoteLatestAppVersion?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        partial void OnAnnouncementTextChanged(string value) => UpdateServerConfigChangeState();

        partial void OnMinimumSupportedVersionTextChanged(string value) => UpdateServerConfigChangeState();

        partial void OnLatestAppVersionTextChanged(string value) => UpdateServerConfigChangeState();

        private void Log(string message)
        {
            LogText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        private void BeginPublishProgress(int exportStepCount)
        {
            IsPublishing = true;
            PublishProgress = 0;
            PublishProgressText = string.Empty;
            _publishExportStepCount = Math.Max(1, exportStepCount);
            _publishExportStepIndex = 0;
        }

        private void ResetPublishProgress()
        {
            IsPublishing = false;
            PublishProgress = 0;
            PublishProgressText = string.Empty;
            _publishExportStepCount = 0;
            _publishExportStepIndex = 0;
        }

        private void ReportPublishProgress(double value, string? message = null)
        {
            PublishProgress = Math.Clamp(value, 0, 100);
            if (!string.IsNullOrWhiteSpace(message))
                PublishProgressText = message;
        }

        private void ReportPublishExportStep(string message)
        {
            _publishExportStepIndex++;
            double fraction = _publishExportStepIndex / (double)_publishExportStepCount;
            double value = PublishExportPhaseStart + (PublishExportPhaseEnd - PublishExportPhaseStart) * fraction;
            ReportPublishProgress(value, message);
        }

        private int CountPublishExportSteps(bool exportDiagrams, bool exportGeothermometers)
        {
            int steps = 0;
            if (PublishAnnouncement && !exportDiagrams && !PublishHomeLinks)
                steps++;
            if (PublishHomeLinks && !exportDiagrams)
                steps++;
            if (exportDiagrams)
                steps++;
            if (exportGeothermometers)
                steps++;
            return Math.Max(1, steps);
        }

        [RelayCommand]
        private void BrowseStagingDirectory()
        {
            string? folder = FileHelper.GetFolderPath();
            if (!string.IsNullOrEmpty(folder))
            {
                StagingDirectory = folder;
                ConfigHelper.SetConfig("publish_staging_dir", StagingDirectory);
            }
        }

        [RelayCommand]
        private void SaveCosSettings()
        {
            if (string.IsNullOrWhiteSpace(SecretId))
            {
                ShowWarning(LanguageService.Instance["cos_secret_id_required"] ?? "SecretId is required.");
                return;
            }

            var settings = CosPublishSettingsService.Load();
            settings.SecretId = SecretId.Trim();
            settings.Region = string.IsNullOrWhiteSpace(Region) ? OfficialContentEndpoints.DefaultRegion : Region.Trim();
            settings.Bucket = string.IsNullOrWhiteSpace(Bucket) ? OfficialContentEndpoints.DefaultBucket : Bucket.Trim();
            settings.StagingDirectory = StagingDirectory;

            CosPublishSettingsService.Save(settings, string.IsNullOrWhiteSpace(SecretKey) ? null : SecretKey.Trim());
            SecretKey = string.Empty;
            Log(LanguageService.Instance["cos_settings_saved"] ?? "COS settings saved.");
            ShowSuccess(LanguageService.Instance["cos_settings_saved"] ?? "COS settings saved.");
        }

        [RelayCommand]
        private async Task TestCosConnection()
        {
            if (!EnsureDeveloperMode()) return;
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                var settings = BuildSettingsFromUi();
                if (!settings.IsConfigured && string.IsNullOrWhiteSpace(SecretKey))
                {
                    ShowWarning(LanguageService.Instance["cos_credentials_required"] ?? "Configure COS credentials first.");
                    return;
                }

                bool ok = await TencentCosPublishService.TestConnectionAsync(settings, SecretKey.Trim());
                if (ok)
                {
                    Log(LanguageService.Instance["cos_test_success"] ?? "COS connection test succeeded.");
                    ShowSuccess(LanguageService.Instance["cos_test_success"] ?? "COS connection test succeeded.");
                }
                else
                {
                    Log(LanguageService.Instance["cos_test_failed"] ?? "COS connection test failed.");
                    ShowError(LanguageService.Instance["cos_test_failed"] ?? "COS connection test failed.");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefreshPreview()
        {
            if (!EnsureDeveloperMode()) return;
            if (IsBusy) return;

            IsBusy = true;
            DiagramPreviewLines.Clear();
            GeothermometerPreviewLines.Clear();
            try
            {
                Log(LanguageService.Instance["official_publisher_refreshing"] ?? "Refreshing publish preview...");

                if (PublishDiagrams)
                {
                    _diagramPreview = await GraphMapTemplatePublishService.GetPublishPreviewAsync();
                    DiagramNewCount = _diagramPreview.NewItems.Count;
                    DiagramUpdatedCount = _diagramPreview.UpdatedItems.Count;
                    DiagramRemovedCount = _diagramPreview.RemovedItems.Count;
                    DiagramUnchangedCount = _diagramPreview.UnchangedItems.Count;

                    AppendPreviewGroup(DiagramPreviewLines,
                        LanguageService.Instance["official_publisher_diagram_section"] ?? "Diagrams",
                        _diagramPreview.NewItems, _diagramPreview.UpdatedItems, _diagramPreview.RemovedItems);

                    Log(string.Format(
                        LanguageService.Instance["official_publisher_diagram_preview_summary"] ?? "Diagrams: new {0}, updated {1}, removed {2}, unchanged {3}.",
                        DiagramNewCount, DiagramUpdatedCount, DiagramRemovedCount, DiagramUnchangedCount));
                }
                else
                {
                    _diagramPreview = null;
                    DiagramNewCount = DiagramUpdatedCount = DiagramRemovedCount = DiagramUnchangedCount = 0;
                }

                if (PublishGeothermometers)
                {
                    _geothermometerPreview = await GeothermometerPublishService.GetPublishPreviewAsync();
                    GeoNewCount = _geothermometerPreview.NewItems.Count;
                    GeoUpdatedCount = _geothermometerPreview.UpdatedItems.Count;
                    GeoRemovedCount = _geothermometerPreview.RemovedItems.Count;
                    GeoUnchangedCount = _geothermometerPreview.UnchangedItems.Count;

                    AppendPreviewGroup(GeothermometerPreviewLines,
                        LanguageService.Instance["official_publisher_geothermometer_section"] ?? "Geothermobarometers",
                        _geothermometerPreview.NewItems, _geothermometerPreview.UpdatedItems, _geothermometerPreview.RemovedItems);

                    Log(string.Format(
                        LanguageService.Instance["official_publisher_geo_preview_summary"] ?? "Geothermobarometers: new {0}, updated {1}, removed {2}, unchanged {3}.",
                        GeoNewCount, GeoUpdatedCount, GeoRemovedCount, GeoUnchangedCount));
                }
                else
                {
                    _geothermometerPreview = null;
                    GeoNewCount = GeoUpdatedCount = GeoRemovedCount = GeoUnchangedCount = 0;
                }

                if (PublishHomeLinks)
                {
                    HomeLinksPreviewLines.Clear();
                    var catalog = BuildCatalogFromEditor();
                    UpdateHomeLinkCounts(catalog);
                    var homePreview = await HomeLinksPublishService.GetPublishPreviewAsync(catalog);
                    HomeLinksHasRemoteChanges = homePreview.HasRemoteChanges;

                    HomeLinksPreviewLines.Add(string.Format(
                        LanguageService.Instance["official_publisher_home_links_preview_summary"]
                            ?? "Groups: {0}, links: {1}, local hash: {2}, remote hash: {3}.",
                        homePreview.GroupCount,
                        homePreview.LinkCount,
                        homePreview.LocalHash,
                        string.IsNullOrEmpty(homePreview.RemoteHash) ? "(none)" : homePreview.RemoteHash));

                    HomeLinksPreviewLines.Add(homePreview.HasRemoteChanges
                        ? (LanguageService.Instance["official_publisher_home_links_pending"] ?? "Pending publish.")
                        : (LanguageService.Instance["official_publisher_home_links_up_to_date"] ?? "Already up to date on server."));

                    Log(string.Format(
                        LanguageService.Instance["official_publisher_home_links_summary"]
                            ?? "Home links: {0} groups, {1} links, hash={2}.",
                        homePreview.GroupCount,
                        homePreview.LinkCount,
                        homePreview.LocalHash ?? string.Empty));
                }
                else
                {
                    HomeLinksPreviewLines.Clear();
                    HomeLinksHasRemoteChanges = false;
                }

                if (PublishAnnouncement)
                {
                    AnnouncementPreviewLines.Clear();
                    var remoteInfo = await LoadRemoteServerInfoAsync();
                    _remoteAnnouncementText = remoteInfo?.Announcement ?? string.Empty;
                    _remoteMinimumSupportedVersion = remoteInfo?.MinimumSupportedVersion ?? string.Empty;
                    _remoteLatestAppVersion = remoteInfo?.LatestAppVersion ?? string.Empty;
                    UpdateServerConfigChangeState();

                    AnnouncementPreviewLines.Add(AnnouncementHasRemoteChanges
                        ? (LanguageService.Instance["official_publisher_announcement_pending"] ?? "Pending publish.")
                        : (LanguageService.Instance["official_publisher_announcement_up_to_date"] ?? "Already up to date on server."));

                    AnnouncementPreviewLines.Add(MinimumSupportedVersionHasRemoteChanges
                        ? (LanguageService.Instance["official_publisher_minimum_version_pending"] ?? "Minimum supported version pending publish.")
                        : (LanguageService.Instance["official_publisher_minimum_version_up_to_date"] ?? "Minimum supported version is up to date."));

                    AnnouncementPreviewLines.Add(LatestAppVersionHasRemoteChanges
                        ? "Latest app version pending publish."
                        : "Latest app version is up to date.");

                    if (!TryNormalizeMinimumSupportedVersion(MinimumSupportedVersionText, out string normalizedMinimumVersion, out string minimumVersionError))
                    {
                        AnnouncementPreviewLines.Add(minimumVersionError);
                        ShowWarning(minimumVersionError);
                        return;
                    }

                    if (!TryNormalizeLatestAppVersion(LatestAppVersionText, out string normalizedLatestVersion, out string latestVersionError))
                    {
                        AnnouncementPreviewLines.Add(latestVersionError);
                        ShowWarning(latestVersionError);
                        return;
                    }

                    AnnouncementPreviewLines.Add(string.Format(
                        LanguageService.Instance["official_publisher_minimum_version_preview"]
                            ?? "Minimum supported version: {0}",
                        string.IsNullOrWhiteSpace(normalizedMinimumVersion)
                            ? (LanguageService.Instance["official_publisher_minimum_version_empty"] ?? "(empty)")
                            : normalizedMinimumVersion));

                    AnnouncementPreviewLines.Add(string.IsNullOrWhiteSpace(normalizedLatestVersion)
                        ? "Latest app version: (empty)"
                        : $"Latest app version: {normalizedLatestVersion}");

                    if (!string.IsNullOrWhiteSpace(normalizedLatestVersion))
                    {
                        AnnouncementPreviewLines.Add($"Installer file: {OfficialContentEndpoints.BuildInstallerFileName(normalizedLatestVersion)}");
                    }

                    if (!string.IsNullOrWhiteSpace(AnnouncementText))
                    {
                        AnnouncementPreviewLines.Add(LanguageService.Instance["official_publisher_announcement_preview"] ?? "Current draft:");
                        foreach (var line in AnnouncementText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                            AnnouncementPreviewLines.Add("  " + line);
                    }
                    else
                    {
                        AnnouncementPreviewLines.Add(LanguageService.Instance["official_publisher_announcement_empty"] ?? "(empty)");
                    }

                    Log(AnnouncementHasRemoteChanges
                        ? (LanguageService.Instance["official_publisher_announcement_pending"] ?? "Announcement pending publish.")
                        : (LanguageService.Instance["official_publisher_announcement_up_to_date"] ?? "Announcement is up to date."));
                }
                else
                {
                    AnnouncementPreviewLines.Clear();
                    AnnouncementHasRemoteChanges = false;
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExportLocal()
        {
            if (!EnsureDeveloperMode()) return;
            if (IsBusy) return;

            SavePlotCategoriesToLocal();
            SaveGeoTMineralCategoriesToLocal();
            if (!await EnsurePublishTargetSelectedAsync()) return;

            string? outputDir = ResolveStagingDirectory();
            if (string.IsNullOrEmpty(outputDir)) return;

            IsBusy = true;
            try
            {
                Log(string.Format(LanguageService.Instance["official_publisher_exporting"] ?? "Exporting to {0}...", outputDir));
                string? announcement = await ResolveAnnouncementForPublishAsync();
                string? minimumSupportedVersion = await ResolveMinimumSupportedVersionForPublishAsync();
                string? latestAppVersion = await ResolveLatestAppVersionForPublishAsync();
                SaveHomeLinksToLocal();

                var (exportDiagrams, exportGeothermometers) = await ResolveCategoryExportFlagsAsync();
                LogCategoryOnlyExportNotice(exportDiagrams, exportGeothermometers);

                HomeLinksPublishResult? homeLinksResult = null;
                AnnouncementPublishResult? announcementResult = null;

                if (PublishAnnouncement && !exportDiagrams && !PublishHomeLinks)
                {
                    announcementResult = await Task.Run(() =>
                        HomeLinksPublishService.ExportAnnouncementToDirectory(outputDir, announcement, minimumSupportedVersion, latestAppVersion));
                    Log(announcementResult.Summary);
                }

                if (PublishHomeLinks)
                {
                    if (exportDiagrams)
                    {
                        Log(LanguageService.Instance["official_publisher_home_links_draft_saved"]
                            ?? "Home links draft saved locally.");
                    }
                    else
                    {
                        homeLinksResult = await Task.Run(() => HomeLinksPublishService.ExportToDirectory(
                            outputDir, BuildCatalogFromEditor(), announcement, minimumSupportedVersion, latestAppVersion));
                        Log(homeLinksResult.Summary);
                        LogHomeLinksPublishSummary(homeLinksResult);
                    }
                }

                if (exportDiagrams)
                {
                    var result = await Task.Run(() => GraphMapTemplatePublishService.ExportToDirectory(outputDir, new PublishOptions
                    {
                        PreserveAnnouncement = announcement,
                        PreserveMinimumSupportedVersion = minimumSupportedVersion,
                        PreserveLatestAppVersion = latestAppVersion
                    }));
                    Log(result.Summary);
                    LogHomeLinksCatalogSummary(result);
                }

                if (exportGeothermometers)
                {
                    var geoResult = await Task.Run(() => GeothermometerPublishService.ExportToDirectory(outputDir));
                    Log(geoResult.Summary);
                }

                ShowSuccess(LanguageService.Instance["official_publisher_export_done"] ?? "Export completed.");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task PublishToCos()
        {
            if (!EnsureDeveloperMode()) return;
            if (IsBusy) return;

            SavePlotCategoriesToLocal();
            SaveGeoTMineralCategoriesToLocal();
            if (!await EnsurePublishTargetSelectedAsync()) return;

            var settings = BuildSettingsFromUi();
            if (!settings.IsConfigured)
            {
                ShowWarning(LanguageService.Instance["cos_credentials_required"] ?? "Configure and save COS credentials first.");
                return;
            }

            string? outputDir = ResolveStagingDirectory();
            if (string.IsNullOrEmpty(outputDir)) return;

            bool confirm = await ShowConfirmAsync(
                LanguageService.Instance["official_publisher_confirm"] ?? "Publish official content to COS?",
                LanguageService.Instance["Cancel"] ?? "Cancel",
                LanguageService.Instance["Confirm"] ?? "Confirm");
            if (!confirm) return;

            var (exportDiagrams, exportGeothermometers) = await ResolveCategoryExportFlagsAsync();

            IsBusy = true;
            BeginPublishProgress(CountPublishExportSteps(exportDiagrams, exportGeothermometers));
            try
            {
                string startMessage = LanguageService.Instance["official_publisher_start"] ?? "Starting publish...";
                Log(startMessage);
                ReportPublishProgress(2, startMessage);

                string? announcement = await ResolveAnnouncementForPublishAsync();
                string? minimumSupportedVersion = await ResolveMinimumSupportedVersionForPublishAsync();
                string? latestAppVersion = await ResolveLatestAppVersionForPublishAsync();
                SaveHomeLinksToLocal();
                LogCategoryOnlyExportNotice(exportDiagrams, exportGeothermometers);

                PublishResult? diagramResult = null;
                GeothermometerPublishResult? geoResult = null;
                HomeLinksPublishResult? homeLinksResult = null;
                AnnouncementPublishResult? announcementResult = null;

                if (PublishAnnouncement && !exportDiagrams && !PublishHomeLinks)
                {
                    string exportMessage = LanguageService.Instance["official_publisher_progress_exporting_announcement"]
                        ?? "Exporting announcement...";
                    ReportPublishExportStep(exportMessage);
                    announcementResult = await Task.Run(() =>
                        HomeLinksPublishService.ExportAnnouncementToDirectory(outputDir, announcement, minimumSupportedVersion, latestAppVersion));
                    Log(announcementResult.Summary);
                }

                if (PublishHomeLinks && !exportDiagrams)
                {
                    string exportMessage = LanguageService.Instance["official_publisher_progress_exporting_home_links"]
                        ?? "Exporting home links...";
                    ReportPublishExportStep(exportMessage);
                    homeLinksResult = await Task.Run(() => HomeLinksPublishService.ExportToDirectory(
                        outputDir, BuildCatalogFromEditor(), announcement, minimumSupportedVersion, latestAppVersion));
                    Log(homeLinksResult.Summary);
                    LogHomeLinksPublishSummary(homeLinksResult);
                }
                else if (PublishHomeLinks)
                {
                    Log(LanguageService.Instance["official_publisher_home_links_draft_saved"]
                        ?? "Home links draft saved locally.");
                }

                if (exportDiagrams)
                {
                    string exportMessage = LanguageService.Instance["official_publisher_progress_exporting_diagrams"]
                        ?? "Exporting diagrams...";
                    ReportPublishExportStep(exportMessage);
                    diagramResult = await Task.Run(() => GraphMapTemplatePublishService.ExportToDirectory(outputDir, new PublishOptions
                    {
                        PreserveAnnouncement = announcement,
                        PreserveMinimumSupportedVersion = minimumSupportedVersion,
                        PreserveLatestAppVersion = latestAppVersion
                    }));
                    Log(diagramResult.Summary);
                    LogHomeLinksCatalogSummary(diagramResult);
                }

                if (exportGeothermometers)
                {
                    string exportMessage = LanguageService.Instance["official_publisher_progress_exporting_geothermometers"]
                        ?? "Exporting geothermobarometers...";
                    ReportPublishExportStep(exportMessage);
                    geoResult = await Task.Run(() => GeothermometerPublishService.ExportToDirectory(outputDir));
                    Log(geoResult.Summary);
                }

                var logProgress = new Progress<string>(Log);
                var uploadProgress = new Progress<(int current, int total)>(progress =>
                {
                    if (progress.total <= 0)
                        return;

                    double uploadFraction = progress.current / (double)progress.total;
                    double value = PublishUploadPhaseStart + (PublishUploadPhaseEnd - PublishUploadPhaseStart) * uploadFraction;
                    string message = string.Format(
                        LanguageService.Instance["official_publisher_progress_uploading"] ?? "Uploading to COS ({0}/{1})...",
                        progress.current,
                        progress.total);
                    ReportPublishProgress(value, message);
                });

                string uploadingMessage = LanguageService.Instance["official_publisher_progress_uploading_cos"]
                    ?? "Uploading to COS...";
                ReportPublishProgress(PublishUploadPhaseStart, uploadingMessage);

                var uploadResult = await TencentCosPublishService.UploadCombinedPublishAsync(
                    outputDir,
                    diagramResult,
                    geoResult,
                    homeLinksResult,
                    announcementResult,
                    settings,
                    exportDiagrams,
                    exportGeothermometers,
                    PublishHomeLinks,
                    PublishAnnouncement,
                    logProgress,
                    uploadProgress);

                if (exportDiagrams && PublishDiagrams)
                {
                    GraphMapTemplatePublishService.ClearPendingPublishFlags();
                    WeakReferenceMessenger.Default.Send(new OfficialTemplatesPublishedMessage());
                }

                if (PublishAnnouncement)
                {
                    _remoteAnnouncementText = announcement ?? string.Empty;
                    _remoteMinimumSupportedVersion = minimumSupportedVersion ?? string.Empty;
                    _remoteLatestAppVersion = latestAppVersion ?? string.Empty;
                    UpdateServerConfigChangeState();
                }

                string doneMessage = LanguageService.Instance["official_publisher_progress_done"] ?? "Publish completed.";
                ReportPublishProgress(100, doneMessage);
                Log(uploadResult.Message);
                ShowSuccess(uploadResult.Message);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
                ResetPublishProgress();
            }
        }

        private CosPublishSettings BuildSettingsFromUi()
        {
            var settings = CosPublishSettingsService.Load();
            settings.SecretId = SecretId?.Trim() ?? string.Empty;
            settings.Region = string.IsNullOrWhiteSpace(Region) ? OfficialContentEndpoints.DefaultRegion : Region.Trim();
            settings.Bucket = string.IsNullOrWhiteSpace(Bucket) ? OfficialContentEndpoints.DefaultBucket : Bucket.Trim();
            settings.StagingDirectory = StagingDirectory;
            return settings;
        }

        private string? ResolveStagingDirectory()
        {
            if (!string.IsNullOrWhiteSpace(StagingDirectory))
            {
                ConfigHelper.SetConfig("publish_staging_dir", StagingDirectory);
                return StagingDirectory;
            }

            string? folder = FileHelper.GetFolderPath();
            if (!string.IsNullOrEmpty(folder))
            {
                StagingDirectory = folder;
                ConfigHelper.SetConfig("publish_staging_dir", StagingDirectory);
                return StagingDirectory;
            }

            ShowWarning(LanguageService.Instance["official_publisher_staging_required"] ?? "Select a staging directory.");
            return null;
        }

        private bool EnsureDeveloperMode()
        {
            if (IsDeveloperMode) return true;
            ShowWarning(LanguageService.Instance["official_publisher_dev_mode_required"] ?? "Developer mode is required.");
            return false;
        }

        private async Task<bool> EnsurePublishTargetSelectedAsync()
        {
            if (PublishDiagrams || PublishGeothermometers || PublishHomeLinks || PublishAnnouncement)
                return true;

            if (await HasPlotCategoriesPendingPublishAsync() || await HasGeoTMineralCategoriesPendingPublishAsync())
                return true;

            ShowWarning(LanguageService.Instance["official_publisher_target_required"] ?? "Select at least one publish target.");
            return false;
        }

        private async Task<(bool exportDiagrams, bool exportGeothermometers)> ResolveCategoryExportFlagsAsync()
        {
            bool plotCategoriesPending = await HasPlotCategoriesPendingPublishAsync();
            bool geoCategoriesPending = await HasGeoTMineralCategoriesPendingPublishAsync();
            return (PublishDiagrams || plotCategoriesPending, PublishGeothermometers || geoCategoriesPending);
        }

        private void LogCategoryOnlyExportNotice(bool exportDiagrams, bool exportGeothermometers)
        {
            if (exportDiagrams && !PublishDiagrams)
            {
                Log(LanguageService.Instance["official_publisher_plot_categories_auto_included"]
                    ?? "Plot categories changed; including diagram index files in this publish.");
            }

            if (exportGeothermometers && !PublishGeothermometers)
            {
                Log(LanguageService.Instance["official_publisher_geot_categories_auto_included"]
                    ?? "GeoT mineral tags changed; including geothermometer index files in this publish.");
            }
        }

        private static async Task<bool> HasPlotCategoriesPendingPublishAsync()
        {
            string path = PlotCategoryHelper.ResolveExportConfigPath();
            if (!File.Exists(path))
                return false;

            string localHash = UpdateHelper.ComputeFileMd5(path);
            var info = await LoadRemoteServerInfoAsync();
            return string.IsNullOrEmpty(info.ListPlotCategoriesHash)
                || !string.Equals(localHash, info.ListPlotCategoriesHash, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<bool> HasGeoTMineralCategoriesPendingPublishAsync()
        {
            string path = GeoTMineralCategoryHelper.ResolveExportConfigPath();
            if (!File.Exists(path))
                return false;

            string localHash = UpdateHelper.ComputeFileMd5(path);
            try
            {
                string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.GeoTIndexUrl);
                var index = JsonHelper.Deserialize<GeoTIndex>(json);
                return string.IsNullOrEmpty(index?.MineralCategoriesHash)
                    || !string.Equals(localHash, index.MineralCategoriesHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true;
            }
        }

        private Task<string?> ResolveAnnouncementForPublishAsync()
        {
            if (PublishAnnouncement)
                return Task.FromResult<string?>(AnnouncementText ?? string.Empty);

            return Task.FromResult<string?>(null);
        }

        private Task<string?> ResolveMinimumSupportedVersionForPublishAsync()
        {
            if (PublishAnnouncement)
            {
                if (!TryNormalizeMinimumSupportedVersion(MinimumSupportedVersionText, out string normalized, out string error))
                    throw new InvalidOperationException(error);

                return Task.FromResult<string?>(normalized);
            }

            return Task.FromResult<string?>(null);
        }

        private Task<string?> ResolveLatestAppVersionForPublishAsync()
        {
            if (PublishAnnouncement)
            {
                if (!TryNormalizeLatestAppVersion(LatestAppVersionText, out string normalized, out string error))
                    throw new InvalidOperationException(error);

                return Task.FromResult<string?>(normalized);
            }

            return Task.FromResult<string?>(null);
        }

        [RelayCommand]
        private async Task ReloadHomeLinksEditor()
        {
            if (!EnsureDeveloperMode())
                return;

            await HomeLinksCatalogService.SyncFromServerAsync();
            PopulateHomeLinksEditor(HomeLinksPublishService.LoadPublisherCatalogFromSyncedLocal());
            Log(LanguageService.Instance["official_publisher_home_links_reloaded"] ?? "Home links editor reloaded.");
        }

        [RelayCommand]
        private void AddHomeLinkGroup()
        {
            var dialog = new HomeLinkLocalizedEditWindow(
                HomeLinkLocalizedEditMode.Group,
                HomeLinksLanguageContext,
                LanguageService.Instance["official_publisher_home_add_group"] ?? "Add Group");
            dialog.Owner = OwnerWindow ?? Application.Current.MainWindow;
            if (dialog.ShowDialog() != true)
                return;

            int nextOrder = HomeLinkGroups.Count == 0 ? 0 : HomeLinkGroups.Max(g => g.SortOrder) + 1;
            var group = new HomeLinkGroupEditorViewModel(HomeLinksLanguageContext)
            {
                Title = dialog.ResultTitle,
                SortOrder = nextOrder
            };
            group.Id = Slugify(HomeLinksLocalization.GetSortKey(group.Title));
            HomeLinkGroups.Add(group);
            SelectedHomeLinkGroup = group;
            UpdateHomeLinkCounts(BuildCatalogFromEditor());
        }

        [RelayCommand]
        private void EditHomeLinkGroup()
        {
            if (SelectedHomeLinkGroup == null)
                return;

            var dialog = new HomeLinkLocalizedEditWindow(
                HomeLinkLocalizedEditMode.Group,
                HomeLinksLanguageContext,
                LanguageService.Instance["official_publisher_home_edit_group"] ?? "Edit Group",
                title: SelectedHomeLinkGroup.Title);
            dialog.Owner = OwnerWindow ?? Application.Current.MainWindow;
            if (dialog.ShowDialog() != true)
                return;

            SelectedHomeLinkGroup.Title = dialog.ResultTitle;
            UpdateHomeLinkCounts(BuildCatalogFromEditor());
        }

        [RelayCommand]
        private void RemoveHomeLinkGroup()
        {
            if (SelectedHomeLinkGroup == null)
                return;

            HomeLinkGroups.Remove(SelectedHomeLinkGroup);
            SelectedHomeLinkGroup = HomeLinkGroups.FirstOrDefault();
            SelectedHomeLink = null;
            UpdateHomeLinkCounts(BuildCatalogFromEditor());
        }

        [RelayCommand]
        private void AddHomeLink()
        {
            if (SelectedHomeLinkGroup == null)
            {
                ShowWarning(LanguageService.Instance["official_publisher_home_select_group"] ?? "Select a group first.");
                return;
            }

            if (!TryEditLink(null, out var link) || link == null)
                return;

            SelectedHomeLinkGroup.Links.Add(link);
            SelectedHomeLink = link;
            UpdateHomeLinkCounts(BuildCatalogFromEditor());
        }

        [RelayCommand]
        private void EditHomeLink()
        {
            if (SelectedHomeLink == null)
                return;

            if (!TryEditLink(SelectedHomeLink, out var updated) || updated == null)
                return;

            SelectedHomeLink.Title = updated.Title;
            SelectedHomeLink.Url = updated.Url;
            SelectedHomeLink.Description = updated.Description;
            SelectedHomeLink.Icon = updated.Icon;
            UpdateHomeLinkCounts(BuildCatalogFromEditor());
        }

        [RelayCommand]
        private void RemoveHomeLink()
        {
            if (SelectedHomeLinkGroup == null || SelectedHomeLink == null)
                return;

            SelectedHomeLinkGroup.Links.Remove(SelectedHomeLink);
            SelectedHomeLink = null;
            UpdateHomeLinkCounts(BuildCatalogFromEditor());
        }

        [RelayCommand]
        private void SaveHomeLinksDraft()
        {
            if (!EnsureDeveloperMode())
                return;

            SaveHomeLinksToLocal();
            Log(LanguageService.Instance["official_publisher_home_links_draft_saved"] ?? "Home links draft saved locally.");
            ShowSuccess(LanguageService.Instance["official_publisher_home_links_draft_saved"] ?? "Home links draft saved locally.");
        }

        private void LoadHomeLinksEditor()
        {
            PopulateHomeLinksEditor(HomeLinksPublishService.LoadPublisherCatalog());
        }

        private void PopulateHomeLinksEditor(HomeLinksCatalog catalog)
        {
            HomeLinkGroups.Clear();

            foreach (var group in catalog.Groups ?? Enumerable.Empty<HomeLinkGroup>())
            {
                var groupVm = new HomeLinkGroupEditorViewModel(HomeLinksLanguageContext)
                {
                    Id = group.Id ?? string.Empty,
                    Title = HomeLinksLocalization.Clone(group.Title),
                    SortOrder = group.SortOrder
                };

                foreach (var link in group.Links ?? Enumerable.Empty<HomeLinkEntry>())
                {
                    groupVm.Links.Add(new HomeLinkEntryEditorViewModel(HomeLinksLanguageContext)
                    {
                        Id = link.Id ?? string.Empty,
                        Title = HomeLinksLocalization.Clone(link.Title),
                        Description = HomeLinksLocalization.Clone(link.Description),
                        Url = link.Url ?? string.Empty,
                        Icon = HomeIconHelper.ResolveIcon(link.Icon)
                    });
                }

                HomeLinkGroups.Add(groupVm);
            }

            SelectedHomeLinkGroup = HomeLinkGroups.FirstOrDefault();
            SelectedHomeLink = SelectedHomeLinkGroup?.Links.FirstOrDefault();
            UpdateHomeLinkCounts(catalog);
        }

        private HomeLinksCatalog BuildCatalogFromEditor()
        {
            var groups = HomeLinkGroups
                .Where(g => HomeLinksLocalization.HasText(g.Title))
                .Select(g => new HomeLinkGroup
                {
                    Id = string.IsNullOrWhiteSpace(g.Id)
                        ? Slugify(HomeLinksLocalization.GetSortKey(g.Title))
                        : g.Id.Trim(),
                    Title = HomeLinksLocalization.Clone(g.Title),
                    SortOrder = g.SortOrder,
                    Links = g.Links
                        .Where(l => HomeLinksLocalization.HasText(l.Title) && !string.IsNullOrWhiteSpace(l.Url))
                        .Select(l => HomeLinksPublishService.CreateLink(l.Id, l.Title, l.Url, l.Description, l.Icon))
                        .ToList()
                })
                .Where(g => g.Links.Count > 0)
                .ToList();

            return HomeLinksPublishService.BuildCatalog(groups, 2);
        }

        private void SaveHomeLinksToLocal()
        {
            HomeLinksPublishService.SavePublisherCatalog(BuildCatalogFromEditor());
        }

        private void UpdateHomeLinkCounts(HomeLinksCatalog catalog)
        {
            HomeLinkGroupCount = catalog?.Groups?.Count ?? 0;
            HomeLinkCount = catalog?.Groups?
                .SelectMany(g => g.Links ?? Enumerable.Empty<HomeLinkEntry>())
                .Count() ?? 0;
        }

        private bool TryEditLink(HomeLinkEntryEditorViewModel? existing, out HomeLinkEntryEditorViewModel? result)
        {
            result = null;
            var dialog = new HomeLinkLocalizedEditWindow(
                HomeLinkLocalizedEditMode.Link,
                HomeLinksLanguageContext,
                existing == null
                    ? (LanguageService.Instance["add_link"] ?? "Add Link")
                    : (LanguageService.Instance["edit_link"] ?? "Edit Link"),
                title: existing?.Title,
                description: existing?.Description,
                url: existing?.Url,
                icon: existing?.Icon);
            dialog.Owner = OwnerWindow ?? Application.Current.MainWindow;

            if (dialog.ShowDialog() != true)
                return false;

            result = new HomeLinkEntryEditorViewModel(HomeLinksLanguageContext)
            {
                Id = existing?.Id ?? Slugify(HomeLinksLocalization.GetSortKey(dialog.ResultTitle)),
                Title = dialog.ResultTitle,
                Url = dialog.ResultUrl,
                Description = dialog.ResultDescription,
                Icon = dialog.ResultIcon
            };
            return true;
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Guid.NewGuid().ToString("N")[..8];

            var chars = value.Trim().ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray();
            string slug = new string(chars).Trim('-');
            while (slug.Contains("--", StringComparison.Ordinal))
                slug = slug.Replace("--", "-", StringComparison.Ordinal);

            return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
        }

        private void LogHomeLinksPublishSummary(HomeLinksPublishResult result)
        {
            if (result == null)
                return;

            Log(string.Format(
                LanguageService.Instance["official_publisher_home_links_summary"]
                    ?? "Home links: {0} groups, {1} links, hash={2}.",
                result.GroupCount,
                result.LinkCount,
                result.HomeLinksHash ?? string.Empty));
        }

        private static void AppendPreviewGroup(
            ObservableCollection<string> lines,
            string sectionTitle,
            System.Collections.Generic.List<PublishPreviewItem> newItems,
            System.Collections.Generic.List<PublishPreviewItem> updatedItems,
            System.Collections.Generic.List<PublishPreviewItem> removedItems)
        {
            lines.Add($"=== {sectionTitle} ===");
            AppendPreviewGroup(lines, LanguageService.Instance["official_publisher_new"] ?? "New", newItems);
            AppendPreviewGroup(lines, LanguageService.Instance["official_publisher_updated"] ?? "Updated", updatedItems);
            AppendPreviewGroup(lines, LanguageService.Instance["official_publisher_removed"] ?? "Removed", removedItems);
        }

        private static void AppendPreviewGroup(
            ObservableCollection<string> lines,
            string title,
            System.Collections.Generic.List<PublishPreviewItem> items)
        {
            if (items == null || items.Count == 0) return;
            lines.Add($"[{title}] ({items.Count})");
            foreach (var item in items)
                lines.Add($"  - {item.Name} ({item.GraphMapPath})");
        }

        private void LogHomeLinksCatalogSummary(PublishResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.HomeLinksCatalogPath) || !File.Exists(result.HomeLinksCatalogPath))
                return;

            try
            {
                var catalog = JsonHelper.Deserialize<HomeLinksCatalog>(File.ReadAllText(result.HomeLinksCatalogPath));
                int groupCount = catalog?.Groups?.Count ?? 0;
                int linkCount = catalog?.Groups?
                    .SelectMany(g => g.Links ?? Enumerable.Empty<HomeLinkEntry>())
                    .Count() ?? 0;

                Log(string.Format(
                    LanguageService.Instance["official_publisher_home_links_summary"]
                        ?? "Home links: {0} groups, {1} links, hash={2}.",
                    groupCount,
                    linkCount,
                    result.HomeLinksHash ?? string.Empty));
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private static async Task<ServerInfo> LoadRemoteServerInfoAsync()
        {
            try
            {
                string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl);
                return JsonHelper.Deserialize<ServerInfo>(json) ?? new ServerInfo();
            }
            catch
            {
                return new ServerInfo();
            }
        }

        private static bool TryNormalizeMinimumSupportedVersion(
            string value,
            out string normalized,
            out string error)
        {
            normalized = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (!UpdateHelper.TryNormalizeVersion(value, out Version? version) || version == null)
            {
                error = LanguageService.Instance["official_publisher_minimum_version_invalid"]
                    ?? "Minimum supported version must be a valid version, for example 1.2.3.";
                return false;
            }

            normalized = version.ToString(3);
            return true;
        }

        private static bool TryNormalizeLatestAppVersion(
            string value,
            out string normalized,
            out string error)
        {
            normalized = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (!UpdateHelper.TryNormalizeVersion(value, out Version? version) || version == null)
            {
                error = "Latest app version must be a valid version, for example 1.2.3.";
                return false;
            }

            normalized = version.ToString(3);
            return true;
        }

        [RelayCommand]
        private void ReloadPlotCategoriesEditor()
        {
            LoadPlotCategoriesEditor();
            Log(LanguageService.Instance["official_publisher_plot_categories_reloaded"] ?? "Plot categories editor reloaded.");
        }

        [RelayCommand]
        private void SavePlotCategoriesDraft()
        {
            if (!EnsureDeveloperMode())
                return;

            SavePlotCategoriesToLocal();
            Log(LanguageService.Instance["official_publisher_plot_categories_draft_saved"] ?? "Plot categories draft saved locally.");
            ShowSuccess(LanguageService.Instance["official_publisher_plot_categories_draft_saved"] ?? "Plot categories draft saved locally.");
        }

        [RelayCommand]
        private void AddPlotCategoryEntry()
        {
            if (!TryEditCategoryEntry(null, PlotCategoriesLanguageContext, out var entry) || entry == null)
                return;

            GetPlotCategoryCollection(SelectedPlotCategoryLevel).Add(entry);
            PlotCategoryEntries.Add(entry);
            SelectedPlotCategoryEntry = entry;
            UpdatePlotCategoryEntryCount();
        }

        [RelayCommand]
        private void EditPlotCategoryEntry()
        {
            if (SelectedPlotCategoryEntry == null)
                return;

            if (!TryEditCategoryEntry(SelectedPlotCategoryEntry, PlotCategoriesLanguageContext, out var updated) || updated == null)
                return;

            SelectedPlotCategoryEntry.ReplaceTitle(updated.Title);
        }

        [RelayCommand]
        private void RemovePlotCategoryEntry()
        {
            if (SelectedPlotCategoryEntry == null)
                return;

            GetPlotCategoryCollection(SelectedPlotCategoryLevel).Remove(SelectedPlotCategoryEntry);
            PlotCategoryEntries.Remove(SelectedPlotCategoryEntry);
            SelectedPlotCategoryEntry = PlotCategoryEntries.FirstOrDefault();
            UpdatePlotCategoryEntryCount();
        }

        [RelayCommand]
        private void ReloadGeoTMineralCategoriesEditor()
        {
            LoadGeoTMineralCategoriesEditor();
            Log(LanguageService.Instance["official_publisher_geot_categories_reloaded"] ?? "GeoT mineral categories editor reloaded.");
        }

        [RelayCommand]
        private void SaveGeoTMineralCategoriesDraft()
        {
            if (!EnsureDeveloperMode())
                return;

            SaveGeoTMineralCategoriesToLocal();
            Log(LanguageService.Instance["official_publisher_geot_categories_draft_saved"] ?? "GeoT mineral categories draft saved locally.");
            ShowSuccess(LanguageService.Instance["official_publisher_geot_categories_draft_saved"] ?? "GeoT mineral categories draft saved locally.");
        }

        [RelayCommand]
        private void AddGeoTMineralEntry()
        {
            if (!TryEditCategoryEntry(null, GeoTMineralCategoriesLanguageContext, out var entry) || entry == null)
                return;

            GeoTMineralEntries.Add(entry);
            SelectedGeoTMineralEntry = entry;
            UpdateGeoTMineralEntryCount();
        }

        [RelayCommand]
        private void EditGeoTMineralEntry()
        {
            if (SelectedGeoTMineralEntry == null)
                return;

            if (!TryEditCategoryEntry(SelectedGeoTMineralEntry, GeoTMineralCategoriesLanguageContext, out var updated) || updated == null)
                return;

            SelectedGeoTMineralEntry.ReplaceTitle(updated.Title);
        }

        [RelayCommand]
        private void RemoveGeoTMineralEntry()
        {
            if (SelectedGeoTMineralEntry == null)
                return;

            GeoTMineralEntries.Remove(SelectedGeoTMineralEntry);
            SelectedGeoTMineralEntry = GeoTMineralEntries.FirstOrDefault();
            UpdateGeoTMineralEntryCount();
        }

        private void LoadPlotCategoriesEditor()
        {
            _plotCategoryEntriesByLevel.Clear();
            var config = PlotCategoryHelper.LoadPublisherConfig();

            foreach (string level in PlotCategoryLevelKeys)
            {
                var entries = new ObservableCollection<LocalizedCategoryEntryViewModel>();
                if (config.TryGetValue(level, out var items))
                {
                    foreach (var item in items ?? Enumerable.Empty<Dictionary<string, string>>())
                    {
                        entries.Add(new LocalizedCategoryEntryViewModel(PlotCategoriesLanguageContext)
                        {
                            Title = HomeLinksLocalization.FromDictionary(item)
                        });
                    }
                }

                _plotCategoryEntriesByLevel[level] = entries;
            }

            RefreshPlotCategoryEntriesForSelectedLevel();
        }

        private void LoadGeoTMineralCategoriesEditor()
        {
            GeoTMineralEntries.Clear();
            var config = GeoTMineralCategoryHelper.LoadPublisherConfig();

            foreach (var item in config.Minerals ?? Enumerable.Empty<Dictionary<string, string>>())
            {
                GeoTMineralEntries.Add(new LocalizedCategoryEntryViewModel(GeoTMineralCategoriesLanguageContext)
                {
                    Title = HomeLinksLocalization.FromDictionary(item)
                });
            }

            SelectedGeoTMineralEntry = GeoTMineralEntries.FirstOrDefault();
            UpdateGeoTMineralEntryCount();
        }

        private void RefreshPlotCategoryEntriesForSelectedLevel()
        {
            PlotCategoryEntries.Clear();
            foreach (var entry in GetPlotCategoryCollection(SelectedPlotCategoryLevel))
                PlotCategoryEntries.Add(entry);

            SelectedPlotCategoryEntry = PlotCategoryEntries.FirstOrDefault();
            UpdatePlotCategoryEntryCount();
        }

        private ObservableCollection<LocalizedCategoryEntryViewModel> GetPlotCategoryCollection(string level)
        {
            level ??= PlotCategoryLevelKeys[0];
            if (!_plotCategoryEntriesByLevel.TryGetValue(level, out var entries))
            {
                entries = new ObservableCollection<LocalizedCategoryEntryViewModel>();
                _plotCategoryEntriesByLevel[level] = entries;
            }

            return entries;
        }

        private PlotTemplateCategoryConfig BuildPlotCategoryConfigFromEditor()
        {
            var config = new PlotTemplateCategoryConfig();
            foreach (string level in PlotCategoryLevelKeys)
            {
                var items = GetPlotCategoryCollection(level)
                    .Where(entry => HomeLinksLocalization.HasText(entry.Title, PlotCategoriesLanguageContext))
                    .Select(entry => HomeLinksLocalization.ToDictionary(entry.Title))
                    .Where(dict => dict.Count > 0)
                    .ToList();

                if (items.Count > 0)
                    config[level] = items;
            }

            return config;
        }

        private GeoTMineralCategoryConfig BuildGeoTMineralCategoryConfigFromEditor()
        {
            return new GeoTMineralCategoryConfig
            {
                Minerals = GeoTMineralEntries
                    .Where(entry => HomeLinksLocalization.HasText(entry.Title, GeoTMineralCategoriesLanguageContext))
                    .Select(entry => HomeLinksLocalization.ToDictionary(entry.Title))
                    .Where(dict => dict.Count > 0)
                    .ToList()
            };
        }

        private void SavePlotCategoriesToLocal()
        {
            PlotCategoryHelper.SavePublisherConfig(BuildPlotCategoryConfigFromEditor());
        }

        private void SaveGeoTMineralCategoriesToLocal()
        {
            GeoTMineralCategoryHelper.SavePublisherConfig(BuildGeoTMineralCategoryConfigFromEditor());
        }

        private void UpdatePlotCategoryEntryCount()
        {
            PlotCategoryEntryCount = GetPlotCategoryCollection(SelectedPlotCategoryLevel).Count;
        }

        private void UpdateGeoTMineralEntryCount()
        {
            GeoTMineralEntryCount = GeoTMineralEntries.Count;
        }

        private bool TryEditCategoryEntry(
            LocalizedCategoryEntryViewModel? existing,
            ContentLanguageContext languageContext,
            out LocalizedCategoryEntryViewModel? result)
        {
            result = null;
            var dialog = new HomeLinkLocalizedEditWindow(
                HomeLinkLocalizedEditMode.Group,
                languageContext,
                existing == null
                    ? (LanguageService.Instance["official_publisher_category_add"] ?? "Add Category")
                    : (LanguageService.Instance["official_publisher_category_edit"] ?? "Edit Category"),
                title: existing?.Title);
            dialog.Owner = OwnerWindow ?? Application.Current.MainWindow;

            if (dialog.ShowDialog() != true)
                return false;

            result = new LocalizedCategoryEntryViewModel(languageContext)
            {
                Title = HomeLinksLocalization.Clone(dialog.ResultTitle)
            };
            return true;
        }
    }
}
