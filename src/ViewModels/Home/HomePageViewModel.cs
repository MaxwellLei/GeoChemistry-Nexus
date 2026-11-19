using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Views;
using GeoChemistryNexus.Views.Widgets;
using GeoChemistryNexus.ViewModels.Home;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GeoChemistryNexus.ViewModels
{
    public partial class HomePageViewModel : ObservableObject
    {
        private readonly ObservableCollection<HomeAppItem> _widgets = new();
        private readonly Dictionary<string, Window> _openedWindows = new();
        private HomeLinkGroupViewModel _personalGroup = null!;

        public ObservableCollection<HomeLinkGroupViewModel> OfficialLinkGroups { get; } = new();

        public ObservableCollection<HomeAppItem> Widgets => _widgets;

        public HomeLinkGroupViewModel PersonalLinkGroup => _personalGroup;

        /// <summary>
        /// 左侧导航使用的全部分组：官方分组在前，个人分组在最后。
        /// </summary>
        public ObservableCollection<HomeLinkGroupViewModel> NavGroups { get; } = new();

        /// <summary>
        /// 右侧网格实际展示的链接：未搜索时为当前分组，搜索时为所有分组中的匹配项。
        /// </summary>
        public ObservableCollection<HomeAppItem> DisplayedLinks { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelectedGroupPersonal))]
        [NotifyPropertyChangedFor(nameof(ShowAddLinkTile))]
        private HomeLinkGroupViewModel? selectedLinkGroup;

        [ObservableProperty]
        private HomeLinkGroupViewModel? selectedOfficialGroup;

        [ObservableProperty]
        private string linkSearchText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowAddLinkTile))]
        private bool isSearchingLinks;

        [ObservableProperty]
        private bool hasNoDisplayedLinks;

        [ObservableProperty]
        private string emptyLinksMessage = string.Empty;

        /// <summary>
        /// 仅在“我的”分组且不在搜索状态时，网格末尾显示“添加链接”卡片。
        /// </summary>
        public bool ShowAddLinkTile => IsSelectedGroupPersonal && !IsSearchingLinks;

        partial void OnLinkSearchTextChanged(string value)
        {
            RefreshDisplayedLinks();
        }

        partial void OnSelectedOfficialGroupChanged(HomeLinkGroupViewModel? value)
        {
            if (value != null)
                SelectedLinkGroup = value;
        }

        partial void OnSelectedLinkGroupChanged(HomeLinkGroupViewModel? value)
        {
            if (value?.IsPersonal == true)
                SelectedOfficialGroup = null;
            else if (value != null && !value.IsPersonal)
                SelectedOfficialGroup = value;

            if (value?.IsPersonal != true && IsEditMode)
                IsEditMode = false;

            RefreshDisplayedLinks();
        }

        private void RefreshDisplayedLinks()
        {
            string query = (LinkSearchText ?? string.Empty).Trim();
            bool searching = query.Length > 0;
            IsSearchingLinks = searching;

            IEnumerable<HomeAppItem> source = searching
                ? NavGroups.SelectMany(g => g.Items).Where(item =>
                    (item.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                : SelectedLinkGroup?.Items ?? Enumerable.Empty<HomeAppItem>();

            DisplayedLinks.Clear();
            foreach (var item in source)
                DisplayedLinks.Add(item);

            // 仅搜索无结果时提示；空分组不再显示提示（“我的”分组末尾始终有“添加链接”入口）
            HasNoDisplayedLinks = searching && DisplayedLinks.Count == 0;
            EmptyLinksMessage = searching
                ? LanguageService.Instance["home_search_no_results"]
                : string.Empty;
        }

        // ===== 问候区 =====

        [ObservableProperty]
        private string greetingPrefix = string.Empty;

        [ObservableProperty]
        private string greetingHighlight = string.Empty;

        [ObservableProperty]
        private string greetingTail = string.Empty;

        [ObservableProperty]
        private string todayText = string.Empty;

        [ObservableProperty]
        private string weekdayText = string.Empty;

        private void RefreshGreeting()
        {
            var now = DateTime.Now;
            string key = now.Hour switch
            {
                < 6 => "home_greeting_night",
                < 12 => "home_greeting_morning",
                < 18 => "home_greeting_afternoon",
                _ => "home_greeting_evening"
            };

            GreetingPrefix = LanguageService.Instance[key];
            GreetingHighlight = LanguageService.Instance["home_hello_highlight"];
            GreetingTail = LanguageService.Instance["home_hello_tail"];
            TodayText = now.ToString("yyyy.MM.dd");
            WeekdayText = now.ToString("dddd");
        }

        /// <summary>
        /// 轮播计数，如 “02 / 10”。
        /// </summary>
        [ObservableProperty]
        private string announcementCounterText = string.Empty;

        private void UpdateAnnouncementCounter()
        {
            AnnouncementCounterText = Announcements.Count == 0
                ? string.Empty
                : $"{_currentAnnouncementIndex + 1:00} / {Announcements.Count:00}";
        }

        [ObservableProperty]
        private bool isEditMode;

        public bool IsSelectedGroupPersonal => SelectedLinkGroup?.IsPersonal == true;

        private List<HomeAnnouncementEntry> _announcementEntries = new();

        public ObservableCollection<HomeAnnouncementDisplayViewModel> Announcements { get; } = new();

        [ObservableProperty]
        private HomeAnnouncementDisplayViewModel? currentAnnouncement;

        /// <summary>
        /// 正在滑出的公告。动画结束后清空。
        /// </summary>
        [ObservableProperty]
        private HomeAnnouncementDisplayViewModel? outgoingAnnouncement;

        /// <summary>
        /// 1 表示下一页（向左滚），-1 表示上一页（向右滚），0 表示不播放动画。
        /// </summary>
        [ObservableProperty]
        private int announcementSlideDirection;

        /// <summary>
        /// 每次切换加一，用来触发滚动动画。
        /// </summary>
        [ObservableProperty]
        private int announcementSlideId;

        private int _currentAnnouncementIndex;

        [ObservableProperty]
        private bool hasAnnouncement;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NextAnnouncementCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousAnnouncementCommand))]
        private bool hasMultipleAnnouncements;

        /// <summary>
        /// 多条公告时自动轮播的间隔。手动切换后重新计时。
        /// </summary>
        private static readonly TimeSpan AnnouncementCarouselInterval = TimeSpan.FromSeconds(6);

        private DispatcherTimer? _announcementCarouselTimer;

        private int _announcementImageLoadVersion;

        private CancellationTokenSource? _announcementImageLoadCts;

        private bool _announcementCarouselPaused;

        [ObservableProperty]
        private bool isAnnouncementBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCatalogIdle))]
        private bool isCatalogBusy;

        public bool IsAnnouncementIdle => !IsAnnouncementBusy;

        public bool IsCatalogIdle => !IsCatalogBusy;

        public HomePageViewModel()
        {
            LanguageService.Instance.PropertyChanged += OnAppLanguageChanged;
            _widgets.CollectionChanged += (_, _) => OnWidgetsCollectionChanged();
            // 主题色切换后重建公告展示项，让跟随主题色的公告背景立即换色
            WeakReferenceMessenger.Default.Register<HomePageViewModel, AppThemeChangedMessage>(
                this, static (recipient, _) => recipient.RebuildAnnouncementDisplays());
            RefreshGreeting();
            RebuildGroups();
            ShowLocalAnnouncements();
        }

        private void OnAppLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                RefreshGreeting();
                RebuildGroups();
                RebuildAnnouncementDisplays();
            }
        }

        [RelayCommand]
        private async Task Loaded()
        {
            await RefreshHomeDataAsync(showUpdateMessage: false);
        }

        [RelayCommand]
        private async Task RefreshCatalog()
        {
            await RefreshHomeDataAsync(showUpdateMessage: true);
        }

        private async Task RefreshHomeDataAsync(bool showUpdateMessage)
        {
            if (IsCatalogBusy)
                return;

            IsCatalogBusy = true;
            try
            {
                var serverInfo = await TryGetServerInfoAsync();
                var linksTask = HomeLinksCatalogService.SyncFromServerAsync(serverInfo);
                var announcementsTask = SyncAnnouncementsFromServerAsync(serverInfo);
                await Task.WhenAll(linksTask, announcementsTask);

                bool updated = await linksTask;
                RebuildGroups();

                if (showUpdateMessage)
                {
                    if (updated)
                        MessageHelper.Success(LanguageService.Instance["home_catalog_updated"]);
                    else
                        MessageHelper.Info(LanguageService.Instance["home_catalog_already_latest"]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomePageViewModel] Refresh failed: {ex.Message}");
                if (showUpdateMessage)
                    MessageHelper.Warning(LanguageService.Instance["home_catalog_sync_failed"]);
            }
            finally
            {
                IsCatalogBusy = false;
            }
        }

        /// <summary>
        /// 立刻用本地（或安装包内置）公告填充卡片，不等待网络。
        /// </summary>
        private void ShowLocalAnnouncements()
        {
            _announcementEntries = HomeAnnouncementService.LoadLocalAnnouncements();
            RebuildAnnouncementDisplays();
        }

        /// <summary>
        /// 后台按 server_info 的 hash 更新公告。失败或无变化时保留当前已显示的内容。
        /// </summary>
        private async Task SyncAnnouncementsFromServerAsync(ServerInfo? serverInfo)
        {
            if (IsAnnouncementBusy)
                return;

            IsAnnouncementBusy = true;
            OnPropertyChanged(nameof(IsAnnouncementIdle));
            try
            {
                bool changed = await HomeAnnouncementService.SyncFromServerAsync(serverInfo);
                if (!changed && _announcementEntries.Count > 0)
                    return;

                var entries = HomeAnnouncementService.LoadLocalAnnouncements();
                if (entries.Count == 0)
                    entries = HomeAnnouncementService.CreateLegacyEntries(serverInfo);

                if (!changed && entries.Count == 0)
                    return;

                _announcementEntries = entries;
                RebuildAnnouncementDisplays();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomePageViewModel] Announcement sync failed: {ex.Message}");
            }
            finally
            {
                IsAnnouncementBusy = false;
                OnPropertyChanged(nameof(IsAnnouncementIdle));
            }
        }

        private static async Task<ServerInfo?> TryGetServerInfoAsync()
        {
            try
            {
                return await UpdateHelper.GetServerInfoAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomePageViewModel] server_info fetch failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 按当前界面语言把公告原始数据解析为展示项；语言切换时重建。
        /// 未指定展示语言的公告对所有语言显示。
        /// </summary>
        private void RebuildAnnouncementDisplays()
        {
            string? previousId = CurrentAnnouncement?.Id;
            var shownImages = new Dictionary<string, ImageSource>(StringComparer.Ordinal);
            foreach (var item in Announcements)
            {
                if (item.BackgroundImage != null && !string.IsNullOrWhiteSpace(item.BackgroundImageUrl))
                    shownImages.TryAdd(item.BackgroundImageUrl, item.BackgroundImage);
            }

            Announcements.Clear();
            string appLanguage = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            foreach (var entry in _announcementEntries)
            {
                if (!entry.IsVisibleForLanguage(appLanguage))
                    continue;

                var display = HomeAnnouncementDisplayViewModel.FromEntry(entry, HomeLinksLocalization.ResolveForApp);
                if (display.UseImageBackground
                    && shownImages.TryGetValue(display.BackgroundImageUrl, out ImageSource? shown))
                    display.BackgroundImage = shown;

                Announcements.Add(display);
            }

            _ = LoadAnnouncementImagesAsync();

            HasAnnouncement = Announcements.Count > 0;
            HasMultipleAnnouncements = Announcements.Count > 1;

            int index = 0;
            if (!string.IsNullOrEmpty(previousId))
            {
                int found = Announcements.ToList().FindIndex(a => a.Id == previousId);
                if (found >= 0)
                    index = found;
            }

            ShowAnnouncementAt(index);
        }

        private async Task LoadAnnouncementImagesAsync()
        {
            int loadVersion = Interlocked.Increment(ref _announcementImageLoadVersion);
            _announcementImageLoadCts?.Cancel();
            var loadCts = new CancellationTokenSource();
            _announcementImageLoadCts = loadCts;

            var pending = Announcements.Where(item => item.UseImageBackground).ToList();
            HomeAnnouncementImageCache.PruneExcept(pending.Select(item => item.BackgroundImageUrl), loadVersion);
            if (pending.Count == 0)
                return;

            try
            {
                await Task.WhenAll(pending.Select(item => item.LoadBackgroundImageAsync(loadVersion, loadCts.Token)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomePage] announcement images failed: {ex.Message}");
            }
        }

        private void ShowAnnouncementAt(int index)
        {
            if (Announcements.Count == 0)
            {
                _currentAnnouncementIndex = 0;
                CurrentAnnouncement = null;
                BeginAnnouncementSlide(null, null, 0);
                UpdateAnnouncementCounter();
                SyncAnnouncementCarousel();
                return;
            }

            int fromIndex = _currentAnnouncementIndex;
            int nextIndex = ((index % Announcements.Count) + Announcements.Count) % Announcements.Count;
            var next = Announcements[nextIndex];
            var previous = CurrentAnnouncement;

            foreach (var item in Announcements)
                item.IsCurrent = false;

            next.IsCurrent = true;
            _currentAnnouncementIndex = nextIndex;

            bool animate = previous != null
                && !ReferenceEquals(previous, next)
                && Announcements.Contains(previous);
            int direction = 0;
            if (animate)
                direction = index < fromIndex ? -1 : 1;
            BeginAnnouncementSlide(next, animate ? previous : null, direction);
            UpdateAnnouncementCounter();
            SyncAnnouncementCarousel();
        }

        private void BeginAnnouncementSlide(
            HomeAnnouncementDisplayViewModel? incoming,
            HomeAnnouncementDisplayViewModel? outgoing,
            int direction)
        {
            OutgoingAnnouncement = outgoing;
            AnnouncementSlideDirection = direction;
            CurrentAnnouncement = incoming;
            AnnouncementSlideId++;
        }

        [RelayCommand]
        private void CompleteAnnouncementSlide(int slideId)
        {
            if (slideId != AnnouncementSlideId)
                return;

            OutgoingAnnouncement = null;
        }

        /// <summary>
        /// 多于一条、且指针不在卡片上时按固定间隔循环切换。
        /// 只有一条、没有公告，或正在查看卡片时停掉计时。
        /// </summary>
        private void SyncAnnouncementCarousel()
        {
            if (!HasMultipleAnnouncements || _announcementCarouselPaused)
            {
                _announcementCarouselTimer?.Stop();
                return;
            }

            if (_announcementCarouselTimer == null)
            {
                _announcementCarouselTimer = new DispatcherTimer
                {
                    Interval = AnnouncementCarouselInterval
                };
                _announcementCarouselTimer.Tick += OnAnnouncementCarouselTick;
            }

            _announcementCarouselTimer.Stop();
            _announcementCarouselTimer.Start();
        }

        private void OnAnnouncementCarouselTick(object? sender, EventArgs e)
        {
            ShowAnnouncementAt(_currentAnnouncementIndex + 1);
        }

        [RelayCommand]
        private void PauseAnnouncementCarousel()
        {
            _announcementCarouselPaused = true;
            _announcementCarouselTimer?.Stop();
        }

        [RelayCommand]
        private void ResumeAnnouncementCarousel()
        {
            _announcementCarouselPaused = false;
            SyncAnnouncementCarousel();
        }

        private bool CanSwitchAnnouncement() => HasMultipleAnnouncements;

        [RelayCommand(CanExecute = nameof(CanSwitchAnnouncement))]
        private void NextAnnouncement()
        {
            ShowAnnouncementAt(_currentAnnouncementIndex + 1);
        }

        [RelayCommand(CanExecute = nameof(CanSwitchAnnouncement))]
        private void PreviousAnnouncement()
        {
            ShowAnnouncementAt(_currentAnnouncementIndex - 1);
        }

        [RelayCommand]
        private void SelectAnnouncement(HomeAnnouncementDisplayViewModel item)
        {
            if (item == null)
                return;

            int index = Announcements.IndexOf(item);
            if (index >= 0)
                ShowAnnouncementAt(index);
        }

        [RelayCommand]
        private void OpenAnnouncementAction(HomeAnnouncementDisplayViewModel item)
        {
            string? url = item?.ActionUrl;
            if (string.IsNullOrWhiteSpace(url))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageHelper.Warning("OpenBrowserError: " + ex.Message);
            }
        }

        private void RebuildGroups()
        {
            string? previousGroupId = SelectedLinkGroup?.GroupId;

            var catalog = HomeLinksCatalogService.LoadLocalCatalog();
            var userConfig = HomeUserConfigService.Load();

            OfficialLinkGroups.Clear();
            _widgets.Clear();

            foreach (var group in catalog.Groups ?? Enumerable.Empty<HomeLinkGroup>())
            {
                if (group.Links == null || group.Links.Count == 0)
                    continue;

                var groupVm = new HomeLinkGroupViewModel
                {
                    GroupId = group.Id,
                    Title = HomeLinksLocalization.ResolveForApp(group.Title),
                    IsPersonal = false,
                    IsVisible = true
                };

                foreach (var link in group.Links)
                    groupVm.Items.Add(ToOfficialAppItem(link));

                OfficialLinkGroups.Add(groupVm);
            }

            _personalGroup = new HomeLinkGroupViewModel
            {
                GroupId = "personal",
                Title = LanguageService.Instance["home_personal_group"],
                IsPersonal = true,
                IsVisible = true
            };

            foreach (var link in userConfig.PersonalLinks ?? Enumerable.Empty<HomeAppItem>())
            {
                link.IsOfficial = false;
                link.Type = HomeAppType.WebLink;
                _personalGroup.Items.Add(link);
            }

            // 个人链接增删时，右侧网格同步刷新
            _personalGroup.Items.CollectionChanged += (_, _) => RefreshDisplayedLinks();

            OnPropertyChanged(nameof(PersonalLinkGroup));

            NavGroups.Clear();
            foreach (var officialGroup in OfficialLinkGroups)
                NavGroups.Add(officialGroup);
            NavGroups.Add(_personalGroup);

            var availableWidgets = HomeAppService.GetAvailableWidgets()
                .Where(w => !string.IsNullOrEmpty(w.WidgetKey))
                .ToDictionary(w => w.WidgetKey, w => w, StringComparer.OrdinalIgnoreCase);

            foreach (var widget in userConfig.Widgets ?? Enumerable.Empty<HomeAppItem>())
            {
                widget.IsOfficial = false;
                widget.Type = HomeAppType.Widget;
                if (availableWidgets.TryGetValue(widget.WidgetKey ?? string.Empty, out var template))
                {
                    widget.Title = template.Title;
                    widget.Description = template.Description;
                    widget.Icon = template.Icon;
                }
                _widgets.Add(widget);
            }

            RestoreSelectedLinkGroup(previousGroupId);
        }

        private void RestoreSelectedLinkGroup(string? previousGroupId)
        {
            if (string.Equals(previousGroupId, "personal", StringComparison.OrdinalIgnoreCase))
            {
                SelectedLinkGroup = _personalGroup;
                return;
            }

            var official = OfficialLinkGroups.FirstOrDefault(g => g.GroupId == previousGroupId);
            if (official != null)
            {
                SelectedLinkGroup = official;
                return;
            }

            if (OfficialLinkGroups.Count > 0)
                SelectedLinkGroup = OfficialLinkGroups[0];
            else
                SelectedLinkGroup = _personalGroup;
        }

        [RelayCommand]
        private void SelectPersonalGroup()
        {
            SelectedLinkGroup = _personalGroup;
        }

        private static HomeAppItem ToOfficialAppItem(HomeLinkEntry entry)
        {
            return new HomeAppItem
            {
                Id = string.IsNullOrWhiteSpace(entry.Id) ? Guid.NewGuid().ToString() : entry.Id,
                Type = HomeAppType.WebLink,
                Title = HomeLinksLocalization.ResolveForApp(entry.Title),
                Description = HomeLinksLocalization.ResolveForApp(entry.Description),
                Url = entry.Url ?? string.Empty,
                Icon = HomeIconHelper.ResolveIcon(entry.Icon),
                IsOfficial = true
            };
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
        }

        [RelayCommand]
        private void EditApp(HomeAppItem app)
        {
            if (app == null || app.IsReadOnly)
                return;

            if (app.Type != HomeAppType.WebLink)
                return;

            var dialog = new AddLinkWindow();
            dialog.Owner = Application.Current.MainWindow;
            WindowActivationHelper.AttachOwnerFocusPreservation(dialog, dialog.Owner);
            dialog.TitleBox.Text = app.Title;
            dialog.UrlBox.Text = app.Url;
            dialog.DescBox.Text = app.Description;
            dialog.LoadIcon(app.Icon);
            dialog.Title = LanguageService.Instance["edit_link"];

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                app.Title = dialog.Result.Title;
                app.Url = dialog.Result.Url;
                app.Description = dialog.Result.Description;
                app.Icon = dialog.Result.Icon;
                SavePersonalLinks();
            }
        }

        [RelayCommand]
        private void OpenApp(HomeAppItem app)
        {
            if (app == null || IsEditMode)
                return;

            if (app.Type == HomeAppType.WebLink && !string.IsNullOrWhiteSpace(app.Url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {app.Url}") { CreateNoWindow = true });
                }
                catch (Exception ex)
                {
                    MessageHelper.Warning("OpenBrowserError: " + ex.Message);
                }
            }
            else if (app.Type == HomeAppType.Widget)
            {
                OpenWidget(app);
            }
        }

        private void OpenWidget(HomeAppItem app)
        {
            try
            {
                if (!string.IsNullOrEmpty(app.WidgetKey) && _openedWindows.TryGetValue(app.WidgetKey, out var existingWindow))
                {
                    if (existingWindow.IsLoaded)
                    {
                        if (existingWindow.WindowState == WindowState.Minimized)
                            existingWindow.WindowState = WindowState.Normal;
                        existingWindow.Activate();
                        return;
                    }

                    _openedWindows.Remove(app.WidgetKey);
                }

                Window? window = null;

                if (app.WidgetKey == "OfficialTemplatePublisherWidget")
                {
                    window = new OfficialTemplatePublisherWindow();
                }
                else if (app.WidgetKey == "AlkalinityCalculatorWidget")
                {
                    window = new WidgetWindow
                    {
                        WidgetIcon = app.Icon,
                        Title = LanguageService.Instance["alkalinity_calculator"],
                        Width = 680,
                        Height = 560,
                        MinWidth = 560,
                        MinHeight = 420,
                        Content = new AlkalinityCalculatorWidget { DataContext = new AlkalinityCalculatorViewModel() }
                    };
                }
                else if (app.WidgetKey == "HardnessCalculatorWidget")
                {
                    window = new WidgetWindow
                    {
                        WidgetIcon = app.Icon,
                        Title = LanguageService.Instance["hardness_calculator"],
                        Width = 560,
                        Height = 580,
                        MinWidth = 480,
                        MinHeight = 440,
                        Content = new HardnessCalculatorWidget { DataContext = new HardnessCalculatorViewModel() }
                    };
                }
                else if (app.WidgetKey == "BlackBodyRadiationCalculatorWidget")
                {
                    window = new WidgetWindow
                    {
                        WidgetIcon = app.Icon,
                        Title = LanguageService.Instance["black_body_radiation_calculator"],
                        Width = 920,
                        Height = 560,
                        MinWidth = 760,
                        MinHeight = 460,
                        Content = new BlackBodyRadiationCalculatorWidget
                        {
                            DataContext = new BlackBodyRadiationCalculatorViewModel()
                        }
                    };
                }
                else if (app.WidgetKey == "PeriodicTableWidget")
                {
                    var main = Application.Current.MainWindow;
                    double maxWidth = main != null && main.ActualWidth > 0 ? main.ActualWidth : SystemParameters.WorkArea.Width;
                    double maxHeight = main != null && main.ActualHeight > 0 ? main.ActualHeight : SystemParameters.WorkArea.Height;

                    // 独立窗体高度不超过主窗体
                    double height = Math.Min(620, maxHeight);
                    double width = Math.Min(1180, maxWidth);
                    double minHeight = Math.Min(480, height);
                    double minWidth = Math.Min(960, width);

                    window = new WidgetWindow
                    {
                        WidgetIcon = app.Icon,
                        Title = LanguageService.Instance["periodic_table_widget"],
                        Width = width,
                        Height = height,
                        MinWidth = minWidth,
                        MinHeight = minHeight,
                        Content = new PeriodicTableWidget
                        {
                            DataContext = new PeriodicTableWidgetViewModel()
                        }
                    };
                }
                else if (app.WidgetKey == "ChronostratNavigatorWidget")
                {
                    var main = Application.Current.MainWindow;
                    double maxWidth  = main != null && main.ActualWidth  > 0 ? main.ActualWidth  : SystemParameters.WorkArea.Width;
                    double maxHeight = main != null && main.ActualHeight > 0 ? main.ActualHeight : SystemParameters.WorkArea.Height;

                    double width     = Math.Min(1180, maxWidth);
                    double height    = Math.Min(740,  maxHeight);
                    double minWidth  = Math.Min(960,  width);
                    double minHeight = Math.Min(600,  height);

                    window = new WidgetWindow
                    {
                        WidgetIcon = app.Icon,
                        Title    = LanguageService.Instance["chronostrat_navigator_widget"],
                        Width    = width,
                        Height   = height,
                        MinWidth = minWidth,
                        MinHeight = minHeight,
                        Content  = new ChronostratNavigatorWidget
                        {
                            DataContext = new ChronostratNavigatorWidgetViewModel()
                        }
                    };
                }
                else if (app.WidgetKey == "OxideElementConverterWidget")
                {
                    window = new WidgetWindow
                    {
                        WidgetIcon = app.Icon,
                        Title    = LanguageService.Instance["oxide_element_converter"],
                        Width    = 720,
                        Height   = 640,
                        MinWidth = 560,
                        MinHeight = 480,
                        Content  = new OxideElementConverterWidget
                        {
                            DataContext = new OxideElementConverterViewModel()
                        }
                    };
                }
                else if (app.WidgetKey == "GeoscienceUnitConverterWidget")
                {
                    window = new WidgetWindow
                    {
                        WidgetIcon = app.Icon,
                        Title    = LanguageService.Instance["geoscience_unit_converter"],
                        Width    = 720,
                        Height   = 680,
                        MinWidth = 720,
                        MinHeight = 500,
                        Content  = new GeoscienceUnitConverterWidget
                        {
                            DataContext = new GeoscienceUnitConverterViewModel()
                        }
                    };
                }

                if (window == null)
                    return;

                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null && mainWindow.IsVisible)
                {
                    window.Owner = mainWindow;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // 子窗体必须小于主窗体（宽高、最小尺寸、最大高度统一收敛）
                ChildWindowSizeHelper.FitInside(window, mainWindow);

                WindowActivationHelper.AttachOwnerFocusPreservation(window, mainWindow);

                window.Show();

                if (!string.IsNullOrEmpty(app.WidgetKey))
                {
                    _openedWindows[app.WidgetKey] = window;
                    window.Closed += (_, _) =>
                    {
                        _openedWindows.Remove(app.WidgetKey);
                        if (window.Content is System.Windows.FrameworkElement content
                            && content.DataContext is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["failed_to_open_widget"] + ex.Message + "\n" + ex.StackTrace);
            }
        }

        [RelayCommand]
        private void RemoveApp(HomeAppItem app)
        {
            if (app == null || app.IsReadOnly)
                return;

            if (app.Type == HomeAppType.Widget && _widgets.Contains(app))
            {
                _widgets.Remove(app);
                SaveWidgets();
                return;
            }

            if (_personalGroup?.Items.Contains(app) == true)
            {
                _personalGroup.Items.Remove(app);
                SavePersonalLinks();
            }
        }

        [RelayCommand]
        private void AddWebLink()
        {
            EnsurePersonalGroup();
            SelectedLinkGroup = _personalGroup;

            var dialog = new AddLinkWindow();
            dialog.Owner = Application.Current.MainWindow;
            WindowActivationHelper.AttachOwnerFocusPreservation(dialog, dialog.Owner);
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _personalGroup.Items.Add(dialog.Result);
                SavePersonalLinks();
            }
        }

        // ===== 添加小组件：锚定在“+”按钮下方的气泡面板 =====

        private readonly List<AddWidgetCardItem> _widgetPickerAll = new();

        /// <summary>
        /// 气泡面板当前展示（已按搜索词过滤）的小组件条目。
        /// </summary>
        public ObservableCollection<AddWidgetCardItem> WidgetPickerItems { get; } = new();

        public bool HasWidgets => _widgets.Count > 0;

        public bool HasWidgetPickerMatches => WidgetPickerItems.Count > 0;

        [ObservableProperty]
        private bool isWidgetPickerOpen;

        [ObservableProperty]
        private string widgetPickerSearchText = string.Empty;

        [ObservableProperty]
        private string widgetPickerCountText = string.Empty;

        partial void OnIsWidgetPickerOpenChanged(bool value)
        {
            if (!value)
                return;

            // 每次打开都重新读取可用小组件（语言 / 开发者模式可能变化），并清空上次的搜索词
            WidgetPickerSearchText = string.Empty;
            BuildWidgetPicker();
        }

        partial void OnWidgetPickerSearchTextChanged(string value)
        {
            ApplyWidgetPickerFilter();
        }

        [RelayCommand]
        private void AddWidget()
        {
            IsWidgetPickerOpen = !IsWidgetPickerOpen;
        }

        private void BuildWidgetPicker()
        {
            _widgetPickerAll.Clear();

            var addedKeys = GetAddedWidgetKeys();
            foreach (var widget in HomeAppService.GetAvailableWidgets().Where(w => !string.IsNullOrEmpty(w.WidgetKey)))
            {
                _widgetPickerAll.Add(new AddWidgetCardItem(
                    widget,
                    addedKeys.Contains(widget.WidgetKey),
                    card => AddWidgetFromTemplate(card.Item),
                    card => RemoveWidgetByKey(card.WidgetKey)));
            }

            ApplyWidgetPickerFilter();
            UpdateWidgetPickerSummary();
        }

        private void ApplyWidgetPickerFilter()
        {
            string query = (WidgetPickerSearchText ?? string.Empty).Trim();

            WidgetPickerItems.Clear();
            foreach (var card in _widgetPickerAll)
            {
                if (query.Length == 0
                    || (card.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (card.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    WidgetPickerItems.Add(card);
                }
            }

            OnPropertyChanged(nameof(HasWidgetPickerMatches));
        }

        private void UpdateWidgetPickerSummary()
        {
            int added = _widgetPickerAll.Count(c => c.IsAdded);
            WidgetPickerCountText = $"{LanguageService.Instance["added"]} {added} / {_widgetPickerAll.Count}";
        }

        private HashSet<string> GetAddedWidgetKeys()
        {
            return new HashSet<string>(
                _widgets.Select(w => w.WidgetKey).Where(k => !string.IsNullOrEmpty(k)),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 小组件列表发生变化（包括在列表里右键移除）时，同步气泡面板里的“已添加”状态。
        /// </summary>
        private void OnWidgetsCollectionChanged()
        {
            OnPropertyChanged(nameof(HasWidgets));

            if (_widgetPickerAll.Count == 0)
                return;

            var addedKeys = GetAddedWidgetKeys();
            foreach (var card in _widgetPickerAll)
                card.IsAdded = addedKeys.Contains(card.WidgetKey);

            UpdateWidgetPickerSummary();
        }

        private void AddWidgetFromTemplate(HomeAppItem template)
        {
            if (_widgets.Any(w => string.Equals(w.WidgetKey, template.WidgetKey, StringComparison.OrdinalIgnoreCase)))
                return;

            _widgets.Add(new HomeAppItem
            {
                Type = HomeAppType.Widget,
                Title = template.Title,
                Description = template.Description,
                WidgetKey = template.WidgetKey,
                Icon = template.Icon
            });
            SaveWidgets();
        }

        private void RemoveWidgetByKey(string widgetKey)
        {
            var existing = _widgets.FirstOrDefault(w =>
                string.Equals(w.WidgetKey, widgetKey, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
                return;

            _widgets.Remove(existing);
            SaveWidgets();
        }

        private void EnsurePersonalGroup()
        {
            if (_personalGroup != null)
                return;

            _personalGroup = new HomeLinkGroupViewModel
            {
                GroupId = "personal",
                Title = LanguageService.Instance["home_personal_group"],
                IsPersonal = true,
                IsVisible = true
            };
            OnPropertyChanged(nameof(PersonalLinkGroup));
        }

        private void SavePersonalLinks()
        {
            EnsurePersonalGroup();
            HomeUserConfigService.SavePersonalLinks(_personalGroup.Items);
        }

        private void SaveWidgets()
        {
            HomeUserConfigService.SaveWidgets(_widgets);
        }
    }
}
