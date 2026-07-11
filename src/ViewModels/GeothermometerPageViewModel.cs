using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using GeoChemistryNexus.Views;
using Ookii.Dialogs.Wpf;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.ViewModels
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "WPF binding requires instance members.")]
    public partial class GeothermometerPageViewModel : ObservableObject,
        IRecipient<DeveloperModeChangedMessage>,
        IRecipient<GeoTMineralCategoryUpdatedMessage>
    {
        /// <summary>
        /// 应用温压计后工作表的初始行数（含表头与示例行）
        /// </summary>
        private const int InitialWorksheetRowCount = 500;

        /// <summary>
        /// 搜索防抖间隔，避免每个按键都重建侧栏绑定
        /// </summary>
        private static readonly TimeSpan SearchDebounceInterval = TimeSpan.FromMilliseconds(200);

        private Worksheet? _rowExpansionWorksheet;
        private DispatcherTimer? _searchDebounceTimer;
        private int _searchFilterVersion;
        private bool _isUpdatingTagFilters;
        /// <summary>
        /// 侧栏一级区块（收藏 / 官方 / 自定义），各含二级类别分组
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<GeoTSidebarSectionViewModel> sidebarSections = new();

        /// <summary>
        /// 当前选中的 GTM
        /// </summary>
        [ObservableProperty]
        private Geothermometer? selectedPlugin;

        /// <summary>
        /// 是否显示帮助文档区域（选中温压计时显示，点击确认后隐藏）
        /// </summary>
        [ObservableProperty]
        private bool isHelpDocVisible;

        /// <summary>
        /// 帮助文档是否正在异步加载（控制帮助区遮罩）
        /// </summary>
        [ObservableProperty]
        private bool isHelpDocLoading;

        /// <summary>
        /// 当前选中的温压计是否已应用（控制顶部按钮显示逻辑）
        /// </summary>
        [ObservableProperty]
        private bool isPluginApplied;

        /// <summary>
        /// 是否正在检查更新
        /// </summary>
        [ObservableProperty]
        private bool isCheckingUpdates;

        /// <summary>
        /// 更新遮罩层是否可见
        /// </summary>
        [ObservableProperty]
        private bool isUpdateOverlayVisible;

        /// <summary>
        /// 更新进度（0-100）
        /// </summary>
        [ObservableProperty]
        private double updateProgress;

        /// <summary>
        /// 更新进度条是否为不确定模式（检查更新阶段）
        /// </summary>
        [ObservableProperty]
        private bool isUpdateProgressIndeterminate = true;

        /// <summary>
        /// 更新状态文本
        /// </summary>
        [ObservableProperty]
        private string updateStatusText = string.Empty;

        /// <summary>
        /// 可用更新数量
        /// </summary>
        [ObservableProperty]
        private int availableUpdateCount;

        /// <summary>
        /// 搜索文本
        /// </summary>
        [ObservableProperty]
        private string searchText = string.Empty;

        /// <summary>
        /// 标签筛选菜单是否打开
        /// </summary>
        [ObservableProperty]
        private bool isTagFilterMenuOpen;

        /// <summary>
        /// 可选标签筛选项（有搜索时来自搜索结果，否则来自全部模板）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<GeoTTagFilterItemViewModel> availableTagFilters = new();

        /// <summary>
        /// 是否已启用标签筛选
        /// </summary>
        public bool HasActiveTagFilter => AvailableTagFilters.Any(t => t.IsSelected);

        /// <summary>
        /// 是否存在可用标签筛选项
        /// </summary>
        public bool HasAvailableTagFilters => AvailableTagFilters.Count > 0;

        /// <summary>
        /// 已加载的 GTM 总数
        /// </summary>
        [ObservableProperty]
        private int totalPluginCount;

        /// <summary>
        /// 官方温压计数量
        /// </summary>
        [ObservableProperty]
        private int officialPluginCount;

        /// <summary>
        /// 自定义温压计数量
        /// </summary>
        [ObservableProperty]
        private int customPluginCount;

        /// <summary>
        /// 收藏温压计数量
        /// </summary>
        [ObservableProperty]
        private int favoritePluginCount;

        /// <summary>
        /// 是否为开发者模式
        /// </summary>
        [ObservableProperty]
        private bool isDeveloperMode;

        /// <summary>
        /// 中间计算步骤列表（用于详细计算面板展示）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CalculationStep> calculationSteps = new();

        /// <summary>
        /// 计算详情面板是否最小化（仅显示标题栏）
        /// </summary>
        [ObservableProperty]
        private bool isDetailPanelMinimized = true;

        /// <summary>
        /// 是否有可显示的计算数据
        /// </summary>
        [ObservableProperty]
        private bool hasCalculationData;

        /// <summary>
        /// 上一次用于详细计算的行输入值（语言切换时用于刷新步骤注释）
        /// </summary>
        private double[]? _lastCalculationInputValues;

        /// <summary>
        /// 选中行的描述信息
        /// </summary>
        [ObservableProperty]
        private string selectedRowInfo = string.Empty;

        /// <summary>
        /// 计算详情滚动复位令牌（递增时 View 重置横向/纵向滚动位置）
        /// </summary>
        [ObservableProperty]
        private int detailScrollResetToken;

        /// <summary>
        /// ReoGrid 滚动复位令牌（应用新模板时递增，View 重置表格横向/纵向滚动位置）
        /// </summary>
        [ObservableProperty]
        private int gridScrollResetToken;

        /// <summary>
        /// 当前选中温压计的工作实体（点选时仅摘要；脚本按需补齐）
        /// </summary>
        private GeothermometerEntity? _selectedFullEntity;

        /// <summary>
        /// 上一次已应用的温压计（用于点击确定时恢复）
        /// </summary>
        private Geothermometer? _appliedPlugin;

        /// <summary>
        /// 上一次已应用的温压计对应的工作实体
        /// </summary>
        private GeothermometerEntity? _appliedFullEntity;

        /// <summary>
        /// 帮助文档异步加载取消源（连点时取消上一次）
        /// </summary>
        private CancellationTokenSource? _helpDocLoadCts;

        /// <summary>
        /// 当前语言帮助文档 RTF 缓存（key = "{pluginId}|{lang}"）
        /// </summary>
        private readonly Dictionary<string, string> _helpDocRtfCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 帮助文档缓存上限，避免长时间切换后占用过多内存
        /// </summary>
        private const int MaxHelpDocCacheEntries = 24;

        /// <summary>
        /// 当前选中 GTM 的显示名称
        /// </summary>
        public string SelectedPluginDisplayName
        {
            get
            {
                if (SelectedPlugin == null) return string.Empty;
                if (!string.IsNullOrEmpty(SelectedPlugin.NameLangKey))
                {
                    var localized = LanguageService.Instance[SelectedPlugin.NameLangKey];
                    if (!string.IsNullOrEmpty(localized)) return localized;
                }
                return SelectedPlugin.Name;
            }
        }

        private RichTextBox? _helpRichTextBox;

        /// <summary>
        /// 标记是否已执行过自动检查更新（每次启动只检查一次）
        /// </summary>
        private static bool _hasAutoCheckedUpdate;

        /// <summary>
        /// 标记当前是否为自动检查更新（区分自动/手动，自动检查时已是最新版不弹窗）
        /// </summary>
        private bool _isAutoChecking;

        /// <summary>
        /// 标记是否正在从持久化状态恢复侧栏（避免恢复过程触发保存）
        /// </summary>
        private bool _isRestoringSidebarState;

        public GeothermometerPageViewModel()
        {
            // 注册开发者模式消息
            WeakReferenceMessenger.Default.RegisterAll(this);

            // 读取开发者模式配置
            if (bool.TryParse(ConfigHelper.GetConfig("developer_mode"), out bool devMode))
            {
                IsDeveloperMode = devMode;
            }

            // 初始化 GTM 服务
            GeothermometerService.Initialize();

            // 加载分组数据（内部会按当前搜索刷新筛选项与列表）
            LoadSidebarSections();

            LanguageService.Instance.PropertyChanged += OnAppLanguageChanged;
        }

        private void OnAppLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Item[]")
                return;

            ReloadCategoryGroupsAndSelection();

            // 语言切换后按新语言重新加载帮助文档（缓存按语言分键）
            if (IsHelpDocVisible && SelectedPlugin != null)
                _ = LoadHelpDocumentAsync(SelectedPlugin.Id, forceReload: false);

            if (_lastCalculationInputValues == null || _selectedFullEntity == null || !HasCalculationData)
                return;

            ApplyCalculationSteps(_lastCalculationInputValues);
        }

        private void ReloadCategoryGroupsAndSelection()
        {
            string? selectedPluginId = SelectedPlugin?.Id;
            string? appliedPluginId = _appliedPlugin?.Id;

            // 语言切换后需重建显示名（标签/类别），不能只做就地过滤
            LoadSidebarSections();

            if (!string.IsNullOrEmpty(selectedPluginId))
            {
                SelectedPlugin = FindPluginById(selectedPluginId);
                OnPropertyChanged(nameof(SelectedPluginDisplayName));
            }

            if (!string.IsNullOrEmpty(appliedPluginId))
            {
                _appliedPlugin = FindPluginById(appliedPluginId);
            }
        }

        private Geothermometer? FindPluginById(string pluginId)
        {
            foreach (var section in SidebarSections)
            {
                foreach (var group in section.CategoryGroups)
                {
                    var plugin = group.Plugins.FirstOrDefault(p => p.Id == pluginId);
                    if (plugin != null)
                        return plugin;
                }
            }

            return null;
        }

        /// <summary>
        /// 在温压计页面首次显示时自动检查更新（仅在开启设置且本次启动尚未检查过时执行）
        /// </summary>
        public async Task CheckUpdatesIfNeededAsync()
        {
            if (_hasAutoCheckedUpdate) return;

            if (bool.TryParse(ConfigHelper.GetConfig("auto_check_gtm_update"), out bool autoCheck) && autoCheck)
            {
                _hasAutoCheckedUpdate = true;
                _isAutoChecking = true;
                try
                {
                    await CheckForPluginUpdates();
                }
                finally
                {
                    _isAutoChecking = false;
                }
            }
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            IsDeveloperMode = message.Value;
        }

        public void Receive(GeoTMineralCategoryUpdatedMessage message)
        {
            ReloadCategoryGroupsAndSelection();
        }

        /// <summary>
        /// 设置帮助文档 RichTextBox 引用
        /// </summary>
        public void SetHelpRichTextBox(RichTextBox richTextBox)
        {
            _helpRichTextBox = richTextBox;
        }

        /// <summary>
        /// 加载侧栏三级结构：收藏 / 官方 / 自定义 → 类别 → 温压计列表
        /// </summary>
        private void LoadSidebarSections()
        {
            string? selectedPluginId = SelectedPlugin?.Id;
            string? appliedPluginId = _appliedPlugin?.Id;

            SidebarSections.Clear();

            SidebarSections.Add(BuildSidebarSection(
                GeoTSidebarSectionKeys.Official,
                LanguageService.Instance["geo_section_official"] ?? "官方温压计",
                entity => entity.IsOfficial));

            SidebarSections.Add(BuildSidebarSection(
                GeoTSidebarSectionKeys.Custom,
                LanguageService.Instance["geo_section_custom"] ?? "自定义温压计",
                entity => !entity.IsOfficial));

            SidebarSections.Add(BuildSidebarSection(
                GeoTSidebarSectionKeys.Favorite,
                LanguageService.Instance["favorite_templates"] ?? "收藏",
                entity => entity.IsFavorite));

            ApplyListFilterCore(preserveExpansionState: true);
            ApplySidebarStateFromStorage();

            if (!string.IsNullOrEmpty(selectedPluginId))
                SelectedPlugin = FindPluginById(selectedPluginId);

            if (!string.IsNullOrEmpty(appliedPluginId))
                _appliedPlugin = FindPluginById(appliedPluginId);
        }

        private static GeoTSidebarSectionViewModel BuildSidebarSection(
            string sectionKey,
            string displayName,
            Func<GeothermometerEntity, bool> entityFilter)
        {
            var section = new GeoTSidebarSectionViewModel
            {
                SectionKey = sectionKey,
                DisplayName = displayName
            };

            foreach (string categoryKey in GeoTCategoryHelper.GetCategoryKeys())
            {
                var plugins = GeothermometerService.LoadedEntities
                    .Where(entityFilter)
                    .Where(e => GeoTCategoryHelper.NormalizeCategoryKey(e.Category) == categoryKey)
                    .Select(e => GeothermometerService.CreateGeothermometerFromEntity(e))
                    .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                // 始终保留三个固定类别分组，便于筛选时就地更新而不重建整树
                section.CategoryGroups.Add(new CategoryGroupViewModel
                {
                    CategoryKey = categoryKey,
                    DisplayName = GeoTCategoryHelper.GetDisplayName(categoryKey),
                    SectionKey = sectionKey,
                    AllPlugins = plugins,
                    Plugins = new ObservableCollection<Geothermometer>(plugins)
                });
            }

            return section;
        }

        private void UpdatePluginCounts()
        {
            OfficialPluginCount = GeothermometerService.LoadedEntities.Count(e => e.IsOfficial);
            CustomPluginCount = GeothermometerService.LoadedEntities.Count(e => !e.IsOfficial);
            FavoritePluginCount = GeothermometerService.LoadedEntities.Count(e => e.IsFavorite);
            TotalPluginCount = GeothermometerService.LoadedEntities.Count;
        }

        private void ApplySidebarStateFromStorage()
        {
            var state = GeoTSidebarStateHelper.Load();
            state.ExpandedCategoryKeys ??= new List<string>();
            bool hasFilter = !string.IsNullOrWhiteSpace(SearchText) || HasActiveTagFilter;

            _isRestoringSidebarState = true;
            try
            {
                if (hasFilter)
                {
                    ApplyFilterExpansionState();
                }
                else
                {
                    foreach (var section in SidebarSections)
                    {
                        section.IsExpanded = string.Equals(
                            section.SectionKey,
                            state.ExpandedSection,
                            StringComparison.OrdinalIgnoreCase);
                    }

                    if (SidebarSections.All(s => !s.IsExpanded))
                    {
                        var defaultSection = SidebarSections.FirstOrDefault(s =>
                            s.SectionKey == GeoTSidebarSectionKeys.Official);
                        if (defaultSection != null)
                            defaultSection.IsExpanded = true;
                    }

                    foreach (var section in SidebarSections)
                    {
                        foreach (var group in section.CategoryGroups)
                        {
                            string compositeKey = BuildCategoryStateKey(section.SectionKey, group.CategoryKey);
                            group.IsExpanded = state.ExpandedCategoryKeys.Contains(compositeKey);
                        }
                    }
                }

            }
            finally
            {
                _isRestoringSidebarState = false;
            }
        }

        private void ApplyFilterExpansionState()
        {
            foreach (var section in SidebarSections)
                section.IsExpanded = false;

            var firstSectionWithResults = SidebarSections.FirstOrDefault(s => s.TotalCount > 0);
            if (firstSectionWithResults != null)
                firstSectionWithResults.IsExpanded = true;

            foreach (var section in SidebarSections)
            {
                foreach (var group in section.CategoryGroups)
                    group.IsExpanded = group.PluginCount > 0;
            }
        }

        private static string BuildCategoryStateKey(string sectionKey, string categoryKey)
            => $"{sectionKey}:{categoryKey}";

        private void SaveSidebarState()
        {
            if (_isRestoringSidebarState)
                return;

            var state = new GeoTSidebarState
            {
                ExpandedSection = SidebarSections.FirstOrDefault(s => s.IsExpanded)?.SectionKey,
                ExpandedCategoryKeys = SidebarSections
                    .SelectMany(s => s.CategoryGroups
                        .Where(g => g.IsExpanded)
                        .Select(g => BuildCategoryStateKey(s.SectionKey, g.CategoryKey)))
                    .ToList()
            };

            GeoTSidebarStateHelper.Save(state);
        }

        /// <summary>
        /// 收集可用标签筛选项。
        /// 有搜索时仅从搜索命中结果取标签；已勾选标签始终保留，避免选项消失后无法取消。
        /// </summary>
        private void LoadAvailableTagFilters()
        {
            var previouslySelected = AvailableTagFilters
                .Where(t => t.IsSelected)
                .Select(t => t.StorageKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string? keyword = string.IsNullOrWhiteSpace(SearchText)
                ? null
                : SearchText.Trim().ToLowerInvariant();

            var tagMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 筛选项来自「当前搜索结果」；无搜索时来自全部模板
            foreach (var plugin in EnumerateAllSidebarPlugins())
            {
                if (!MatchesSearch(plugin, keyword))
                    continue;

                foreach (var storageKey in plugin.StorageTags ?? Enumerable.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(storageKey))
                        continue;

                    string key = storageKey.Trim();
                    if (!tagMap.ContainsKey(key))
                        tagMap[key] = GeoTMineralCategoryHelper.GetDisplayName(key);
                }
            }

            // 已勾选但不在当前搜索结果中的标签仍保留，便于取消勾选
            foreach (var selectedKey in previouslySelected)
            {
                if (tagMap.ContainsKey(selectedKey))
                    continue;

                tagMap[selectedKey] = GeoTMineralCategoryHelper.GetDisplayName(selectedKey);
            }

            // 侧栏尚未建好时（首次初始化），回退到全量实体标签
            if (tagMap.Count == 0 && previouslySelected.Count == 0 && SidebarSections.Count == 0)
            {
                foreach (var entity in GeothermometerService.LoadedEntities)
                {
                    foreach (var storageKey in entity.Tags ?? Enumerable.Empty<string>())
                    {
                        if (string.IsNullOrWhiteSpace(storageKey))
                            continue;

                        string key = storageKey.Trim();
                        if (!tagMap.ContainsKey(key))
                            tagMap[key] = GeoTMineralCategoryHelper.GetDisplayName(key);
                    }
                }
            }

            SyncAvailableTagFilters(tagMap, previouslySelected);
        }

        private void SyncAvailableTagFilters(
            Dictionary<string, string> tagMap,
            HashSet<string> previouslySelected)
        {
            var ordered = tagMap
                .OrderBy(p => p.Value, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            bool needsRebuild = AvailableTagFilters.Count != ordered.Count;
            if (!needsRebuild)
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    var existing = AvailableTagFilters[i];
                    if (!string.Equals(existing.StorageKey, ordered[i].Key, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(existing.DisplayName, ordered[i].Value, StringComparison.Ordinal))
                    {
                        needsRebuild = true;
                        break;
                    }
                }
            }

            if (!needsRebuild)
            {
                OnPropertyChanged(nameof(HasActiveTagFilter));
                OnPropertyChanged(nameof(HasAvailableTagFilters));
                return;
            }

            _isUpdatingTagFilters = true;
            try
            {
                AvailableTagFilters.Clear();
                foreach (var pair in ordered)
                {
                    AvailableTagFilters.Add(new GeoTTagFilterItemViewModel(OnTagFilterSelectionChanged)
                    {
                        StorageKey = pair.Key,
                        DisplayName = pair.Value,
                        IsSelected = previouslySelected.Contains(pair.Key)
                    });
                }
            }
            finally
            {
                _isUpdatingTagFilters = false;
            }

            OnPropertyChanged(nameof(HasActiveTagFilter));
            OnPropertyChanged(nameof(HasAvailableTagFilters));
        }

        private IEnumerable<Geothermometer> EnumerateAllSidebarPlugins()
        {
            foreach (var section in SidebarSections)
            {
                foreach (var group in section.CategoryGroups)
                {
                    if (group.AllPlugins == null)
                        continue;

                    foreach (var plugin in group.AllPlugins)
                    {
                        if (plugin != null)
                            yield return plugin;
                    }
                }
            }
        }

        partial void OnSearchTextChanged(string value) => ScheduleSearchFilter();

        private void OnTagFilterSelectionChanged()
        {
            if (_isUpdatingTagFilters)
                return;

            OnPropertyChanged(nameof(HasActiveTagFilter));
            // 标签筛选立即生效；搜索仍走防抖
            ApplyListFilter();
        }

        private void ScheduleSearchFilter()
        {
            _searchDebounceTimer ??= new DispatcherTimer
            {
                Interval = SearchDebounceInterval
            };
            _searchDebounceTimer.Tick -= OnSearchDebounceTick;
            _searchDebounceTimer.Tick += OnSearchDebounceTick;
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void OnSearchDebounceTick(object? sender, EventArgs e)
        {
            _searchDebounceTimer?.Stop();
            ApplyListFilter();
        }

        /// <summary>
        /// 根据搜索文本与标签筛选条件刷新列表（就地过滤，不重建侧栏树）。
        /// 搜索与筛选互相约束：列表 = 搜索结果 ∩ 标签筛选；筛选项来自当前搜索结果。
        /// </summary>
        private void ApplyListFilter()
        {
            ApplyListFilterCore(preserveExpansionState: false);
        }

        private void ApplyListFilterCore(bool preserveExpansionState)
        {
            int version = Interlocked.Increment(ref _searchFilterVersion);
            bool hasSearch = !string.IsNullOrWhiteSpace(SearchText);
            bool hasTagFilter = HasActiveTagFilter;
            string? keyword = hasSearch ? SearchText.Trim().ToLowerInvariant() : null;

            // 先按搜索结果刷新筛选项，再按搜索∩筛选刷新列表
            LoadAvailableTagFilters();
            hasTagFilter = HasActiveTagFilter;

            foreach (var section in SidebarSections)
            {
                foreach (var group in section.CategoryGroups)
                {
                    if (version != _searchFilterVersion)
                        return;

                    var source = group.AllPlugins ?? new List<Geothermometer>();
                    var filtered = source
                        .Where(p => p != null && MatchesListFilter(p, keyword))
                        .ToList();

                    SyncObservableCollection(group.Plugins ??= new ObservableCollection<Geothermometer>(), filtered);
                    group.NotifyPluginCountChanged();
                }

                section.NotifyTotalCountChanged();
            }

            UpdatePluginCounts();

            if (!preserveExpansionState && (hasSearch || hasTagFilter))
                ApplyFilterExpansionState();
            else if (!preserveExpansionState && !hasSearch && !hasTagFilter)
                ApplySidebarStateFromStorage();
        }

        private static void SyncObservableCollection(
            ObservableCollection<Geothermometer> target,
            IReadOnlyList<Geothermometer> source)
        {
            if (target.Count == source.Count)
            {
                bool identical = true;
                for (int i = 0; i < source.Count; i++)
                {
                    if (!ReferenceEquals(target[i], source[i])
                        && !string.Equals(target[i].Id, source[i].Id, StringComparison.OrdinalIgnoreCase))
                    {
                        identical = false;
                        break;
                    }
                }

                if (identical)
                    return;
            }

            target.Clear();
            foreach (var item in source)
                target.Add(item);
        }

        private bool MatchesListFilter(Geothermometer plugin, string? keyword)
        {
            // 搜索 ∩ 标签筛选
            return MatchesSearch(plugin, keyword) && MatchesTagFilter(plugin);
        }

        private static bool MatchesSearch(Geothermometer plugin, string? keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return true;

            string categoryDisplay = GeoTCategoryHelper.GetDisplayName(plugin.Category) ?? string.Empty;

            return (plugin.Name ?? string.Empty).ToLowerInvariant().Contains(keyword) ||
                   (plugin.TagsDisplayText ?? string.Empty).ToLowerInvariant().Contains(keyword) ||
                   categoryDisplay.ToLowerInvariant().Contains(keyword) ||
                   (plugin.Author ?? string.Empty).ToLowerInvariant().Contains(keyword) ||
                   plugin.Year.ToString().Contains(keyword);
        }

        private bool MatchesTagFilter(Geothermometer plugin)
        {
            var selectedKeys = AvailableTagFilters
                .Where(t => t.IsSelected)
                .Select(t => t.StorageKey)
                .ToList();

            if (selectedKeys.Count == 0)
                return true;

            if (plugin.StorageTags == null || plugin.StorageTags.Count == 0)
                return false;

            var entityKeys = plugin.StorageTags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return selectedKeys.Any(key => entityKeys.Contains(key));
        }

        /// <summary>
        /// 切换标签筛选菜单显示状态
        /// </summary>
        [RelayCommand]
        private void ToggleTagFilterMenu()
        {
            IsTagFilterMenuOpen = !IsTagFilterMenuOpen;
        }

        /// <summary>
        /// 选中一个 GTM：先响应选中态，再异步加载当前语言帮助文档
        /// </summary>
        [RelayCommand]
        private void SelectPlugin(Geothermometer plugin)
        {
            if (plugin == null) return;

            SelectedPlugin = plugin;
            IsPluginApplied = false;
            _selectedFullEntity = BuildWorkingEntityFromPlugin(plugin);

            OnPropertyChanged(nameof(SelectedPluginDisplayName));
            IsHelpDocVisible = true;
            _ = LoadHelpDocumentAsync(plugin.Id, forceReload: false);
            SaveSidebarState();
        }

        /// <summary>
        /// 确认阅读完帮助文档，隐藏文档区域，恢复之前已应用的温压计界面
        /// </summary>
        [RelayCommand]
        private void ConfirmAndShowTable()
        {
            CancelHelpDocLoad();
            IsHelpDocLoading = false;

            // 如果之前有已应用的温压计，恢复到该温压计的状态
            if (_appliedPlugin != null)
            {
                SelectedPlugin = _appliedPlugin;
                _selectedFullEntity = _appliedFullEntity;
                IsPluginApplied = true;
                OnPropertyChanged(nameof(SelectedPluginDisplayName));
            }

            IsHelpDocVisible = false;
        }

        /// <summary>
        /// 切换帮助文档显示状态
        /// </summary>
        [RelayCommand]
        private void ToggleHelpDocument()
        {
            if (SelectedPlugin == null) return;

            if (IsHelpDocVisible)
            {
                CancelHelpDocLoad();
                IsHelpDocLoading = false;
                IsHelpDocVisible = false;
            }
            else
            {
                IsHelpDocVisible = true;
                _ = LoadHelpDocumentAsync(SelectedPlugin.Id, forceReload: false);
            }
        }

        /// <summary>
        /// 异步加载当前语言帮助文档：连点取消上一次，命中缓存则跳过读库
        /// </summary>
        private async Task LoadHelpDocumentAsync(string pluginId, bool forceReload)
        {
            if (_helpRichTextBox == null || string.IsNullOrEmpty(pluginId))
                return;

            CancelHelpDocLoad();
            var cts = new CancellationTokenSource();
            _helpDocLoadCts = cts;
            var token = cts.Token;

            string langCode = LanguageService.GetLanguage();
            string cacheKey = BuildHelpDocCacheKey(pluginId, langCode);

            IsHelpDocLoading = true;
            ClearHelpRichTextBox();

            try
            {
                // 先让出 UI 线程，确保加载遮罩能完成一次渲染
                await Dispatcher.Yield(DispatcherPriority.Render);

                if (token.IsCancellationRequested)
                    return;

                string? rtfContent = null;
                if (!forceReload && _helpDocRtfCache.TryGetValue(cacheKey, out var cached))
                {
                    rtfContent = cached;
                }
                else
                {
                    var entityId = GeothermometerDatabaseService.GenerateId(pluginId);
                    rtfContent = await Task.Run(
                        () => GeothermometerDatabaseService.Instance.GetHelpDocument(entityId, langCode),
                        token).ConfigureAwait(true);
                }

                if (token.IsCancellationRequested)
                    return;

                // 连点后若选中已变，丢弃过期结果
                if (!string.Equals(SelectedPlugin?.Id, pluginId, StringComparison.OrdinalIgnoreCase))
                    return;

                if (!string.IsNullOrEmpty(rtfContent))
                {
                    CacheHelpDocRtf(cacheKey, rtfContent);
                    RtfHelper.LoadRtfString(_helpRichTextBox, rtfContent);
                }
                else
                {
                    ShowHelpDocPlaceholder(LanguageService.Instance["geo_help_no_doc_text"]);
                }
            }
            catch (OperationCanceledException)
            {
                // 连点取消，忽略
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested)
                    return;

                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_load_help_doc_failed"], ex.Message));
                ShowHelpDocPlaceholder(LanguageService.Instance["geo_help_no_doc_text"]);
            }
            finally
            {
                if (ReferenceEquals(_helpDocLoadCts, cts))
                {
                    IsHelpDocLoading = false;
                    _helpDocLoadCts = null;
                }

                cts.Dispose();
            }
        }

        private void CancelHelpDocLoad()
        {
            if (_helpDocLoadCts == null)
                return;

            try
            {
                _helpDocLoadCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 已释放则忽略
            }
        }

        private void ClearHelpRichTextBox()
        {
            if (_helpRichTextBox == null)
                return;

            _helpRichTextBox.Document.Blocks.Clear();
        }

        private void ShowHelpDocPlaceholder(string text)
        {
            if (_helpRichTextBox == null)
                return;

            _helpRichTextBox.Document.Blocks.Clear();
            var run = new Run(text)
            {
                Foreground = Brushes.Gray,
                FontSize = 14
            };
            _helpRichTextBox.Document.Blocks.Add(new Paragraph(run));
        }

        private static string BuildHelpDocCacheKey(string pluginId, string langCode)
            => $"{pluginId}|{langCode}";

        private void CacheHelpDocRtf(string cacheKey, string rtfContent)
        {
            if (_helpDocRtfCache.ContainsKey(cacheKey))
            {
                _helpDocRtfCache[cacheKey] = rtfContent;
                return;
            }

            if (_helpDocRtfCache.Count >= MaxHelpDocCacheEntries)
            {
                var oldestKey = _helpDocRtfCache.Keys.First();
                _helpDocRtfCache.Remove(oldestKey);
            }

            _helpDocRtfCache[cacheKey] = rtfContent;
        }

        private void InvalidateHelpDocCache(string? pluginId = null)
        {
            if (string.IsNullOrEmpty(pluginId))
            {
                _helpDocRtfCache.Clear();
                return;
            }

            string prefix = pluginId + "|";
            var keysToRemove = _helpDocRtfCache.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in keysToRemove)
                _helpDocRtfCache.Remove(key);
        }

        /// <summary>
        /// 由侧栏轻量模型构造工作实体（不含脚本与帮助文档）
        /// </summary>
        private static GeothermometerEntity BuildWorkingEntityFromPlugin(Geothermometer plugin)
        {
            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            var summary = GeothermometerService.LoadedEntities
                .FirstOrDefault(e => e.Id == entityId);

            if (summary != null)
            {
                return new GeothermometerEntity
                {
                    Id = summary.Id,
                    PluginId = summary.PluginId,
                    Version = summary.Version,
                    FileHash = summary.FileHash,
                    LastModified = summary.LastModified,
                    IsOfficial = summary.IsOfficial,
                    IsFavorite = summary.IsFavorite,
                    Category = summary.Category,
                    Tags = summary.Tags != null ? new List<string>(summary.Tags) : new List<string>(),
                    Name = summary.Name,
                    NameLangKey = summary.NameLangKey,
                    Author = summary.Author,
                    Year = summary.Year,
                    Reference = summary.Reference,
                    IconCode = summary.IconCode,
                    IconColor = summary.IconColor,
                    Headers = summary.Headers != null ? new List<string>(summary.Headers) : new List<string>(),
                    ExampleRow = summary.ExampleRow != null ? new List<string>(summary.ExampleRow) : new List<string>(),
                    FormulaName = summary.FormulaName,
                    InputColumns = summary.InputColumns != null ? new List<string>(summary.InputColumns) : new List<string>(),
                    AdditionalFormulas = summary.AdditionalFormulas != null
                        ? new List<AdditionalFormula>(summary.AdditionalFormulas)
                        : new List<AdditionalFormula>(),
                    ScriptContent = string.Empty,
                    HelpDocuments = new Dictionary<string, string>()
                };
            }

            return new GeothermometerEntity
            {
                Id = entityId,
                PluginId = plugin.Id,
                Version = plugin.Version,
                IsOfficial = plugin.IsBuiltIn,
                IsFavorite = plugin.IsFavorite,
                Category = plugin.Category,
                Tags = plugin.StorageTags != null ? new List<string>(plugin.StorageTags) : new List<string>(),
                Name = plugin.Name,
                NameLangKey = plugin.NameLangKey,
                Author = plugin.Author,
                Year = plugin.Year,
                Reference = plugin.Reference,
                IconCode = plugin.IconCode,
                IconColor = plugin.IconColor,
                Headers = plugin.Headers != null ? new List<string>(plugin.Headers) : new List<string>(),
                ExampleRow = plugin.ExampleRow != null ? new List<string>(plugin.ExampleRow) : new List<string>(),
                FormulaName = plugin.FormulaName,
                InputColumns = plugin.InputColumns != null ? new List<string>(plugin.InputColumns) : new List<string>(),
                AdditionalFormulas = plugin.AdditionalFormulas != null
                    ? new List<AdditionalFormula>(plugin.AdditionalFormulas)
                    : new List<AdditionalFormula>(),
                ScriptContent = string.Empty,
                HelpDocuments = new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// 计算/应用前按需补齐脚本内容（不加载帮助文档）
        /// </summary>
        private void EnsureSelectedEntityScriptLoaded()
        {
            if (_selectedFullEntity == null)
                return;

            if (!string.IsNullOrEmpty(_selectedFullEntity.ScriptContent))
                return;

            var withScript = GeothermometerDatabaseService.Instance.GetEntityForRegistration(_selectedFullEntity.Id);
            if (withScript == null || string.IsNullOrEmpty(withScript.ScriptContent))
                return;

            _selectedFullEntity.ScriptContent = withScript.ScriptContent;
            if (withScript.AdditionalFormulas != null && withScript.AdditionalFormulas.Count > 0)
                _selectedFullEntity.AdditionalFormulas = withScript.AdditionalFormulas;
        }

        /// <summary>
        /// 导出指定温压计为包文件（.gngtm，内容仍为 zip）
        /// </summary>
        [RelayCommand]
        private async Task ExportPlugin(Geothermometer? plugin)
        {
            plugin ??= SelectedPlugin;
            if (plugin == null)
            {
                MessageHelper.Info(LanguageService.Instance["geo_msg_select_thermometer_first"]);
                return;
            }

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            var entity = GeothermometerDatabaseService.Instance.GetEntity(entityId);
            if (entity == null)
            {
                MessageHelper.Error(LanguageService.Instance["geo_msg_export_failed"]);
                return;
            }

            try
            {
                string? filePath = await FileHelper.GetSaveFilePath2Async(
                    title: LanguageService.Instance["geo_msg_export_dialog_title"],
                    filter: FileDialogFilterHelper.GeothermometerPackageFiles,
                    defaultExt: TemplatePackageFileExtensions.GeothermometerPrimary,
                    defaultFileName: plugin.Name);
                if (string.IsNullOrEmpty(filePath)) return;

                await Task.Run(() => GeothermometerService.ExportToZip(entity.Id, filePath));
                MessageHelper.Success(LanguageService.Instance["geo_msg_export_success"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_export_failed"], ex.Message));
            }
        }

        /// <summary>
        /// 从包文件导入 GTM（支持 .gngtm / .zip 多选）
        /// </summary>
        [RelayCommand]
        private void ImportPlugin()
        {
            var openFileDialog = new VistaOpenFileDialog
            {
                Title = LanguageService.Instance["geo_menu_import_zip"],
                Filter = FileDialogFilterHelper.ImportGeothermometerPackages,
                DefaultExt = TemplatePackageFileExtensions.GeothermometerPrimary,
                CheckFileExists = true,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            ImportPluginsBatch(openFileDialog.FileNames);
        }

        /// <summary>
        /// 从指定 ZIP 路径导入 GTM（支持拖入文件，可多文件）
        /// </summary>
        [RelayCommand]
        private void ImportPluginFromPath(object? parameter)
        {
            var filePaths = ExtractImportFilePaths(parameter);
            if (filePaths.Count == 0)
                return;

            ImportPluginsBatch(filePaths);
        }

        private static IReadOnlyList<string> ExtractImportFilePaths(object? parameter)
        {
            return parameter switch
            {
                string singlePath when !string.IsNullOrWhiteSpace(singlePath) => new[] { singlePath },
                string[] paths => paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray(),
                IEnumerable<string> paths => paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray(),
                _ => Array.Empty<string>()
            };
        }

        private void ImportPluginsBatch(IEnumerable<string> filePaths)
        {
            var paths = filePaths
                .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                .Where(p => TemplatePackageFileExtensions.IsGeothermometerPackagePath(p))
                .ToList();

            if (paths.Count == 0)
                return;

            int successCount = 0;

            foreach (var filePath in paths)
            {
                try
                {
                    GeothermometerService.ImportFromZip(filePath);
                    successCount++;
                }
                catch (GeothermometerImportException ex)
                {
                    string message = ex.Reason switch
                    {
                        GeothermometerImportFailureReason.VersionIncompatible =>
                            LanguageService.Instance["template_version_too_high"],
                        _ => LanguageService.Instance["geo_msg_import_invalid_format"]
                    };
                    MessageHelper.Error($"{Path.GetFileName(filePath)}: {message}");
                }
                catch (InvalidOperationException ex)
                {
                    MessageHelper.Error($"{Path.GetFileName(filePath)}: {string.Format(LanguageService.Instance["geo_msg_import_formula_name_rejected"], ex.Message)}");
                }
                catch (Exception ex)
                {
                    MessageHelper.Error($"{Path.GetFileName(filePath)}: {string.Format(LanguageService.Instance["geo_msg_import_failed_detail"], ex.Message)}");
                }
            }

            if (successCount <= 0)
                return;

            // ImportFromZip 已增量 UpsertLoadedPlugin，只需刷新侧栏
            InvalidateHelpDocCache();
            LoadSidebarSections();

            if (successCount == 1 && paths.Count == 1)
            {
                MessageHelper.Success(LanguageService.Instance["geo_msg_import_success"]);
                return;
            }

            MessageHelper.Success(string.Format(
                LanguageService.Instance["geo_msg_batch_import_success"],
                successCount));
        }

        /// <summary>
        /// 应用选中的 GTM 到 ReoGrid 表格
        /// </summary>
        [RelayCommand]
        private async Task ApplyPlugin(ReoGridControl reoGridControl)
        {
            if (SelectedPlugin == null || reoGridControl == null) return;

            try
            {
                bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                    LanguageService.Instance["geo_dialog_apply_overwrite"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]);

                if (!isConfirmed) return;

                // 应用时再按需加载脚本（点选阶段不读 ScriptContent）
                EnsureSelectedEntityScriptLoaded();

                Worksheet? worksheet = reoGridControl.CurrentWorksheet;
                if (worksheet == null)
                {
                    if (reoGridControl.Worksheets.Count == 0) return;
                    worksheet = reoGridControl.Worksheets[0];
                    reoGridControl.CurrentWorksheet = worksheet;
                }

                var currentScale = worksheet.ScaleFactor;
                worksheet.Reset();
                worksheet.ScaleFactor = currentScale;
                worksheet.RowCount = InitialWorksheetRowCount;

                // 设置整个工作表默认居中对齐
                worksheet.SetRangeStyles(RangePosition.EntireRange, new WorksheetRangeStyle
                {
                    Flag = PlainStyleFlag.HorizontalAlign,
                    HAlign = ReoGridHorAlign.Center
                });

                var headers = SelectedPlugin.Headers;
                var exampleRow = CommaSeparatedListHelper.AlignToHeaderCount(headers, SelectedPlugin.ExampleRow);

                // 设置表头
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet[0, i] = headers[i];
                    worksheet.Cells[0, i].IsReadOnly = true;
                }

                // 设置示例数据
                for (int i = 0; i < exampleRow.Count && i < headers.Count; i++)
                {
                    worksheet[1, i] = exampleRow[i];
                }

                // 设置表头样式
                var headerRange = new RangePosition(0, 0, 1, headers.Count);
                worksheet.SetRangeStyles(headerRange, new WorksheetRangeStyle
                {
                    Flag = PlainStyleFlag.BackColor | PlainStyleFlag.TextColor |
                           PlainStyleFlag.FontStyleBold | PlainStyleFlag.HorizontalAlign,
                    HAlign = ReoGridHorAlign.Center,
                    BackColor = Colors.LightGray,
                    TextColor = Colors.Black,
                    Bold = true
                });
                ApplyHeaderSideBorders(worksheet, headers.Count);

                // 设置示例行样式
                var exampleRange = new RangePosition(1, 0, 1, headers.Count);
                worksheet.SetRangeStyles(exampleRange, new WorksheetRangeStyle
                {
                    Flag = PlainStyleFlag.BackColor | PlainStyleFlag.TextColor |
                           PlainStyleFlag.FontStyleBold | PlainStyleFlag.HorizontalAlign,
                    HAlign = ReoGridHorAlign.Center,
                    TextColor = Colors.Black,
                });

                // 设置列宽和温度列格式
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.AutoFitColumnWidth(i);
                    var currentWidth = worksheet.GetColumnWidth(i);
                    worksheet.SetColumnsWidth(i, 1, (ushort)(currentWidth + 10));

                    if (IsTemperatureHeader(headers[i]))
                    {
                        var range = new RangePosition(1, i, worksheet.RowCount - 1, 1);
                        worksheet.SetRangeDataFormat(range,
                            unvell.ReoGrid.DataFormat.CellDataFormatFlag.Number,
                            new unvell.ReoGrid.DataFormat.NumberDataFormatter.NumberFormatArgs
                            {
                                DecimalPlaces = 0
                            });
                    }
                }

                // 冻结表头行，使滚动时始终显示
                worksheet.FreezeToCell(1, 0);

                AttachWorksheetRowExpansionEvents(worksheet);

                // 记录已应用的温压计，用于确定按钮恢复
                _appliedPlugin = SelectedPlugin;
                _appliedFullEntity = _selectedFullEntity;

                // 复位 ReoGrid 选区，并通知 View 重置横向/纵向滚动位置
                worksheet.SelectRange(new RangePosition(1, 0, 1, 1));
                GridScrollResetToken++;

                // 应用后确保切换到表格视图，并清除上一模板的计算详情
                IsHelpDocVisible = false;
                IsPluginApplied = true;
                ClearCalculationData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("创建表格失败: " + ex.Message);
            }
        }

        private static void ApplyHeaderSideBorders(Worksheet worksheet, int headerCount)
        {
            for (int i = 0; i < headerCount; i++)
            {
                var cellRange = new RangePosition(0, i, 1, 1);
                worksheet.SetRangeBorders(cellRange, BorderPositions.Left, RangeBorderStyle.SilverSolid);
                worksheet.SetRangeBorders(cellRange, BorderPositions.Right, RangeBorderStyle.SilverSolid);
            }
        }

        /// <summary>
        /// 绑定工作表行扩展相关事件（粘贴扩行、末行编辑自动增行）
        /// </summary>
        public void AttachWorksheetRowExpansionEvents(Worksheet? worksheet)
        {
            if (_rowExpansionWorksheet != null)
            {
                _rowExpansionWorksheet.BeforePaste -= Worksheet_BeforePaste;
                _rowExpansionWorksheet.CellDataChanged -= Worksheet_CellDataChanged;
            }

            _rowExpansionWorksheet = worksheet;

            if (worksheet == null)
                return;

            worksheet.BeforePaste += Worksheet_BeforePaste;
            worksheet.CellDataChanged += Worksheet_CellDataChanged;
        }

        private void Worksheet_BeforePaste(object? sender, BeforeRangeOperationEventArgs e)
        {
            if (sender is not Worksheet worksheet)
                return;

            if (!Clipboard.ContainsText())
                return;

            string pasteText = Clipboard.GetText();
            if (string.IsNullOrEmpty(pasteText))
                return;

            var lines = pasteText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int pastedRowCount = lines.Length;

            if (string.IsNullOrEmpty(lines.Last()))
                pastedRowCount--;

            if (pastedRowCount <= 0)
                return;

            int startRow = worksheet.SelectionRange.Row;
            int requiredTotalRows = startRow + pastedRowCount;

            if (requiredTotalRows > worksheet.RowCount)
                worksheet.RowCount = requiredTotalRows;
        }

        private void Worksheet_CellDataChanged(object? sender, CellEventArgs e)
        {
            if (sender is not Worksheet worksheet)
                return;

            int row = e.Cell.Position.Row;
            if (row >= worksheet.RowCount - 1)
                worksheet.RowCount++;
        }

        /// <summary>
        /// 处理 ReoGrid 行选中事件，提取输入参数并计算中间步骤
        /// 由 View 的 code-behind 调用
        /// </summary>
        public void OnRowSelected(Worksheet worksheet, int row)
        {
            if (_selectedFullEntity == null || worksheet == null || row < 1)
            {
                ClearCalculationData();
                return;
            }

            EnsureSelectedEntityScriptLoaded();

            var inputColumns = _selectedFullEntity.InputColumns;
            if (inputColumns == null || inputColumns.Count == 0)
            {
                ClearCalculationData();
                return;
            }

            var headers = _selectedFullEntity.Headers;
            if (headers == null || headers.Count == 0)
            {
                ClearCalculationData();
                return;
            }

            try
            {
                // 根据 InputColumns 定义，从表头中找到对应列索引，提取该行数据
                var inputValues = new List<double>();
                bool allValid = true;

                foreach (var inputCol in inputColumns)
                {
                    int colIndex = headers.IndexOf(inputCol);
                    if (colIndex < 0)
                    {
                        allValid = false;
                        break;
                    }

                    var cellData = worksheet.GetCellData(row, colIndex);
                    if (cellData != null && double.TryParse(cellData.ToString(), out double val))
                    {
                        inputValues.Add(val);
                    }
                    else
                    {
                        allValid = false;
                        break;
                    }
                }

                if (!allValid || inputValues.Count == 0)
                {
                    ClearCalculationData();
                    return;
                }

                _lastCalculationInputValues = inputValues.ToArray();
                var steps = ApplyCalculationSteps(_lastCalculationInputValues);

                if (steps.Count > 0)
                {
                    SelectedRowInfo = $"Row {row} - {SelectedPluginDisplayName}";
                    HasCalculationData = true;
                }
                else
                {
                    ClearCalculationData();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewModel] 详细计算失败: {ex.Message}");
                ClearCalculationData();
            }
        }

        private List<CalculationStep> ApplyCalculationSteps(double[] inputValues)
        {
            if (_selectedFullEntity == null)
                return new List<CalculationStep>();

            var steps = GeothermometerService.ExecuteDetailedCalculation(_selectedFullEntity, inputValues);
            CalculationSteps.Clear();
            foreach (var step in steps)
            {
                CalculationSteps.Add(step);
            }

            return steps;
        }

        /// <summary>
        /// 清除计算数据，不改变用户当前的详情面板展开/收缩状态。
        /// </summary>
        private void ClearCalculationData()
        {
            _lastCalculationInputValues = null;
            CalculationSteps.Clear();
            HasCalculationData = false;
            SelectedRowInfo = string.Empty;
            DetailScrollResetToken++;
        }

        /// <summary>
        /// 最小化计算详情面板（收起为底部长条）
        /// </summary>
        [RelayCommand]
        private void MinimizeDetailPanel()
        {
            IsDetailPanelMinimized = true;
        }

        /// <summary>
        /// 从最小化状态还原计算详情面板
        /// </summary>
        [RelayCommand]
        private void RestoreDetailPanel()
        {
            IsDetailPanelMinimized = false;
        }

        private void ShowUpdateOverlay(bool indeterminate, string statusText, double progress = 0)
        {
            if (_isAutoChecking && indeterminate)
                return;

            IsUpdateProgressIndeterminate = indeterminate;
            UpdateProgress = progress;
            UpdateStatusText = statusText;
            IsUpdateOverlayVisible = true;
        }

        private void HideUpdateOverlay()
        {
            IsUpdateOverlayVisible = false;
            IsUpdateProgressIndeterminate = true;
            UpdateProgress = 0;
        }

        private static string ShowGeothermometerDownloadResult(GeothermometerBatchDownloadResult downloadResult)
        {
            var resultMessage = new StringBuilder();
            resultMessage.AppendLine(string.Format(
                LanguageService.Instance["batch_download_success_count"],
                downloadResult.SuccessCount));

            if (downloadResult.RemovalCount > 0)
            {
                resultMessage.AppendLine(string.Format(
                    LanguageService.Instance["geo_msg_update_removed_count"],
                    downloadResult.RemovalCount));
            }

            if (downloadResult.Failures.Count > 0)
            {
                resultMessage.AppendLine(string.Format(
                    LanguageService.Instance["batch_download_fail_count"],
                    downloadResult.Failures.Count));
                resultMessage.AppendLine(LanguageService.Instance["geo_msg_update_download_failed_items"]);

                foreach (var failure in downloadResult.Failures.Take(10))
                {
                    resultMessage.AppendLine(string.Format(
                        LanguageService.Instance["geo_msg_update_download_failed_detail"],
                        failure.PluginId,
                        failure.ErrorMessage));
                }

                if (downloadResult.Failures.Count > 10)
                    resultMessage.AppendLine("...");
            }

            string message = resultMessage.ToString().TrimEnd();

            if (downloadResult.Failures.Count > 0
                && downloadResult.SuccessCount == 0
                && downloadResult.RemovalCount == 0)
            {
                MessageHelper.Error(message);
            }
            else if (downloadResult.Failures.Count > 0)
            {
                MessageHelper.Warning(message);
            }
            else
            {
                MessageHelper.Success(message);
            }

            return message;
        }

        /// <summary>
        /// 检查 GTM 更新
        /// </summary>
        [RelayCommand]
        private async Task CheckForPluginUpdates()
        {
            if (IsCheckingUpdates) return;

            IsCheckingUpdates = true;
            ShowUpdateOverlay(true, LanguageService.Instance["geo_msg_checking_update"]);

            try
            {
                var checkResult = await GeothermometerService.CheckForUpdatesAsync();
                bool listNeedsReload = checkResult.MineralCategoriesSynced;

                if (checkResult.Status == GeothermometerUpdateCheckStatus.Failed)
                {
                    UpdateStatusText = LanguageService.Instance["geo_msg_check_update_failed"];
                    if (!_isAutoChecking)
                    {
                        MessageHelper.Error($"{UpdateStatusText}: {checkResult.ErrorMessage}");
                    }
                    return;
                }

                int changeCount = checkResult.Updates.Count + checkResult.Removals.Count;
                AvailableUpdateCount = changeCount;

                if (checkResult.HasChanges)
                {
                    string msg = string.Format(LanguageService.Instance["geo_msg_update_available"], changeCount);
                    UpdateStatusText = msg;

                    HideUpdateOverlay();

                    bool confirmed = await MessageHelper.ShowAsyncDialog(
                        msg,
                        LanguageService.Instance["Cancel"],
                        LanguageService.Instance["Confirm"]);

                    if (confirmed)
                    {
                        ShowUpdateOverlay(false, LanguageService.Instance["geo_msg_downloading_update"]);

                        var progress = new Progress<(int current, int total, string name)>(p =>
                        {
                            UpdateProgress = p.total > 0 ? (double)p.current / p.total * 100 : 0;
                            UpdateStatusText = string.Format(
                                LanguageService.Instance["batch_download_progress"],
                                p.current,
                                p.total,
                                p.name);
                        });

                        var downloadResult = await GeothermometerService.DownloadAndReloadAsync(
                            checkResult.Updates,
                            checkResult.Removals,
                            progress);

                        string result = ShowGeothermometerDownloadResult(downloadResult);
                        UpdateStatusText = result;

                        if (downloadResult.SuccessCount > 0 || downloadResult.RemovalCount > 0)
                        {
                            InvalidateHelpDocCache();
                            listNeedsReload = true;
                        }
                    }
                    else
                    {
                        UpdateStatusText = string.Empty;
                    }
                }
                else
                {
                    UpdateStatusText = LanguageService.Instance["geo_msg_already_latest"];
                    if (!_isAutoChecking)
                    {
                        MessageHelper.Info(UpdateStatusText);
                    }
                }

                if (listNeedsReload)
                    ReloadCategoryGroupsAndSelection();
            }
            catch (Exception ex)
            {
                UpdateStatusText = LanguageService.Instance["geo_msg_check_update_failed"];
                MessageHelper.Error($"{UpdateStatusText}: {ex.Message}");
            }
            finally
            {
                HideUpdateOverlay();
                IsCheckingUpdates = false;
            }
        }

        /// <summary>
        /// 打开数据文件
        /// </summary>
        [RelayCommand]
        public void OpenExcelFile(ReoGridControl reoGridControl)
        {
            string? filePath = FileHelper.GetFilePath(LanguageService.Instance["csv_file_filter"]);
            if (filePath != null)
            {
                reoGridControl.Load(filePath);
                MessageHelper.Success(LanguageService.Instance["geo_msg_import_success"]);
            }
            else
            {
                MessageHelper.Info(LanguageService.Instance["geo_msg_import_cancelled"]);
            }
        }

        /// <summary>
        /// 导出当前工作表
        /// </summary>
        [RelayCommand]
        public async Task ExportWorksheet(ReoGridControl reoGridControl)
        {
            var worksheet = reoGridControl.CurrentWorksheet;
            if (worksheet == null) return;

            string? tempFilePath = await FileHelper.GetSaveFilePath2Async(
                title: LanguageService.Instance["geo_msg_csv_save_title"],
                filter: LanguageService.Instance["csv_file_filter"],
                defaultExt: ".csv",
                defaultFileName: worksheet.Name);
            if (string.IsNullOrEmpty(tempFilePath)) return;

            try
            {
                var range = worksheet.UsedRange;
                var csvBuilder = new StringBuilder();

                for (int r = range.Row; r <= range.EndRow; r++)
                {
                    var rowValues = new List<string>();
                    for (int c = range.Col; c <= range.EndCol; c++)
                    {
                        string cellValue = worksheet.GetCellText(r, c) ?? "";
                        if (cellValue.Contains(',') || cellValue.Contains('"'))
                        {
                            cellValue = $"\"{cellValue.Replace("\"", "\"\"")}\"";
                        }
                        rowValues.Add(cellValue);
                    }
                    csvBuilder.AppendLine(string.Join(",", rowValues));
                }

                var csvContent = csvBuilder.ToString();
                await Task.Run(() => File.WriteAllText(tempFilePath, csvContent, new UTF8Encoding(true)));
                MessageHelper.Success(LanguageService.Instance["geo_msg_export_success"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_export_failed"], ex.Message));
            }
        }

        /// <summary>
        /// 新建工作表
        /// </summary>
        [RelayCommand]
        public void CreateWorkSheet(ReoGridControl reoGridControl)
        {
            try
            {
                Worksheet newWorksheet = reoGridControl.CreateWorksheet();
                reoGridControl.Worksheets.Add(newWorksheet);
                reoGridControl.CurrentWorksheet = newWorksheet;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(LanguageService.Instance["geo_msg_create_table_failed"], ex.Message));
            }
        }

        // ==================== 温压计管理 ====================

        /// <summary>
        /// 打开自由温压计表格窗口
        /// </summary>
        [RelayCommand]
        private void OpenFreeSheet()
        {
            GeothermometerFreeSheetWindow.ShowOrActivate();
        }

        /// <summary>
        /// 新建温压计（打开编辑器窗口）
        /// </summary>
        [RelayCommand]
        private void CreateCustomThermometer()
        {
            var editorVm = new GeothermometerEditorViewModel
            {
                OnSaved = () =>
                {
                    // SaveEntity 已增量 UpsertLoadedPlugin
                    InvalidateHelpDocCache();
                    LoadSidebarSections();
                }
            };

            var window = new GeothermometerEditorWindow(editorVm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        /// <summary>
        /// 编辑温压计（支持官方和自定义）
        /// </summary>
        [RelayCommand]
        private void EditCustomThermometer(Geothermometer plugin)
        {
            if (plugin == null) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            var entity = GeothermometerDatabaseService.Instance.GetEntity(entityId);
            if (entity == null) return;

            var editorVm = new GeothermometerEditorViewModel
            {
                OnSaved = () =>
                {
                    // SaveEntity 已增量 UpsertLoadedPlugin
                    InvalidateHelpDocCache(plugin.Id);
                    LoadSidebarSections();
                }
            };
            editorVm.LoadEntity(entity);

            var window = new GeothermometerEditorWindow(editorVm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        /// <summary>
        /// 删除温压计（支持官方和自定义）
        /// </summary>
        [RelayCommand]
        private async Task DeleteCustomThermometer(Geothermometer plugin)
        {
            if (plugin == null) return;

            string typeLabel = plugin.IsBuiltIn
                ? LanguageService.Instance["geo_label_official"]
                : LanguageService.Instance["geo_label_custom"];
            bool confirmed = await MessageHelper.ShowAsyncDialog(
                string.Format(LanguageService.Instance["geo_dialog_delete_thermometer"], typeLabel, plugin.Name),
                LanguageService.Instance["Cancel"],
                LanguageService.Instance["Confirm"]);

            if (!confirmed) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            if (GeothermometerService.DeleteEntity(entityId))
            {
                InvalidateHelpDocCache(plugin.Id);
                if (string.Equals(SelectedPlugin?.Id, plugin.Id, StringComparison.OrdinalIgnoreCase))
                {
                    CancelHelpDocLoad();
                    IsHelpDocLoading = false;
                    SelectedPlugin = null;
                    _selectedFullEntity = null;
                    IsHelpDocVisible = false;
                }

                LoadSidebarSections();
                MessageHelper.Success(LanguageService.Instance["geo_msg_delete_success"]);
            }
        }

        /// <summary>
        /// 开发者模式：强制从服务器重新下载官方温压计模板
        /// </summary>
        [RelayCommand]
        private async Task ForceUpdatePlugin(Geothermometer plugin)
        {
            if (plugin == null || !plugin.IsBuiltIn || !IsDeveloperMode)
                return;

            if (IsCheckingUpdates)
                return;

            IsCheckingUpdates = true;
            ShowUpdateOverlay(true, LanguageService.Instance["geo_msg_downloading_update"]);

            try
            {
                var (entry, errorMessage) = await GeothermometerService.FetchFreshPluginIndexEntryAsync(plugin.Id);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    MessageHelper.Error($"{LanguageService.Instance["geo_msg_check_update_failed"]}: {errorMessage}");
                    return;
                }

                if (entry == null)
                {
                    MessageHelper.Error(LanguageService.Instance["force_update_template_not_found"]);
                    return;
                }

                var result = await GeothermometerService.DownloadPluginAsync(entry);

                if (result.Success)
                {
                    // DownloadPluginAsync 已增量 UpsertLoadedPlugin
                    InvalidateHelpDocCache(plugin.Id);
                    LoadSidebarSections();
                    MessageHelper.Success(string.Format(LanguageService.Instance["geo_msg_update_downloaded"], 1));
                }
                else
                {
                    MessageHelper.Error(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error($"{LanguageService.Instance["geo_msg_check_update_failed"]}: {ex.Message}");
            }
            finally
            {
                HideUpdateOverlay();
                IsCheckingUpdates = false;
            }
        }

        /// <summary>
        /// 转换为官方温压计
        /// </summary>
        [RelayCommand]
        private void ConvertToOfficial(Geothermometer plugin)
        {
            if (plugin == null) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            if (GeothermometerService.ConvertToOfficial(entityId))
            {
                LoadSidebarSections();
                MessageHelper.Success(LanguageService.Instance["geo_msg_promoted"]);
            }
        }

        /// <summary>
        /// 降级为自定义温压计
        /// </summary>
        [RelayCommand]
        private void ConvertToCustom(Geothermometer plugin)
        {
            if (plugin == null) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            if (GeothermometerService.ConvertToCustom(entityId))
            {
                LoadSidebarSections();
                MessageHelper.Success(LanguageService.Instance["geo_msg_demoted"]);
            }
        }

        /// <summary>
        /// 切换收藏状态
        /// </summary>
        [RelayCommand]
        private void ToggleFavorite(Geothermometer? plugin)
        {
            plugin ??= SelectedPlugin;
            if (plugin == null)
                return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            var entity = GeothermometerDatabaseService.Instance.GetEntity(entityId);
            if (entity == null)
                return;

            entity.IsFavorite = !entity.IsFavorite;
            GeothermometerDatabaseService.Instance.UpsertEntity(entity);

            var loaded = GeothermometerService.LoadedEntities
                .FirstOrDefault(e => string.Equals(e.PluginId, plugin.Id, StringComparison.OrdinalIgnoreCase));
            if (loaded != null)
                loaded.IsFavorite = entity.IsFavorite;

            plugin.IsFavorite = entity.IsFavorite;

            string message = entity.IsFavorite
                ? LanguageService.Instance["favorite_added"]
                : LanguageService.Instance["favorite_removed"];
            MessageHelper.Success(message);

            string? selectedId = SelectedPlugin?.Id;
            SaveSidebarState();
            // 收藏状态变化会影响「收藏」区块成员，需重建侧栏数据源
            LoadSidebarSections();

            if (!string.IsNullOrEmpty(selectedId))
            {
                SelectedPlugin = FindPluginById(selectedId);
                OnPropertyChanged(nameof(SelectedPluginDisplayName));
            }
        }

        /// <summary>
        /// 展开/折叠一级侧栏区块（手风琴：同时仅一个展开）
        /// </summary>
        [RelayCommand]
        private void ToggleSidebarSection(GeoTSidebarSectionViewModel? section)
        {
            if (section == null)
                return;

            bool willExpand = !section.IsExpanded;

            foreach (var item in SidebarSections)
                item.IsExpanded = item == section && willExpand;

            SaveSidebarState();
        }

        /// <summary>
        /// 展开/折叠类别分组
        /// </summary>
        [RelayCommand]
        private void ToggleCategoryGroup(CategoryGroupViewModel? group)
        {
            if (group == null)
                return;

            group.IsExpanded = !group.IsExpanded;
            SaveSidebarState();
        }

        /// <summary>
        /// 判断表头是否为温度列（T(℃) 或 T(K)）
        /// </summary>
        private static bool IsTemperatureHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return false;

            return header.Contains("T(K)", StringComparison.OrdinalIgnoreCase)
                || header.Contains("T(\u2103)", StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// 温压计标签筛选项 ViewModel
    /// </summary>
    public partial class GeoTTagFilterItemViewModel : ObservableObject
    {
        private readonly Action? _onSelectionChanged;

        public GeoTTagFilterItemViewModel(Action? onSelectionChanged = null)
        {
            _onSelectionChanged = onSelectionChanged;
        }

        public string StorageKey { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        [ObservableProperty]
        private bool isSelected;

        partial void OnIsSelectedChanged(bool value) => _onSelectionChanged?.Invoke();
    }

    /// <summary>
    /// 温压计侧栏一级区块 ViewModel（收藏 / 官方 / 自定义）
    /// </summary>
    public partial class GeoTSidebarSectionViewModel : ObservableObject
    {
        public string SectionKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        [ObservableProperty]
        private bool isExpanded;

        public ObservableCollection<CategoryGroupViewModel> CategoryGroups { get; } = new();

        public int TotalCount => CategoryGroups.Sum(g => g.PluginCount);

        public void NotifyTotalCountChanged() => OnPropertyChanged(nameof(TotalCount));
    }

    /// <summary>
    /// 温压计类别分组 ViewModel（支持 UI 绑定和展开/折叠）
    /// </summary>
    public partial class CategoryGroupViewModel : ObservableObject
    {
        public string SectionKey { get; set; } = string.Empty;

        public string CategoryKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        [ObservableProperty]
        private bool isExpanded;

        /// <summary>
        /// 该分组下的完整列表（筛选前），用于就地过滤
        /// </summary>
        public List<Geothermometer> AllPlugins { get; set; } = new();

        public ObservableCollection<Geothermometer> Plugins { get; set; } = new();

        public int PluginCount => Plugins.Count;

        public void NotifyPluginCountChanged() => OnPropertyChanged(nameof(PluginCount));
    }
}
