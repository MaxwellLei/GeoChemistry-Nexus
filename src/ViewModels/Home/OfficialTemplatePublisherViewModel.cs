using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;
using GeoChemistryNexus.Views.Widgets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

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

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(CanStageOrPublish));
            OnPropertyChanged(nameof(CanPublish));
            OnPropertyChanged(nameof(CanPublishSection));
        }

        public const string SectionRelease = "release";
        public const string SectionTargets = "targets";
        public const string SectionDiagrams = "diagrams";
        public const string SectionGeothermometers = "geothermometers";
        public const string SectionEpma = "epma";
        public const string SectionHomeLinks = "homeLinks";
        public const string SectionAnnouncement = "announcement";
        public const string SectionPlotCategories = "plotCategories";
        public const string SectionGeoTCategories = "geotCategories";

        [ObservableProperty]
        private string selectedSection = SectionRelease;

        public bool IsReleaseSection
        {
            get => SelectedSection == SectionRelease;
            set { if (value) SelectedSection = SectionRelease; }
        }

        public bool IsTargetsSection
        {
            get => SelectedSection == SectionTargets;
            set { if (value) SelectedSection = SectionTargets; }
        }

        public bool IsDiagramsSection
        {
            get => SelectedSection == SectionDiagrams;
            set { if (value) SelectedSection = SectionDiagrams; }
        }

        public bool IsGeothermometersSection
        {
            get => SelectedSection == SectionGeothermometers;
            set { if (value) SelectedSection = SectionGeothermometers; }
        }

        public bool IsEpmaSection
        {
            get => SelectedSection == SectionEpma;
            set { if (value) SelectedSection = SectionEpma; }
        }

        public bool IsHomeLinksSection
        {
            get => SelectedSection == SectionHomeLinks;
            set { if (value) SelectedSection = SectionHomeLinks; }
        }

        public bool IsAnnouncementSection
        {
            get => SelectedSection == SectionAnnouncement;
            set { if (value) SelectedSection = SectionAnnouncement; }
        }

        public bool IsPlotCategoriesSection
        {
            get => SelectedSection == SectionPlotCategories;
            set { if (value) SelectedSection = SectionPlotCategories; }
        }

        public bool IsGeoTCategoriesSection
        {
            get => SelectedSection == SectionGeoTCategories;
            set { if (value) SelectedSection = SectionGeoTCategories; }
        }

        partial void OnSelectedSectionChanged(string value)
        {
            OnPropertyChanged(nameof(IsReleaseSection));
            OnPropertyChanged(nameof(IsTargetsSection));
            OnPropertyChanged(nameof(IsDiagramsSection));
            OnPropertyChanged(nameof(IsGeothermometersSection));
            OnPropertyChanged(nameof(IsEpmaSection));
            OnPropertyChanged(nameof(IsHomeLinksSection));
            OnPropertyChanged(nameof(IsAnnouncementSection));
            OnPropertyChanged(nameof(IsPlotCategoriesSection));
            OnPropertyChanged(nameof(IsGeoTCategoriesSection));
            OnPropertyChanged(nameof(IsLibrarySection));
            OnPropertyChanged(nameof(SectionTitle));
        }

        public bool IsLibrarySection => IsDiagramsSection || IsGeothermometersSection || IsEpmaSection;

        public string SectionTitle => SelectedSection switch
        {
            SectionAnnouncement => "公告与版本",
            SectionHomeLinks => "导航链接",
            SectionDiagrams => "图解与分类",
            SectionGeothermometers => "温压计与矿物分类",
            SectionEpma => "EPMA 计算模板",
            SectionTargets => "发布目标",
            _ => "发布单"
        };

        public string PublishButtonText => EnabledTargetCount == 0
            ? "没有可用服务器"
            : $"发布到 {EnabledTargetCount} 台服务器";

        public bool HasIncludedPack =>
            PublishAnnouncement || PublishHomeLinks || PublishDiagrams || PublishGeothermometers || PublishEpma;

        public bool CanStageOrPublish => IsNotBusy && HasIncludedPack;

        /// <summary>至少勾选了一项内容，并且至少有一台启用的服务器。</summary>
        public bool CanPublish => CanStageOrPublish && EnabledTargetCount > 0;

        public bool HasNoEnabledTarget => EnabledTargetCount == 0;

        /// <summary>“发布本页”按钮是否可用：不依赖发布单里的勾选，只要空闲并且有服务器。</summary>
        public bool CanPublishSection => IsNotBusy && EnabledTargetCount > 0;

        public string AnnouncementReleaseStatus
        {
            get
            {
                string min = string.IsNullOrWhiteSpace(MinimumSupportedVersionText) ? "—" : MinimumSupportedVersionText.Trim();
                string latest = string.IsNullOrWhiteSpace(LatestAppVersionText) ? "—" : LatestAppVersionText.Trim();
                return $"{AnnouncementEntries.Count} 条 · 最低 {min} · 最新 {latest}";
            }
        }

        public string AnnouncementReleaseChip => !PublishAnnouncement ? "不发布" : AnnouncementPackHasChanges ? "有改动" : "纳入";

        public string HomeLinksReleaseStatus => $"{HomeLinkGroupCount} 个分组 · {HomeLinkCount} 个链接";

        public string HomeLinksReleaseChip => !PublishHomeLinks ? "不发布" : HomeLinksHasRemoteChanges ? "有改动" : "纳入";

        public string DiagramReleaseStatus =>
            $"新增 {DiagramNewCount} · 更新 {DiagramUpdatedCount} · 移除 {DiagramRemovedCount} · 未变化 {DiagramUnchangedCount}";

        public string DiagramReleaseChip =>
            !PublishDiagrams ? "不发布" : DiagramPackHasChanges ? "有改动" : "纳入";

        public string GeoReleaseStatus =>
            $"新增 {GeoNewCount} · 更新 {GeoUpdatedCount} · 移除 {GeoRemovedCount} · 未变化 {GeoUnchangedCount}";

        public string GeoReleaseChip =>
            !PublishGeothermometers ? "不发布" : GeoPackHasChanges ? "有改动" : "纳入";

        public string EpmaReleaseStatus =>
            $"新增 {EpmaNewCount} · 更新 {EpmaUpdatedCount} · 移除 {EpmaRemovedCount} · 未变化 {EpmaUnchangedCount}";

        public string EpmaReleaseChip =>
            !PublishEpma ? "不发布" : EpmaPackHasChanges ? "有改动" : "纳入";

        private void NotifyReleaseSheet()
        {
            OnPropertyChanged(nameof(AnnouncementReleaseStatus));
            OnPropertyChanged(nameof(AnnouncementReleaseChip));
            OnPropertyChanged(nameof(HomeLinksReleaseStatus));
            OnPropertyChanged(nameof(HomeLinksReleaseChip));
            OnPropertyChanged(nameof(DiagramReleaseStatus));
            OnPropertyChanged(nameof(DiagramReleaseChip));
            OnPropertyChanged(nameof(GeoReleaseStatus));
            OnPropertyChanged(nameof(GeoReleaseChip));
            OnPropertyChanged(nameof(EpmaReleaseStatus));
            OnPropertyChanged(nameof(EpmaReleaseChip));
            OnPropertyChanged(nameof(HasIncludedPack));
            OnPropertyChanged(nameof(CanStageOrPublish));
            OnPropertyChanged(nameof(CanPublish));
            OnPropertyChanged(nameof(PublishButtonText));
            OnPropertyChanged(nameof(ReleaseSummaryText));
        }

        /// <summary>启用的服务器数量变化后，刷新依赖它的按钮文字和可用状态。</summary>
        private void NotifyPublishTargetsChanged()
        {
            OnPropertyChanged(nameof(EnabledTargetCount));
            OnPropertyChanged(nameof(HasNoEnabledTarget));
            OnPropertyChanged(nameof(PublishButtonText));
            OnPropertyChanged(nameof(CanPublish));
            OnPropertyChanged(nameof(CanPublishSection));
            OnPropertyChanged(nameof(ReleaseSummaryText));
        }

        public ObservableCollection<CosPublishTargetItemViewModel> PublishTargets { get; } = new();

        [ObservableProperty]
        private CosPublishTargetItemViewModel? selectedPublishTarget;

        public int EnabledTargetCount => PublishTargets.Count(target => target.Enabled);

        public bool HasSelectedPublishTarget => SelectedPublishTarget != null;

        partial void OnSelectedPublishTargetChanged(CosPublishTargetItemViewModel? value)
        {
            SecretKey = string.Empty;
            OnPropertyChanged(nameof(HasSelectedPublishTarget));
        }

        public bool HasSelectedHomeLinkGroup => SelectedHomeLinkGroup != null;

        public bool HasSelectedHomeLink => SelectedHomeLink != null;

        public bool HasSelectedPlotCategory => SelectedPlotCategoryEntry != null;

        public bool HasSelectedMineralCategory => SelectedGeoTMineralEntry != null;

        partial void OnSelectedPlotCategoryEntryChanged(LocalizedCategoryEntryViewModel? value)
            => OnPropertyChanged(nameof(HasSelectedPlotCategory));

        partial void OnSelectedGeoTMineralEntryChanged(LocalizedCategoryEntryViewModel? value)
            => OnPropertyChanged(nameof(HasSelectedMineralCategory));

        partial void OnSelectedHomeLinkGroupChanged(HomeLinkGroupEditorViewModel? value)
        {
            OnPropertyChanged(nameof(HasSelectedHomeLinkGroup));
            if (_suppressHomeLinksTracking)
                return;

            SelectedHomeLink = value?.Links.FirstOrDefault();
        }

        partial void OnSelectedHomeLinkChanged(HomeLinkEntryEditorViewModel? value)
        {
            OnPropertyChanged(nameof(HasSelectedHomeLink));
        }

        partial void OnSelectedPreviewFilterChanged(string value)
        {
            DiagramPreviewView?.Refresh();
            GeothermometerPreviewView?.Refresh();
            EpmaPreviewView?.Refresh();
        }

        [RelayCommand]
        private void OpenPublisherSection(string? section)
        {
            if (section is SectionAnnouncement or SectionHomeLinks or SectionDiagrams or SectionGeothermometers or SectionEpma or SectionTargets or SectionRelease)
                SelectedSection = section;
        }

        [RelayCommand]
        private void SetPreviewFilter(string? filter)
        {
            if (filter is "all" or "new" or "update" or "remove" or "same")
                SelectedPreviewFilter = filter;
        }

        [RelayCommand]
        private void SetPlotCategoryLevel(string? level)
        {
            if (!string.IsNullOrWhiteSpace(level) && PlotCategoryLevelKeys.Contains(level))
                SelectedPlotCategoryLevel = level;
        }

        private int _publishExportStepCount;
        private int _publishExportStepIndex;

        private const double PublishExportPhaseStart = 5;
        private const double PublishExportPhaseEnd = 50;
        private const double PublishUploadPhaseStart = 50;
        private const double PublishUploadPhaseEnd = 95;

        [ObservableProperty]
        private bool publishDiagrams;

        partial void OnPublishDiagramsChanged(bool value) => NotifyReleaseSheet();

        [ObservableProperty]
        private bool publishGeothermometers;

        partial void OnPublishGeothermometersChanged(bool value) => NotifyReleaseSheet();

        [ObservableProperty]
        private bool publishEpma;

        partial void OnPublishEpmaChanged(bool value) => NotifyReleaseSheet();

        [ObservableProperty]
        private bool publishHomeLinks;

        partial void OnPublishHomeLinksChanged(bool value) => NotifyReleaseSheet();

        [ObservableProperty]
        private bool publishAnnouncement;

        partial void OnPublishAnnouncementChanged(bool value) => NotifyReleaseSheet();

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
        private string secretKey = string.Empty;

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

        [ObservableProperty]
        private int epmaNewCount;

        [ObservableProperty]
        private int epmaUpdatedCount;

        [ObservableProperty]
        private int epmaRemovedCount;

        [ObservableProperty]
        private int epmaUnchangedCount;

        public ObservableCollection<string> DiagramPreviewLines { get; } = new();
        public ObservableCollection<string> GeothermometerPreviewLines { get; } = new();
        public ObservableCollection<string> EpmaPreviewLines { get; } = new();
        public ObservableCollection<PublishDiffRow> DiagramPreviewItems { get; } = new();
        public ObservableCollection<PublishDiffRow> GeothermometerPreviewItems { get; } = new();
        public ObservableCollection<PublishDiffRow> EpmaPreviewItems { get; } = new();
        public ICollectionView DiagramPreviewView { get; }
        public ICollectionView GeothermometerPreviewView { get; }
        public ICollectionView EpmaPreviewView { get; }

        [ObservableProperty]
        private string selectedPreviewFilter = "all";
        public ObservableCollection<string> HomeLinksPreviewLines { get; } = new();
        public ObservableCollection<string> AnnouncementPreviewLines { get; } = new();
        public ObservableCollection<HomeLinkGroupEditorViewModel> HomeLinkGroups { get; } = new();

        public ContentLanguageContext HomeLinksLanguageContext { get; } = new();

        public ContentLanguageContext PlotCategoriesLanguageContext { get; } = new();

        public ContentLanguageContext GeoTMineralCategoriesLanguageContext { get; } = new();

        public IReadOnlyList<string> PlotCategoryLevelKeys { get; } = new[] { "Level1", "Level2", "Level3" };

        public ObservableCollection<LocalizedCategoryEntryViewModel> PlotCategoryEntries { get; } = new();

        public ObservableCollection<LocalizedCategoryEntryViewModel> GeoTMineralEntries { get; } = new();

        public IReadOnlyList<CultureOption> HomeLinksLanguageOptions { get; } = AppCultureRegistry.GetOptions();

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

        private bool _suppressHomeLinksTracking;
        private bool _suppressCategoryTracking;

        private string _remoteAnnouncementText = string.Empty;
        private string _remoteMinimumSupportedVersion = string.Empty;
        private string _remoteLatestAppVersion = string.Empty;

        private PublishPreview? _diagramPreview;
        private PublishPreview? _geothermometerPreview;
        private PublishPreview? _epmaPreview;

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
            DiagramPreviewView = CollectionViewSource.GetDefaultView(DiagramPreviewItems);
            GeothermometerPreviewView = CollectionViewSource.GetDefaultView(GeothermometerPreviewItems);
            EpmaPreviewView = CollectionViewSource.GetDefaultView(EpmaPreviewItems);
            DiagramPreviewView.Filter = MatchesPreviewFilter;
            GeothermometerPreviewView.Filter = MatchesPreviewFilter;
            EpmaPreviewView.Filter = MatchesPreviewFilter;

            if (bool.TryParse(ConfigHelper.GetConfig(ConfigKeys.DeveloperMode), out bool devMode))
                IsDeveloperMode = devMode;

            var settings = CosPublishSettingsService.Load();
            StagingDirectory = settings.StagingDirectory ?? ConfigHelper.GetConfig(ConfigKeys.PublishStagingDir) ?? string.Empty;
            if (settings.Targets.Count == 0)
                PublishTargets.Add(CreateDefaultPublishTarget());
            else
            {
                foreach (var target in settings.Targets.OrderBy(item => item.SortOrder))
                    PublishTargets.Add(CosPublishTargetItemViewModel.FromModel(target));
            }

            foreach (var target in PublishTargets)
                target.PropertyChanged += OnPublishTargetPropertyChanged;
            SelectedPublishTarget = PublishTargets.FirstOrDefault();

            Log(LanguageService.Instance["official_publisher_ready"] ?? "Official publisher ready.");
            if (!IsDeveloperMode)
                Log(LanguageService.Instance["official_publisher_dev_mode_required"] ?? "Developer mode is required.");

            SelectedHomeLinksLanguage = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            HomeLinksLanguageContext.ContentLanguage = SelectedHomeLinksLanguage;
            SelectedPlotCategoriesLanguage = SelectedHomeLinksLanguage;
            PlotCategoriesLanguageContext.ContentLanguage = SelectedPlotCategoriesLanguage;
            SelectedGeoTMineralCategoriesLanguage = SelectedHomeLinksLanguage;
            GeoTMineralCategoriesLanguageContext.ContentLanguage = SelectedGeoTMineralCategoriesLanguage;
            SelectedAnnouncementsLanguage = SelectedHomeLinksLanguage;
            AnnouncementsLanguageContext.ContentLanguage = SelectedAnnouncementsLanguage;
            AnnouncementPreviewLanguage = SelectedHomeLinksLanguage;
            SelectedEditLanguage = SelectedHomeLinksLanguage;
            RefreshAnnouncementTranslationChips();
            AnnouncementEntries.CollectionChanged += OnAnnouncementEntriesChanged;
            LoadHomeLinksEditor();
            LoadPlotCategoriesEditor();
            LoadGeoTMineralCategoriesEditor();
            _ = InitializeHomeLinksEditorAsync();
            _ = InitializeRemoteStateAsync();
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
                StringComparison.Ordinal)
                || AnnouncementsCatalogHasRemoteChanges();

            MinimumSupportedVersionHasRemoteChanges = !string.Equals(
                MinimumSupportedVersionText?.Trim(),
                _remoteMinimumSupportedVersion?.Trim(),
                StringComparison.OrdinalIgnoreCase);

            LatestAppVersionHasRemoteChanges = !string.Equals(
                LatestAppVersionText?.Trim(),
                _remoteLatestAppVersion?.Trim(),
                StringComparison.OrdinalIgnoreCase);
            NotifyReleaseSheet();
            RequestDraftSave(PublisherDraftKinds.Announcements);
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
            if (PublishAnnouncement)
                steps++;
            if (PublishHomeLinks && !exportDiagrams)
                steps++;
            if (exportDiagrams)
                steps++;
            if (exportGeothermometers)
                steps++;
            if (PublishEpma)
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
                ConfigHelper.SetConfig(ConfigKeys.PublishStagingDir, StagingDirectory);
                PersistPublishTargets();
            }
        }

        [RelayCommand]
        private void SaveCosSettings()
        {
            if (SelectedPublishTarget == null)
            {
                ShowWarning(LanguageService.GetString("official_publisher_no_target", "Add a server first."));
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedPublishTarget.SecretId))
            {
                ShowWarning(LanguageService.Instance["cos_secret_id_required"] ?? "SecretId is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedPublishTarget.Bucket))
            {
                ShowWarning(LanguageService.GetString("official_publisher_bucket_required", "Bucket is required."));
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedPublishTarget.PublicBaseUrl))
            {
                ShowWarning(LanguageService.GetString("official_publisher_public_url_required", "Public base URL is required."));
                return;
            }

            if (string.IsNullOrWhiteSpace(SecretKey) && string.IsNullOrWhiteSpace(SelectedPublishTarget.ProtectedSecretKey))
            {
                ShowWarning(LanguageService.Instance["cos_credentials_required"] ?? "Configure COS credentials first.");
                return;
            }

            ApplyPendingSecretKey();
            PersistPublishTargets();
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
                var target = BuildSelectedTarget();
                if (target == null || string.IsNullOrWhiteSpace(target.SecretId) || string.IsNullOrWhiteSpace(target.Bucket))
                {
                    ShowWarning(LanguageService.Instance["cos_credentials_required"] ?? "Configure COS credentials first.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(SecretKey) && string.IsNullOrWhiteSpace(target.ProtectedSecretKey))
                {
                    ShowWarning(LanguageService.Instance["cos_credentials_required"] ?? "Configure COS credentials first.");
                    return;
                }

                string? plainKey = string.IsNullOrWhiteSpace(SecretKey) ? null : SecretKey.Trim();
                bool ok = await TencentCosPublishService.TestConnectionAsync(target, plainKey);
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
            EpmaPreviewLines.Clear();
            try
            {
                Log("正在刷新预览");

                _diagramPreview = await GraphMapTemplatePublishService.GetPublishPreviewAsync();
                DiagramNewCount = _diagramPreview.NewItems.Count;
                DiagramUpdatedCount = _diagramPreview.UpdatedItems.Count;
                DiagramRemovedCount = _diagramPreview.RemovedItems.Count;
                DiagramUnchangedCount = _diagramPreview.UnchangedItems.Count;
                FillDiffRows(DiagramPreviewItems, _diagramPreview);
                Log($"图解：新增 {DiagramNewCount}，更新 {DiagramUpdatedCount}，移除 {DiagramRemovedCount}，未变化 {DiagramUnchangedCount}");

                _geothermometerPreview = await GeothermometerPublishService.GetPublishPreviewAsync();
                GeoNewCount = _geothermometerPreview.NewItems.Count;
                GeoUpdatedCount = _geothermometerPreview.UpdatedItems.Count;
                GeoRemovedCount = _geothermometerPreview.RemovedItems.Count;
                GeoUnchangedCount = _geothermometerPreview.UnchangedItems.Count;
                FillDiffRows(GeothermometerPreviewItems, _geothermometerPreview);
                Log($"温压计：新增 {GeoNewCount}，更新 {GeoUpdatedCount}，移除 {GeoRemovedCount}，未变化 {GeoUnchangedCount}");

                _epmaPreview = await EpmaPublishService.GetPublishPreviewAsync();
                EpmaNewCount = _epmaPreview.NewItems.Count;
                EpmaUpdatedCount = _epmaPreview.UpdatedItems.Count;
                EpmaRemovedCount = _epmaPreview.RemovedItems.Count;
                EpmaUnchangedCount = _epmaPreview.UnchangedItems.Count;
                FillDiffRows(EpmaPreviewItems, _epmaPreview);
                Log($"EPMA：新增 {EpmaNewCount}，更新 {EpmaUpdatedCount}，移除 {EpmaRemovedCount}，未变化 {EpmaUnchangedCount}");

                HomeLinksPreviewLines.Clear();
                var catalog = BuildCatalogFromEditor();
                UpdateHomeLinkCounts(catalog);
                var homePreview = await HomeLinksPublishService.GetPublishPreviewAsync(catalog);
                HomeLinksHasRemoteChanges = homePreview.HasRemoteChanges;
                Log(homePreview.HasRemoteChanges
                    ? $"导航链接有改动：{homePreview.GroupCount} 个分组，{homePreview.LinkCount} 个链接"
                    : $"导航链接已与服务器一致：{homePreview.GroupCount} 个分组，{homePreview.LinkCount} 个链接");

                {
                    AnnouncementPreviewLines.Clear();
                    var remoteInfo = await LoadRemoteServerInfoAsync();
                    // 刚发布完时 CDN 可能还没刷新，沿用刚发布的值作为对照基线，避免误报“有改动”
                    if (!_keepAnnouncementBaseline)
                    {
                        _remoteAnnouncementText = remoteInfo?.Announcement ?? string.Empty;
                        _remoteMinimumSupportedVersion = remoteInfo?.MinimumSupportedVersion ?? string.Empty;
                        _remoteLatestAppVersion = remoteInfo?.LatestAppVersion ?? string.Empty;
                    }
                    UpdateServerConfigChangeState();

                    AnnouncementPreviewLines.Add(AnnouncementHasRemoteChanges
                        ? (LanguageService.Instance["official_publisher_announcement_pending"] ?? "Pending publish.")
                        : (LanguageService.Instance["official_publisher_announcement_up_to_date"] ?? "Already up to date on server."));

                    AnnouncementPreviewLines.Add(MinimumSupportedVersionHasRemoteChanges
                        ? (LanguageService.Instance["official_publisher_minimum_version_pending"] ?? "Minimum supported version pending publish.")
                        : (LanguageService.Instance["official_publisher_minimum_version_up_to_date"] ?? "Minimum supported version is up to date."));

                    AnnouncementPreviewLines.Add(LatestAppVersionHasRemoteChanges
                        ? LanguageService.GetString("official_publisher_latest_version_pending", "Latest app version pending publish.")
                        : LanguageService.GetString("official_publisher_latest_version_up_to_date", "Latest app version is up to date."));

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
                        ? LanguageService.GetString("official_publisher_latest_version_empty", "Latest app version: (empty)")
                        : string.Format(
                            LanguageService.GetString("official_publisher_latest_version_preview", "Latest app version: {0}"),
                            normalizedLatestVersion));

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

                    var announcementsCatalogPreview = BuildAnnouncementsCatalogFromEditor();
                    AnnouncementPreviewLines.Add(string.Format(
                        LanguageService.GetString(
                            "official_publisher_announcements_preview",
                            "Announcement catalog: {0} entries. Remote hash: {1}."),
                        announcementsCatalogPreview.Announcements.Count,
                        string.IsNullOrEmpty(remoteInfo?.AnnouncementsHash) ? "(none)" : remoteInfo.AnnouncementsHash));
                    ReportAnnouncementPublishWarnings(AnnouncementPreviewLines);

                    Log(AnnouncementHasRemoteChanges ? "公告与版本有改动" : "公告与版本已与服务器一致");
                }

                Log("预览已刷新");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
                NotifyReleaseSheet();
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
            if (!EnsureDiagramsDownloadedForPublish()) return;

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
                HomeAnnouncementCatalog? publishedAnnouncementsCatalog = null;

                if (PublishAnnouncement)
                {
                    ReportAnnouncementPublishWarnings();
                    // 先导出公告（含多语言公告目录），后续主页链接/图解导出会从暂存目录合并 server_info，保留 announcements_hash
                    publishedAnnouncementsCatalog = BuildAnnouncementsCatalogFromEditor();
                    var announcementsCatalog = publishedAnnouncementsCatalog;
                    announcementResult = await Task.Run(() =>
                        HomeLinksPublishService.ExportAnnouncementToDirectory(outputDir, announcement, minimumSupportedVersion, latestAppVersion, announcementsCatalog));
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

                if (PublishEpma)
                {
                    var epmaResult = await Task.Run(() => EpmaPublishService.ExportToDirectory(outputDir));
                    Log(epmaResult.Summary);
                }

                MessageHelper.ExportSuccess(
                    LanguageService.Instance["official_publisher_export_done"] ?? "Export completed.",
                    outputDir,
                    OwnerWindow);
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
            if (!EnsureDiagramsDownloadedForPublish()) return;

            ApplyPendingSecretKey();
            var targets = GetConfiguredTargets();
            if (targets.Count == 0)
            {
                ShowWarning(LanguageService.GetString(
                    "official_publisher_no_enabled_target",
                    "There is no enabled server with complete credentials."));
                return;
            }

            string? outputDir = ResolveStagingDirectory();
            if (string.IsNullOrEmpty(outputDir)) return;

            var included = new List<string>();
            if (PublishAnnouncement) included.Add($"公告与版本（{AnnouncementReleaseChip}）");
            if (PublishHomeLinks) included.Add($"导航链接（{HomeLinksReleaseChip}）");
            if (PublishDiagrams) included.Add($"图解与分类（{DiagramReleaseChip}）");
            if (PublishGeothermometers) included.Add($"温压计与矿物分类（{GeoReleaseChip}）");
            if (PublishEpma) included.Add($"EPMA 计算模板（{EpmaReleaseChip}）");
            string serverNames = string.Join("、", targets.Select(target =>
                string.IsNullOrWhiteSpace(target.Name) ? target.Bucket : target.Name.Trim()));
            bool confirm = await ShowConfirmAsync(
                $"发布这些内容？\n{string.Join("\n", included.Select(item => "· " + item))}\n\n发布到：{serverNames}",
                "取消",
                "发布");
            if (!confirm) return;

            var (exportDiagrams, exportGeothermometers) = await ResolveCategoryExportFlagsAsync();
            bool published = false;

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
                EpmaPublishResult? epmaResult = null;
                HomeLinksPublishResult? homeLinksResult = null;
                AnnouncementPublishResult? announcementResult = null;
                HomeAnnouncementCatalog? publishedAnnouncementsCatalog = null;

                if (PublishAnnouncement)
                {
                    string exportMessage = LanguageService.Instance["official_publisher_progress_exporting_announcement"]
                        ?? "Exporting announcement...";
                    ReportPublishExportStep(exportMessage);
                    ReportAnnouncementPublishWarnings();
                    // 先导出公告（含多语言公告目录），后续主页链接/图解导出会从暂存目录合并 server_info，保留 announcements_hash
                    publishedAnnouncementsCatalog = BuildAnnouncementsCatalogFromEditor();
                    var announcementsCatalog = publishedAnnouncementsCatalog;
                    announcementResult = await Task.Run(() =>
                        HomeLinksPublishService.ExportAnnouncementToDirectory(outputDir, announcement, minimumSupportedVersion, latestAppVersion, announcementsCatalog));
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

                if (PublishEpma)
                {
                    string exportMessage = LanguageService.GetString(
                        "official_publisher_progress_exporting_epma", "Exporting EPMA calculation templates...");
                    ReportPublishExportStep(exportMessage);
                    epmaResult = await Task.Run(() => EpmaPublishService.ExportToDirectory(outputDir));
                    Log(epmaResult.Summary);
                }

                string? serverInfoPath = diagramResult?.ServerInfoPath;
                if (string.IsNullOrEmpty(serverInfoPath) || !File.Exists(serverInfoPath))
                    serverInfoPath = homeLinksResult?.ServerInfoPath;
                if (string.IsNullOrEmpty(serverInfoPath) || !File.Exists(serverInfoPath))
                    serverInfoPath = announcementResult?.ServerInfoPath;
                OfficialContentMirrorClient.StampServerInfoFile(
                    serverInfoPath,
                    targets.Select(target => target.PublicBaseUrl));

                var logProgress = new Progress<string>(Log);
                var failures = new List<string>();
                int succeeded = 0;
                double serverSpan = targets.Count == 0
                    ? 0
                    : (PublishUploadPhaseEnd - PublishUploadPhaseStart) / targets.Count;

                for (int index = 0; index < targets.Count; index++)
                {
                    var target = targets[index];
                    string targetName = target.DisplayName;
                    int serverNumber = index + 1;
                    var uploadProgress = new Progress<(int current, int total)>(progress =>
                    {
                        if (progress.total <= 0)
                            return;

                        double uploadFraction = progress.current / (double)progress.total;
                        double value = PublishUploadPhaseStart + serverSpan * index + serverSpan * uploadFraction;
                        string message = string.Format(
                            LanguageService.GetString(
                                "official_publisher_progress_server",
                                "Uploading to {0} ({1}/{2})..."),
                            targetName,
                            serverNumber,
                            targets.Count);
                        ReportPublishProgress(value, message);
                    });

                    string uploadingMessage = string.Format(
                        LanguageService.GetString(
                            "official_publisher_progress_server",
                            "Uploading to {0} ({1}/{2})..."),
                        targetName,
                        serverNumber,
                        targets.Count);
                    ReportPublishProgress(PublishUploadPhaseStart + serverSpan * index, uploadingMessage);
                    Log(uploadingMessage);

                    try
                    {
                        var uploadResult = await TencentCosPublishService.UploadCombinedPublishAsync(
                            outputDir,
                            diagramResult,
                            geoResult,
                            homeLinksResult,
                            announcementResult,
                            target,
                            exportDiagrams,
                            exportGeothermometers,
                            PublishHomeLinks,
                            PublishAnnouncement,
                            logProgress,
                            uploadProgress,
                            epmaResult,
                            PublishEpma);
                        succeeded++;
                        Log($"{targetName}: {uploadResult.Message}");
                    }
                    catch (Exception ex)
                    {
                        failures.Add(targetName);
                        Log(string.Format(
                            LanguageService.GetString(
                                "official_publisher_upload_target_failed",
                                "{0} failed: {1}"),
                            targetName,
                            ex.Message));
                    }
                }

                if (succeeded == 0)
                    throw new InvalidOperationException(LanguageService.GetString(
                        "official_publisher_upload_all_failed",
                        "Every enabled server failed."));

                if (exportDiagrams && PublishDiagrams)
                {
                    GraphMapTemplatePublishService.ClearPendingPublishFlags();
                    WeakReferenceMessenger.Default.Send(new OfficialTemplatesPublishedMessage());
                }

                if (exportGeothermometers && geoResult != null)
                {
                    var publishedGeoResult = geoResult;
                    await Task.Run(() => GeothermometerPublishService.ApplyPublishedResult(publishedGeoResult));
                }

                if (PublishEpma && epmaResult != null)
                {
                    await EpmaPublishService.ApplyPublishedResultAsync(epmaResult);
                }

                if (PublishAnnouncement && succeeded > 0)
                {
                    _remoteAnnouncementText = announcement ?? string.Empty;
                    _remoteMinimumSupportedVersion = minimumSupportedVersion ?? string.Empty;
                    _remoteLatestAppVersion = latestAppVersion ?? string.Empty;
                    if (publishedAnnouncementsCatalog != null)
                        RememberRemoteAnnouncementsCatalog(publishedAnnouncementsCatalog, known: true);
                    UpdateServerConfigChangeState();
                }

                string doneMessage = failures.Count == 0
                    ? (LanguageService.GetString("official_publisher_upload_all_ok", "Published to every enabled server."))
                    : (LanguageService.GetString("official_publisher_upload_partial", "Some servers failed."));
                ReportPublishProgress(100, doneMessage);
                published = true;
                Log(doneMessage);
                if (failures.Count == 0)
                    ShowSuccess(doneMessage);
                else
                    ShowWarning(doneMessage);
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

            // 发布后立刻对照服务器重新计算，发布单上的“有改动”标签才不会过期
            if (published)
            {
                _keepAnnouncementBaseline = PublishAnnouncement;
                try
                {
                    await RefreshPreview();
                }
                finally
                {
                    _keepAnnouncementBaseline = false;
                }
            }
        }

        private CosPublishTarget? BuildSelectedTarget()
        {
            if (SelectedPublishTarget == null)
                return null;

            ApplyPendingSecretKey();
            return SelectedPublishTarget.ToModel();
        }

        private void ApplyPendingSecretKey()
        {
            if (SelectedPublishTarget == null || string.IsNullOrWhiteSpace(SecretKey))
                return;

            SelectedPublishTarget.ProtectedSecretKey = CosPublishSettingsService.ProtectSecretKey(SecretKey.Trim());
            SelectedPublishTarget.NotifySecretKeyChanged();
        }

        private List<CosPublishTarget> GetConfiguredTargets()
        {
            PersistPublishTargets();
            return PublishTargets
                .Select(item => item.ToModel())
                .Where(target => target.Enabled && target.IsConfigured)
                .ToList();
        }

        private void PersistPublishTargets()
        {
            var settings = new CosPublishSettings
            {
                StagingDirectory = StagingDirectory ?? string.Empty
            };

            for (int index = 0; index < PublishTargets.Count; index++)
            {
                var model = PublishTargets[index].ToModel();
                model.SortOrder = index;
                PublishTargets[index].SortOrder = index;
                settings.Targets.Add(model);
            }

            CosPublishSettingsService.Save(settings);
            NotifyPublishTargetsChanged();
        }

        private CosPublishTargetItemViewModel CreateDefaultPublishTarget()
        {
            return new CosPublishTargetItemViewModel
            {
                Name = LanguageService.GetString("official_publisher_default_target_name", "Hong Kong"),
                Enabled = true,
                Region = OfficialContentEndpoints.DefaultRegion,
                Bucket = OfficialContentEndpoints.DefaultBucket,
                PublicBaseUrl = OfficialContentEndpoints.CosBaseUrl
            };
        }

        private void OnPublishTargetPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CosPublishTargetItemViewModel.Enabled))
                NotifyPublishTargetsChanged();
        }

        [RelayCommand]
        private void AddPublishTarget()
        {
            var target = new CosPublishTargetItemViewModel
            {
                Name = LanguageService.GetString("official_publisher_new_target_name", "New server"),
                Enabled = true,
                Region = OfficialContentEndpoints.DefaultRegion
            };
            target.PropertyChanged += OnPublishTargetPropertyChanged;
            PublishTargets.Add(target);
            SelectedPublishTarget = target;
            SecretKey = string.Empty;
            NotifyPublishTargetsChanged();
        }

        [RelayCommand]
        private void RemovePublishTarget()
        {
            if (SelectedPublishTarget == null)
                return;

            var removing = SelectedPublishTarget;
            int index = PublishTargets.IndexOf(removing);
            removing.PropertyChanged -= OnPublishTargetPropertyChanged;
            PublishTargets.Remove(removing);
            SelectedPublishTarget = PublishTargets.Count == 0
                ? null
                : PublishTargets[Math.Min(index, PublishTargets.Count - 1)];
            SecretKey = string.Empty;
            PersistPublishTargets();
        }

        [RelayCommand]
        private void MovePublishTargetUp()
        {
            if (SelectedPublishTarget == null)
                return;

            int index = PublishTargets.IndexOf(SelectedPublishTarget);
            if (index <= 0)
                return;

            PublishTargets.Move(index, index - 1);
            PersistPublishTargets();
        }

        [RelayCommand]
        private void MovePublishTargetDown()
        {
            if (SelectedPublishTarget == null)
                return;

            int index = PublishTargets.IndexOf(SelectedPublishTarget);
            if (index < 0 || index >= PublishTargets.Count - 1)
                return;

            PublishTargets.Move(index, index + 1);
            PersistPublishTargets();
        }

        private string? ResolveStagingDirectory()
        {
            if (!string.IsNullOrWhiteSpace(StagingDirectory))
            {
                ConfigHelper.SetConfig(ConfigKeys.PublishStagingDir, StagingDirectory);
                return StagingDirectory;
            }

            string? folder = FileHelper.GetFolderPath();
            if (!string.IsNullOrEmpty(folder))
            {
                StagingDirectory = folder;
                ConfigHelper.SetConfig(ConfigKeys.PublishStagingDir, StagingDirectory);
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

        /// <summary>
        /// 发布图解前检查：存在尚未下载的官方图解（仅目录占位）时禁止发布。
        /// </summary>
        private bool EnsureDiagramsDownloadedForPublish()
        {
            if (!PublishDiagrams) return true;
            string? message = GraphMapTemplatePublishService.BuildNotDownloadedPublishBlockMessage();
            if (message == null) return true;
            Log(message);
            ShowWarning(message);
            return false;
        }

        private Task<bool> EnsurePublishTargetSelectedAsync()
        {
            if (PublishDiagrams || PublishGeothermometers || PublishEpma || PublishHomeLinks || PublishAnnouncement)
                return Task.FromResult(true);

            ShowWarning("先在发布单里勾选要发布的内容。");
            return Task.FromResult(false);
        }

        private Task<(bool exportDiagrams, bool exportGeothermometers)> ResolveCategoryExportFlagsAsync()
        {
            return Task.FromResult((PublishDiagrams, PublishGeothermometers));
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
            int nextOrder = HomeLinkGroups.Count == 0 ? 0 : HomeLinkGroups.Max(g => g.SortOrder) + 1;
            var group = new HomeLinkGroupEditorViewModel(HomeLinksLanguageContext)
            {
                Id = "group-" + Guid.NewGuid().ToString("N")[..6],
                Title = new LocalizedString(),
                SortOrder = nextOrder
            };
            HookHomeLinkGroup(group);
            HomeLinkGroups.Add(group);
            SelectedHomeLinkGroup = group;
            MarkHomeLinksDirty();
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
            WindowActivationHelper.AttachOwnerFocusPreservation(dialog, dialog.Owner);
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

            UnhookHomeLinkGroup(SelectedHomeLinkGroup);
            HomeLinkGroups.Remove(SelectedHomeLinkGroup);
            SelectedHomeLinkGroup = HomeLinkGroups.FirstOrDefault();
            SelectedHomeLink = null;
            MarkHomeLinksDirty();
        }

        [RelayCommand]
        private void AddHomeLink()
        {
            if (SelectedHomeLinkGroup == null)
            {
                ShowWarning(LanguageService.Instance["official_publisher_home_select_group"] ?? "Select a group first.");
                return;
            }

            var link = new HomeLinkEntryEditorViewModel(HomeLinksLanguageContext)
            {
                Id = "link-" + Guid.NewGuid().ToString("N")[..6],
                Title = new LocalizedString(),
                Description = new LocalizedString(),
                Url = "https://",
                Icon = HomeIconHelper.DefaultIcon
            };
            SelectedHomeLinkGroup.Links.Add(link);
            SelectedHomeLink = link;
            MarkHomeLinksDirty();
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
            MarkHomeLinksDirty();
        }

        [RelayCommand]
        private void SaveHomeLinksDraft()
        {
            if (!EnsureDeveloperMode())
                return;

            SaveHomeLinksToLocal();
            Log("导航链接已保存");
            ShowSuccess("导航链接已保存");
        }

        [RelayCommand]
        private async Task ExportHomeLinksCatalog()
        {
            string? path = await FileHelper.GetSaveFilePath2Async(
                "导出导航链接",
                FileDialogFilterHelper.JsonOnly,
                ".json",
                "HomeLinksCatalog.json",
                OwnerWindow);
            if (string.IsNullOrWhiteSpace(path))
                return;

            File.WriteAllText(path, HomeLinksPublishService.SerializeCatalog(BuildCatalogFromEditor()));
            Log("已导出导航链接");
            ShowSuccess("已导出导航链接");
        }

        [RelayCommand]
        private async Task ImportHomeLinksCatalog()
        {
            string? path = FileHelper.GetFilePath(FileDialogFilterHelper.JsonOnly, OwnerWindow);
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!HomeLinksPublishService.TryDeserializeCatalog(JsonHelper.ReadJsonFile(path), out var catalog))
            {
                ShowWarning("这个文件不是导航链接目录。");
                return;
            }

            if (HomeLinkGroups.Count > 0)
            {
                bool confirmed = await ShowConfirmAsync("导入会替换当前导航链接。", "取消", "导入");
                if (!confirmed)
                    return;
            }

            PopulateHomeLinksEditor(catalog);
            MarkHomeLinksDirty();
            Log($"已导入导航链接：{HomeLinkGroupCount} 个分组，{HomeLinkCount} 个链接。");
            ShowSuccess("已导入导航链接");
        }

        private void LoadHomeLinksEditor()
        {
            PopulateHomeLinksEditor(HomeLinksPublishService.LoadPublisherCatalog());
        }

        private void PopulateHomeLinksEditor(HomeLinksCatalog catalog)
        {
            _suppressHomeLinksTracking = true;
            try
            {
                foreach (var existing in HomeLinkGroups.ToList())
                    UnhookHomeLinkGroup(existing);
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

                    HookHomeLinkGroup(groupVm);
                    HomeLinkGroups.Add(groupVm);
                }

                SelectedHomeLinkGroup = HomeLinkGroups.FirstOrDefault();
                SelectedHomeLink = SelectedHomeLinkGroup?.Links.FirstOrDefault();
                UpdateHomeLinkCounts(catalog);
            }
            finally
            {
                _suppressHomeLinksTracking = false;
            }
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

        private void HookHomeLinkGroup(HomeLinkGroupEditorViewModel group)
        {
            group.PropertyChanged += OnHomeLinkEditorChanged;
            group.Links.CollectionChanged += OnHomeLinkListChanged;
            foreach (var link in group.Links)
                link.PropertyChanged += OnHomeLinkEditorChanged;
        }

        private void UnhookHomeLinkGroup(HomeLinkGroupEditorViewModel group)
        {
            group.PropertyChanged -= OnHomeLinkEditorChanged;
            group.Links.CollectionChanged -= OnHomeLinkListChanged;
            foreach (var link in group.Links)
                link.PropertyChanged -= OnHomeLinkEditorChanged;
        }

        private void OnHomeLinkListChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (HomeLinkEntryEditorViewModel link in e.NewItems)
                    link.PropertyChanged += OnHomeLinkEditorChanged;
            }

            if (e.OldItems != null)
            {
                foreach (HomeLinkEntryEditorViewModel link in e.OldItems)
                    link.PropertyChanged -= OnHomeLinkEditorChanged;
            }

            MarkHomeLinksDirty();
        }

        private void OnHomeLinkEditorChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(HomeLinkEntryEditorViewModel.DisplayTitle)
                or nameof(HomeLinkEntryEditorViewModel.DisplayDescription)
                or nameof(HomeLinkGroupEditorViewModel.DisplayTitle))
                return;

            MarkHomeLinksDirty();
        }

        private void MarkHomeLinksDirty()
        {
            if (_suppressHomeLinksTracking)
                return;

            HomeLinksHasRemoteChanges = true;
            UpdateHomeLinkCounts(BuildCatalogFromEditor());
            RequestDraftSave(PublisherDraftKinds.HomeLinks);
        }

        private void UpdateHomeLinkCounts(HomeLinksCatalog catalog)
        {
            HomeLinkGroupCount = catalog?.Groups?.Count ?? 0;
            HomeLinkCount = catalog?.Groups?
                .SelectMany(g => g.Links ?? Enumerable.Empty<HomeLinkEntry>())
                .Count() ?? 0;
            NotifyReleaseSheet();
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
            WindowActivationHelper.AttachOwnerFocusPreservation(dialog, dialog.Owner);

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
                string json = await OfficialContentMirrorClient.GetStringAsync(OfficialContentEndpoints.ServerInfoFileName);
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
            Log("图解分类已保存");
            ShowSuccess("图解分类已保存");
        }

        [RelayCommand]
        private async Task ExportPlotCategories()
        {
            string? path = await FileHelper.GetSaveFilePath2Async(
                "导出图解分类",
                FileDialogFilterHelper.JsonOnly,
                ".json",
                "PlotTemplateCategories.json",
                OwnerWindow);
            if (string.IsNullOrWhiteSpace(path))
                return;

            PublisherConfigHelper.ExportRelaxed(BuildPlotCategoryConfigFromEditor(), path);
            Log("已导出图解分类");
            ShowSuccess("已导出图解分类");
        }

        [RelayCommand]
        private async Task ImportPlotCategories()
        {
            string? path = FileHelper.GetFilePath(FileDialogFilterHelper.JsonOnly, OwnerWindow);
            if (string.IsNullOrWhiteSpace(path))
                return;

            string? json = JsonHelper.ReadJsonFile(path);
            var config = string.IsNullOrWhiteSpace(json) ? null : JsonHelper.Deserialize<PlotTemplateCategoryConfig>(json);
            if (config == null || !config.Keys.Any(key => key.StartsWith("Level", StringComparison.OrdinalIgnoreCase)))
            {
                ShowWarning("这个文件不是图解分类。");
                return;
            }

            if (_plotCategoryEntriesByLevel.Values.Any(list => list.Count > 0))
            {
                bool confirmed = await ShowConfirmAsync("导入会替换当前图解分类。", "取消", "导入");
                if (!confirmed)
                    return;
            }

            LoadPlotCategories(config);
            PublishDiagrams = true;
            Log("已导入图解分类");
            ShowSuccess("已导入图解分类");
        }

        [RelayCommand]
        private void AddPlotCategoryEntry()
        {
            var entry = new LocalizedCategoryEntryViewModel(PlotCategoriesLanguageContext);
            entry.PropertyChanged += OnPlotCategoryEntryChanged;
            GetPlotCategoryCollection(SelectedPlotCategoryLevel).Add(entry);
            PlotCategoryEntries.Add(entry);
            SelectedPlotCategoryEntry = entry;
            UpdatePlotCategoryEntryCount();
            PublishDiagrams = true;
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

            SelectedPlotCategoryEntry.PropertyChanged -= OnPlotCategoryEntryChanged;
            GetPlotCategoryCollection(SelectedPlotCategoryLevel).Remove(SelectedPlotCategoryEntry);
            PlotCategoryEntries.Remove(SelectedPlotCategoryEntry);
            SelectedPlotCategoryEntry = PlotCategoryEntries.FirstOrDefault();
            UpdatePlotCategoryEntryCount();
            PublishDiagrams = true;
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
            Log("矿物分类已保存");
            ShowSuccess("矿物分类已保存");
        }

        [RelayCommand]
        private async Task ExportGeoTMineralCategories()
        {
            string? path = await FileHelper.GetSaveFilePath2Async(
                "导出矿物分类",
                FileDialogFilterHelper.JsonOnly,
                ".json",
                "GeoTMineralCategories.json",
                OwnerWindow);
            if (string.IsNullOrWhiteSpace(path))
                return;

            PublisherConfigHelper.ExportRelaxed(BuildGeoTMineralCategoryConfigFromEditor(), path);
            Log("已导出矿物分类");
            ShowSuccess("已导出矿物分类");
        }

        [RelayCommand]
        private async Task ImportGeoTMineralCategories()
        {
            string? path = FileHelper.GetFilePath(FileDialogFilterHelper.JsonOnly, OwnerWindow);
            if (string.IsNullOrWhiteSpace(path))
                return;

            string? json = JsonHelper.ReadJsonFile(path);
            var config = string.IsNullOrWhiteSpace(json) ? null : JsonHelper.Deserialize<GeoTMineralCategoryConfig>(json);
            if (string.IsNullOrWhiteSpace(json) || config == null || json.IndexOf("minerals", StringComparison.OrdinalIgnoreCase) < 0)
            {
                ShowWarning("这个文件不是矿物分类。");
                return;
            }

            if (GeoTMineralEntries.Count > 0)
            {
                bool confirmed = await ShowConfirmAsync("导入会替换当前矿物分类。", "取消", "导入");
                if (!confirmed)
                    return;
            }

            config.Minerals ??= new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>();
            LoadGeoTMineralCategories(config);
            PublishGeothermometers = true;
            Log("已导入矿物分类");
            ShowSuccess("已导入矿物分类");
        }

        [RelayCommand]
        private void AddGeoTMineralEntry()
        {
            var entry = new LocalizedCategoryEntryViewModel(GeoTMineralCategoriesLanguageContext);
            entry.PropertyChanged += OnGeoTMineralEntryChanged;
            GeoTMineralEntries.Add(entry);
            SelectedGeoTMineralEntry = entry;
            UpdateGeoTMineralEntryCount();
            PublishGeothermometers = true;
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

            SelectedGeoTMineralEntry.PropertyChanged -= OnGeoTMineralEntryChanged;
            GeoTMineralEntries.Remove(SelectedGeoTMineralEntry);
            SelectedGeoTMineralEntry = GeoTMineralEntries.FirstOrDefault();
            UpdateGeoTMineralEntryCount();
            PublishGeothermometers = true;
        }

        private void LoadPlotCategoriesEditor()
        {
            LoadPlotCategories(PlotCategoryHelper.LoadPublisherConfig());
        }

        private void LoadPlotCategories(PlotTemplateCategoryConfig config)
        {
            _suppressCategoryTracking = true;
            try
            {
                foreach (var existing in _plotCategoryEntriesByLevel.Values.SelectMany(entries => entries))
                    existing.PropertyChanged -= OnPlotCategoryEntryChanged;

                _plotCategoryEntriesByLevel.Clear();
                config ??= new PlotTemplateCategoryConfig();

                foreach (string level in PlotCategoryLevelKeys)
                {
                    var entries = new ObservableCollection<LocalizedCategoryEntryViewModel>();
                    if (config.TryGetValue(level, out var items))
                    {
                        foreach (var item in items ?? Enumerable.Empty<Dictionary<string, string>>())
                        {
                            var entry = new LocalizedCategoryEntryViewModel(PlotCategoriesLanguageContext)
                            {
                                Title = HomeLinksLocalization.FromDictionary(item)
                            };
                            entry.PropertyChanged += OnPlotCategoryEntryChanged;
                            entries.Add(entry);
                        }
                    }

                    _plotCategoryEntriesByLevel[level] = entries;
                }

                RefreshPlotCategoryEntriesForSelectedLevel();
            }
            finally
            {
                _suppressCategoryTracking = false;
            }
        }

        private void LoadGeoTMineralCategoriesEditor()
        {
            LoadGeoTMineralCategories(GeoTMineralCategoryHelper.LoadPublisherConfig());
        }

        private void LoadGeoTMineralCategories(GeoTMineralCategoryConfig config)
        {
            _suppressCategoryTracking = true;
            try
            {
                foreach (var existing in GeoTMineralEntries)
                    existing.PropertyChanged -= OnGeoTMineralEntryChanged;

                GeoTMineralEntries.Clear();
                config ??= new GeoTMineralCategoryConfig();

                foreach (var item in config.Minerals ?? Enumerable.Empty<Dictionary<string, string>>())
                {
                    var entry = new LocalizedCategoryEntryViewModel(GeoTMineralCategoriesLanguageContext)
                    {
                        Title = HomeLinksLocalization.FromDictionary(item)
                    };
                    entry.PropertyChanged += OnGeoTMineralEntryChanged;
                    GeoTMineralEntries.Add(entry);
                }

                SelectedGeoTMineralEntry = GeoTMineralEntries.FirstOrDefault();
                UpdateGeoTMineralEntryCount();
            }
            finally
            {
                _suppressCategoryTracking = false;
            }
        }

        private void OnPlotCategoryEntryChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_suppressCategoryTracking || e.PropertyName != nameof(LocalizedCategoryEntryViewModel.Title))
                return;

            PublishDiagrams = true;
            RequestDraftSave(PublisherDraftKinds.PlotCategories);
        }

        private void OnGeoTMineralEntryChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_suppressCategoryTracking || e.PropertyName != nameof(LocalizedCategoryEntryViewModel.Title))
                return;

            PublishGeothermometers = true;
            RequestDraftSave(PublisherDraftKinds.GeoTCategories);
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
            if (!_suppressCategoryTracking)
                RequestDraftSave(PublisherDraftKinds.PlotCategories);
        }

        private void UpdateGeoTMineralEntryCount()
        {
            GeoTMineralEntryCount = GeoTMineralEntries.Count;
            if (!_suppressCategoryTracking)
                RequestDraftSave(PublisherDraftKinds.GeoTCategories);
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
            WindowActivationHelper.AttachOwnerFocusPreservation(dialog, dialog.Owner);

            if (dialog.ShowDialog() != true)
                return false;

            result = new LocalizedCategoryEntryViewModel(languageContext)
            {
                Title = HomeLinksLocalization.Clone(dialog.ResultTitle)
            };
            return true;
        }

        private bool MatchesPreviewFilter(object item)
        {
            if (item is not PublishDiffRow row)
                return false;

            return SelectedPreviewFilter == "all" || row.Action == SelectedPreviewFilter;
        }

        private static void FillDiffRows(ObservableCollection<PublishDiffRow> rows, PublishPreview? preview)
        {
            rows.Clear();
            if (preview == null)
                return;

            AddDiffRows(rows, preview.NewItems, "new", "新增");
            AddDiffRows(rows, preview.UpdatedItems, "update", "更新");
            AddDiffRows(rows, preview.RemovedItems, "remove", "移除");
            AddDiffRows(rows, preview.UnchangedItems, "same", "未变化");
        }

        private static void AddDiffRows(
            ObservableCollection<PublishDiffRow> rows,
            List<PublishPreviewItem>? items,
            string action,
            string label)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                string name = string.IsNullOrWhiteSpace(item.Name) ? item.GraphMapPath : item.Name;
                rows.Add(new PublishDiffRow(name, action, label));
            }
        }
    }

    public sealed class PublishDiffRow
    {
        public PublishDiffRow(string name, string action, string actionLabel)
        {
            Name = name;
            Action = action;
            ActionLabel = actionLabel;
        }

        public string Name { get; }

        public string Action { get; }

        public string ActionLabel { get; }
    }
}
