using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
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
        private bool publishDiagrams = true;

        [ObservableProperty]
        private bool publishGeothermometers = true;

        [ObservableProperty]
        private bool publishHomeLinks = true;

        [ObservableProperty]
        private bool publishAnnouncement;

        [ObservableProperty]
        private string announcementText = string.Empty;

        [ObservableProperty]
        private bool announcementHasRemoteChanges;

        [ObservableProperty]
        private int homeLinkGroupCount;

        [ObservableProperty]
        private int homeLinkCount;

        [ObservableProperty]
        private bool homeLinksHasRemoteChanges;

        [ObservableProperty]
        private HomeLinkGroupEditorViewModel selectedHomeLinkGroup;

        [ObservableProperty]
        private HomeLinkEntryEditorViewModel selectedHomeLink;

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

        public IReadOnlyList<CultureOption> HomeLinksLanguageOptions { get; } = AppCultureRegistry.GetContentOptions();

        [ObservableProperty]
        private string selectedHomeLinksLanguage = AppCultureRegistry.DefaultContentLanguage;

        partial void OnSelectedHomeLinksLanguageChanged(string value)
        {
            HomeLinksLanguageContext.ContentLanguage = value;
        }

        private string _remoteAnnouncementText = string.Empty;

        private PublishPreview _diagramPreview;
        private PublishPreview _geothermometerPreview;

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
            LoadHomeLinksEditor();
            _ = LoadAnnouncementFromServerAsync();
        }

        [RelayCommand]
        private async Task LoadAnnouncementFromServerAsync()
        {
            try
            {
                _remoteAnnouncementText = await LoadRemoteAnnouncementAsync();
                AnnouncementText = _remoteAnnouncementText;
                UpdateAnnouncementChangeState();
                Log(LanguageService.Instance["official_publisher_announcement_loaded"] ?? "Announcement loaded from server.");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void UpdateAnnouncementChangeState()
        {
            AnnouncementHasRemoteChanges = !string.Equals(
                AnnouncementText?.Trim(),
                _remoteAnnouncementText?.Trim(),
                StringComparison.Ordinal);
        }

        partial void OnAnnouncementTextChanged(string value) => UpdateAnnouncementChangeState();

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

        private void ReportPublishProgress(double value, string message = null)
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

        private int CountPublishExportSteps()
        {
            int steps = 0;
            if (PublishAnnouncement && !PublishDiagrams && !PublishGeothermometers && !PublishHomeLinks)
                steps++;
            if (PublishHomeLinks && !PublishDiagrams)
                steps++;
            if (PublishDiagrams)
                steps++;
            if (PublishGeothermometers)
                steps++;
            return Math.Max(1, steps);
        }

        [RelayCommand]
        private void BrowseStagingDirectory()
        {
            string folder = FileHelper.GetFolderPath();
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
                MessageHelper.Warning(LanguageService.Instance["cos_secret_id_required"] ?? "SecretId is required.");
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
            MessageHelper.Success(LanguageService.Instance["cos_settings_saved"] ?? "COS settings saved.");
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
                    MessageHelper.Warning(LanguageService.Instance["cos_credentials_required"] ?? "Configure COS credentials first.");
                    return;
                }

                bool ok = await TencentCosPublishService.TestConnectionAsync(settings, SecretKey.Trim());
                if (ok)
                {
                    Log(LanguageService.Instance["cos_test_success"] ?? "COS connection test succeeded.");
                    MessageHelper.Success(LanguageService.Instance["cos_test_success"] ?? "COS connection test succeeded.");
                }
                else
                {
                    Log(LanguageService.Instance["cos_test_failed"] ?? "COS connection test failed.");
                    MessageHelper.Error(LanguageService.Instance["cos_test_failed"] ?? "COS connection test failed.");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                MessageHelper.Error(ex.Message);
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
                    _remoteAnnouncementText = await LoadRemoteAnnouncementAsync();
                    UpdateAnnouncementChangeState();

                    AnnouncementPreviewLines.Add(AnnouncementHasRemoteChanges
                        ? (LanguageService.Instance["official_publisher_announcement_pending"] ?? "Pending publish.")
                        : (LanguageService.Instance["official_publisher_announcement_up_to_date"] ?? "Already up to date on server."));

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
                MessageHelper.Error(ex.Message);
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
            if (!EnsurePublishTargetSelected()) return;
            if (IsBusy) return;

            string outputDir = ResolveStagingDirectory();
            if (string.IsNullOrEmpty(outputDir)) return;

            IsBusy = true;
            try
            {
                Log(string.Format(LanguageService.Instance["official_publisher_exporting"] ?? "Exporting to {0}...", outputDir));
                string announcement = await ResolveAnnouncementForPublishAsync();
                SaveHomeLinksToBundled();

                HomeLinksPublishResult homeLinksResult = null;
                AnnouncementPublishResult announcementResult = null;

                if (PublishAnnouncement && !PublishDiagrams && !PublishGeothermometers && !PublishHomeLinks)
                {
                    announcementResult = await Task.Run(() =>
                        HomeLinksPublishService.ExportAnnouncementToDirectory(outputDir, announcement));
                    Log(announcementResult.Summary);
                }

                if (PublishHomeLinks)
                {
                    if (PublishDiagrams)
                    {
                        Log(LanguageService.Instance["official_publisher_home_links_bundled_saved"]
                            ?? "Home links saved to bundled catalog.");
                    }
                    else
                    {
                        homeLinksResult = await Task.Run(() => HomeLinksPublishService.ExportToDirectory(
                            outputDir, BuildCatalogFromEditor(), announcement));
                        Log(homeLinksResult.Summary);
                        LogHomeLinksPublishSummary(homeLinksResult);
                    }
                }

                if (PublishDiagrams)
                {
                    var result = await Task.Run(() => GraphMapTemplatePublishService.ExportToDirectory(outputDir, new PublishOptions
                    {
                        PreserveAnnouncement = announcement
                    }));
                    Log(result.Summary);
                    LogHomeLinksCatalogSummary(result);
                }

                if (PublishGeothermometers)
                {
                    var geoResult = await Task.Run(() => GeothermometerPublishService.ExportToDirectory(outputDir));
                    Log(geoResult.Summary);
                }

                MessageHelper.Success(LanguageService.Instance["official_publisher_export_done"] ?? "Export completed.");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                MessageHelper.Error(ex.Message);
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
            if (!EnsurePublishTargetSelected()) return;
            if (IsBusy) return;

            var settings = BuildSettingsFromUi();
            if (!settings.IsConfigured)
            {
                MessageHelper.Warning(LanguageService.Instance["cos_credentials_required"] ?? "Configure and save COS credentials first.");
                return;
            }

            string outputDir = ResolveStagingDirectory();
            if (string.IsNullOrEmpty(outputDir)) return;

            bool confirm = await MessageHelper.ShowAsyncDialog(
                LanguageService.Instance["official_publisher_confirm"] ?? "Publish official content to COS?",
                LanguageService.Instance["Cancel"] ?? "Cancel",
                LanguageService.Instance["Confirm"] ?? "Confirm");
            if (!confirm) return;

            IsBusy = true;
            BeginPublishProgress(CountPublishExportSteps());
            try
            {
                string startMessage = LanguageService.Instance["official_publisher_start"] ?? "Starting publish...";
                Log(startMessage);
                ReportPublishProgress(2, startMessage);

                string announcement = await ResolveAnnouncementForPublishAsync();
                SaveHomeLinksToBundled();

                PublishResult diagramResult = null;
                GeothermometerPublishResult geoResult = null;
                HomeLinksPublishResult homeLinksResult = null;
                AnnouncementPublishResult announcementResult = null;

                if (PublishAnnouncement && !PublishDiagrams && !PublishGeothermometers && !PublishHomeLinks)
                {
                    string exportMessage = LanguageService.Instance["official_publisher_progress_exporting_announcement"]
                        ?? "Exporting announcement...";
                    ReportPublishExportStep(exportMessage);
                    announcementResult = await Task.Run(() =>
                        HomeLinksPublishService.ExportAnnouncementToDirectory(outputDir, announcement));
                    Log(announcementResult.Summary);
                }

                if (PublishHomeLinks && !PublishDiagrams)
                {
                    string exportMessage = LanguageService.Instance["official_publisher_progress_exporting_home_links"]
                        ?? "Exporting home links...";
                    ReportPublishExportStep(exportMessage);
                    homeLinksResult = await Task.Run(() => HomeLinksPublishService.ExportToDirectory(
                        outputDir, BuildCatalogFromEditor(), announcement));
                    Log(homeLinksResult.Summary);
                    LogHomeLinksPublishSummary(homeLinksResult);
                }
                else if (PublishHomeLinks)
                {
                    Log(LanguageService.Instance["official_publisher_home_links_bundled_saved"]
                        ?? "Home links saved to bundled catalog.");
                }

                if (PublishDiagrams)
                {
                    string exportMessage = LanguageService.Instance["official_publisher_progress_exporting_diagrams"]
                        ?? "Exporting diagrams...";
                    ReportPublishExportStep(exportMessage);
                    diagramResult = await Task.Run(() => GraphMapTemplatePublishService.ExportToDirectory(outputDir, new PublishOptions
                    {
                        PreserveAnnouncement = announcement
                    }));
                    Log(diagramResult.Summary);
                    LogHomeLinksCatalogSummary(diagramResult);
                }

                if (PublishGeothermometers)
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
                    PublishDiagrams,
                    PublishGeothermometers,
                    PublishHomeLinks,
                    PublishAnnouncement,
                    logProgress,
                    uploadProgress);

                if (PublishDiagrams)
                    GraphMapTemplatePublishService.ClearPendingPublishFlags();

                if (PublishAnnouncement && announcementResult != null)
                    _remoteAnnouncementText = announcement ?? string.Empty;

                string doneMessage = LanguageService.Instance["official_publisher_progress_done"] ?? "Publish completed.";
                ReportPublishProgress(100, doneMessage);
                Log(uploadResult.Message);
                MessageHelper.Success(uploadResult.Message);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                MessageHelper.Error(ex.Message);
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
            settings.SecretId = SecretId?.Trim();
            settings.Region = string.IsNullOrWhiteSpace(Region) ? OfficialContentEndpoints.DefaultRegion : Region.Trim();
            settings.Bucket = string.IsNullOrWhiteSpace(Bucket) ? OfficialContentEndpoints.DefaultBucket : Bucket.Trim();
            settings.StagingDirectory = StagingDirectory;
            return settings;
        }

        private string ResolveStagingDirectory()
        {
            if (!string.IsNullOrWhiteSpace(StagingDirectory))
            {
                ConfigHelper.SetConfig("publish_staging_dir", StagingDirectory);
                return StagingDirectory;
            }

            string folder = FileHelper.GetFolderPath();
            if (!string.IsNullOrEmpty(folder))
            {
                StagingDirectory = folder;
                ConfigHelper.SetConfig("publish_staging_dir", StagingDirectory);
                return StagingDirectory;
            }

            MessageHelper.Warning(LanguageService.Instance["official_publisher_staging_required"] ?? "Select a staging directory.");
            return null;
        }

        private bool EnsureDeveloperMode()
        {
            if (IsDeveloperMode) return true;
            MessageHelper.Warning(LanguageService.Instance["official_publisher_dev_mode_required"] ?? "Developer mode is required.");
            return false;
        }

        private bool EnsurePublishTargetSelected()
        {
            if (PublishDiagrams || PublishGeothermometers || PublishHomeLinks || PublishAnnouncement) return true;
            MessageHelper.Warning(LanguageService.Instance["official_publisher_target_required"] ?? "Select at least one publish target.");
            return false;
        }

        private async Task<string> ResolveAnnouncementForPublishAsync()
        {
            if (PublishAnnouncement)
                return AnnouncementText ?? string.Empty;

            return await LoadRemoteAnnouncementAsync();
        }

        [RelayCommand]
        private void ReloadHomeLinksEditor()
        {
            LoadHomeLinksEditor();
            Log(LanguageService.Instance["official_publisher_home_links_reloaded"] ?? "Home links editor reloaded.");
        }

        [RelayCommand]
        private void AddHomeLinkGroup()
        {
            var dialog = new HomeLinkLocalizedEditWindow(
                HomeLinkLocalizedEditMode.Group,
                HomeLinksLanguageContext,
                LanguageService.Instance["official_publisher_home_add_group"] ?? "Add Group");
            dialog.Owner = Application.Current.MainWindow;
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
            dialog.Owner = Application.Current.MainWindow;
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
                MessageHelper.Warning(LanguageService.Instance["official_publisher_home_select_group"] ?? "Select a group first.");
                return;
            }

            if (!TryEditLink(null, out var link))
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

            if (!TryEditLink(SelectedHomeLink, out var updated))
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

            SaveHomeLinksToBundled();
            Log(LanguageService.Instance["official_publisher_home_links_draft_saved"] ?? "Home links draft saved locally.");
            MessageHelper.Success(LanguageService.Instance["official_publisher_home_links_draft_saved"] ?? "Home links draft saved locally.");
        }

        private void LoadHomeLinksEditor()
        {
            HomeLinkGroups.Clear();
            var catalog = HomeLinksPublishService.LoadBundledCatalog();

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

        private void SaveHomeLinksToBundled()
        {
            HomeLinksPublishService.SaveBundledCatalog(BuildCatalogFromEditor());
        }

        private void UpdateHomeLinkCounts(HomeLinksCatalog catalog)
        {
            HomeLinkGroupCount = catalog?.Groups?.Count ?? 0;
            HomeLinkCount = catalog?.Groups?
                .SelectMany(g => g.Links ?? Enumerable.Empty<HomeLinkEntry>())
                .Count() ?? 0;
        }

        private bool TryEditLink(HomeLinkEntryEditorViewModel existing, out HomeLinkEntryEditorViewModel result)
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
            dialog.Owner = Application.Current.MainWindow;

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

        private static async Task<string> LoadRemoteAnnouncementAsync()
        {
            try
            {
                string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl);
                var info = System.Text.Json.JsonSerializer.Deserialize<ServerInfo>(json);
                return info?.Announcement ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
