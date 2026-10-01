using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Converter;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 官方发布器：多语言公告目录（Announcements.json）编辑与发布。
    /// </summary>
    public partial class OfficialTemplatePublisherViewModel
    {
        /// <summary>公告编辑语言上下文（供 LocalizedStringControl 使用）</summary>
        public ContentLanguageContext AnnouncementsLanguageContext { get; } = new();

        public ObservableCollection<AnnouncementEditorItemViewModel> AnnouncementEntries { get; } = new();

        /// <summary>
        /// 已成功拿到线上公告目录后为 true。下载失败时不能把空编辑器当成“已同步”。
        /// </summary>
        private bool _announcementsCatalogRemoteKnown;

        private string _remoteAnnouncementsFingerprint = string.Empty;

        private bool _suppressAnnouncementCatalogTracking;

        [ObservableProperty]
        private AnnouncementEditorItemViewModel? selectedAnnouncementEntry;

        [ObservableProperty]
        private string selectedAnnouncementsLanguage = AppCultureRegistry.DefaultContentLanguage;

        /// <summary>预设主题名，对应客户端内置渐变。未填自定义颜色时使用。</summary>
        public IReadOnlyList<AnnouncementEditorChoice> AnnouncementThemeOptions { get; } =
            AnnouncementBrushes.ThemeKeys
                .Select(key => new AnnouncementEditorChoice(key, ThemeLabel(key)))
                .ToArray();

        private static string ThemeLabel(string key) => key switch
        {
            "blue" => "蓝色",
            "purple" => "紫色",
            "teal" => "青绿",
            "orange" => "橙色",
            "red" => "红色",
            "green" => "绿色",
            "indigo" => "靛蓝",
            "rose" => "玫红",
            "cyan" => "青色",
            "gold" => "金色",
            "slate" => "灰蓝",
            "magenta" => "品红",
            "navy" => "藏青",
            "copper" => "铜棕",
            _ => key
        };

        public IReadOnlyList<AnnouncementEditorChoice> AnnouncementBackgroundModeOptions { get; } = new[]
        {
            new AnnouncementEditorChoice(
                HomeAnnouncementBackground.ModeDefault,
                LanguageService.GetString("official_publisher_background_default", "Solid color")),
            new AnnouncementEditorChoice(
                HomeAnnouncementBackground.ModeImage,
                LanguageService.GetString("official_publisher_background_image", "Custom image"))
        };

        partial void OnSelectedAnnouncementsLanguageChanged(string value)
        {
            AnnouncementsLanguageContext.ContentLanguage = value;
            foreach (var item in AnnouncementEntries)
                item.RefreshDisplay();
        }

        /// <summary>
        /// 从 CDN 拉取当前线上的公告目录填充编辑器（编辑器始终以线上版本为基准）。
        /// </summary>
        [RelayCommand]
        private async Task LoadAnnouncementsEditorFromServerAsync()
        {
            try
            {
                bool known = false;
                HomeAnnouncementCatalog? catalog = null;
                try
                {
                    string json = await OfficialContentMirrorClient.GetStringAsync(OfficialContentEndpoints.AnnouncementsFileName);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        known = true;
                        catalog = new HomeAnnouncementCatalog();
                    }
                    else
                    {
                        catalog = JsonHelper.Deserialize<HomeAnnouncementCatalog>(json);
                        known = catalog != null;
                    }
                }
                catch (Exception ex)
                {
                    Log($"Announcements.json download failed: {ex.Message}");
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ReplaceAnnouncementEntries(catalog ?? new HomeAnnouncementCatalog());
                    RememberRemoteAnnouncementsCatalog(known ? catalog : null, known);
                    UpdateServerConfigChangeState();
                    Log(!known
                        ? "多语言公告目录下载失败，暂不能判断是否与服务器一致。"
                        : catalog?.Announcements == null || catalog.Announcements.Count == 0
                            ? "服务器暂无多语言公告目录（Announcements.json），可新增公告后发布。"
                            : $"已从服务器加载多语言公告目录：{AnnouncementEntries.Count} 条。");
                });
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        /// <summary>
        /// 从本地 JSON 导入公告目录。文件须为公告目录对象，且包含 announcements 数组。
        /// </summary>
        [RelayCommand]
        private async Task ImportAnnouncementsCatalogAsync()
        {
            string? path = FileHelper.GetFilePath(FileDialogFilterHelper.JsonOnly, OwnerWindow);
            if (string.IsNullOrWhiteSpace(path))
                return;

            string? json = JsonHelper.ReadJsonFile(path);
            if (!TryParseAnnouncementCatalog(json, out var catalog))
            {
                string invalid = LanguageService.GetString(
                    "official_publisher_announcement_import_invalid",
                    "This file is not a valid announcement catalog. It must be a JSON object with an announcements array.");
                ShowWarning(invalid);
                Log(invalid);
                return;
            }

            if (AnnouncementEntries.Count > 0)
            {
                bool confirmed = await ShowConfirmAsync(
                    LanguageService.GetString(
                        "official_publisher_announcement_import_confirm",
                        "Import replaces the announcements in the editor. Unpublished edits will be lost."),
                    LanguageService.Instance["Cancel"] ?? "Cancel",
                    LanguageService.Instance["Confirm"] ?? "Confirm");
                if (!confirmed)
                    return;
            }

            ReplaceAnnouncementEntries(catalog!);
            UpdateServerConfigChangeState();
            Log(string.Format(
                LanguageService.GetString(
                    "official_publisher_announcement_import_loaded",
                    "Imported announcement catalog: {0} entries."),
                AnnouncementEntries.Count));
        }

        [RelayCommand]
        private void AddAnnouncementEntry()
        {
            var entry = new HomeAnnouncementEntry
            {
                Id = CreateAnnouncementId(),
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Theme = "blue",
                BackgroundMode = HomeAnnouncementBackground.ModeDefault,
                ShowPolygons = true,
                Action = new HomeAnnouncementAction()
            };

            var item = new AnnouncementEditorItemViewModel(entry, AnnouncementsLanguageContext);
            AnnouncementEntries.Insert(0, item);
            SelectedAnnouncementEntry = item;
        }

        [RelayCommand]
        private void RemoveAnnouncementEntry()
        {
            if (SelectedAnnouncementEntry == null)
                return;

            int index = AnnouncementEntries.IndexOf(SelectedAnnouncementEntry);
            AnnouncementEntries.Remove(SelectedAnnouncementEntry);
            SelectedAnnouncementEntry = AnnouncementEntries.Count > 0
                ? AnnouncementEntries[Math.Clamp(index, 0, AnnouncementEntries.Count - 1)]
                : null;
        }

        /// <summary>
        /// 把编辑器内容整理成待发布的公告目录：过滤无标题条目、丢弃空动作。
        /// </summary>
        internal HomeAnnouncementCatalog BuildAnnouncementsCatalogFromEditor()
        {
            var catalog = new HomeAnnouncementCatalog { Version = 1 };

            foreach (var item in AnnouncementEntries)
            {
                var entry = item.Entry;
                if (!HomeLinksLocalization.HasText(entry.Title))
                    continue;

                if (string.IsNullOrWhiteSpace(entry.Id))
                    entry.Id = CreateAnnouncementId();

                entry.BackgroundMode = HomeAnnouncementBackground.NormalizeMode(entry.BackgroundMode);
                entry.PolygonStyle = HomeAnnouncementBackground.NormalizePolygonStyle(entry.PolygonStyle);
                entry.BackgroundColor = entry.BackgroundColor?.Trim() ?? string.Empty;
                entry.BackgroundImageUrl = entry.BackgroundImageUrl?.Trim() ?? string.Empty;

                if (entry.Action != null
                    && string.IsNullOrWhiteSpace(entry.Action.Url)
                    && !HomeLinksLocalization.HasText(entry.Action.Label))
                {
                    entry.Action = null;
                }

                catalog.Announcements.Add(entry);
            }

            return catalog;
        }

        private void ReplaceAnnouncementEntries(HomeAnnouncementCatalog catalog)
        {
            _suppressAnnouncementCatalogTracking = true;
            try
            {
                AnnouncementEntries.Clear();
                if (catalog.Announcements != null)
                {
                    foreach (var entry in catalog.Announcements)
                    {
                        if (entry == null)
                            continue;

                        AnnouncementEntries.Add(new AnnouncementEditorItemViewModel(entry, AnnouncementsLanguageContext));
                    }
                }

                SelectedAnnouncementEntry = AnnouncementEntries.FirstOrDefault();
            }
            finally
            {
                _suppressAnnouncementCatalogTracking = false;
            }
        }

        private static bool TryParseAnnouncementCatalog(string? json, out HomeAnnouncementCatalog? catalog)
        {
            catalog = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || !TryGetPropertyIgnoreCase(document.RootElement, "announcements", out var announcements)
                    || announcements.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }
            }
            catch (JsonException)
            {
                return false;
            }

            catalog = JsonHelper.Deserialize<HomeAnnouncementCatalog>(json);
            return catalog?.Announcements != null;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private void OnAnnouncementEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (AnnouncementEditorItemViewModel item in e.OldItems)
                    item.PropertyChanged -= OnAnnouncementEntryPropertyChanged;
            }

            if (e.NewItems != null)
            {
                foreach (AnnouncementEditorItemViewModel item in e.NewItems)
                    item.PropertyChanged += OnAnnouncementEntryPropertyChanged;
            }

            if (!_suppressAnnouncementCatalogTracking)
                UpdateServerConfigChangeState();
        }

        private void OnAnnouncementEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_suppressAnnouncementCatalogTracking)
                return;

            if (e.PropertyName is nameof(AnnouncementEditorItemViewModel.DisplayTitle)
                or nameof(AnnouncementEditorItemViewModel.BackgroundSummary)
                or nameof(AnnouncementEditorItemViewModel.IsDefaultBackground)
                or nameof(AnnouncementEditorItemViewModel.IsImageBackground)
                or nameof(AnnouncementEditorItemViewModel.PolygonStyleOptions))
                return;

            UpdateServerConfigChangeState();
        }

        private bool AnnouncementsCatalogHasRemoteChanges()
        {
            if (!_announcementsCatalogRemoteKnown)
                return true;

            return !string.Equals(
                FingerprintAnnouncements(AnnouncementEntries.Select(item => item.Entry)),
                _remoteAnnouncementsFingerprint,
                StringComparison.Ordinal);
        }

        private void RememberRemoteAnnouncementsCatalog(HomeAnnouncementCatalog? catalog, bool known)
        {
            _announcementsCatalogRemoteKnown = known;
            _remoteAnnouncementsFingerprint = FingerprintAnnouncements(catalog?.Announcements);
        }

        private string CreateAnnouncementId()
        {
            var used = new HashSet<string>(
                AnnouncementEntries.Select(item => item.Id),
                StringComparer.OrdinalIgnoreCase);

            string id;
            do
            {
                id = $"ann-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
            }
            while (!used.Add(id));

            return id;
        }

        private List<string> CollectAnnouncementPublishWarnings()
        {
            var warnings = new List<string>();
            int untitledCount = AnnouncementEntries.Count(item => !HomeLinksLocalization.HasText(item.Entry.Title));
            if (untitledCount > 0)
                warnings.Add($"有 {untitledCount} 条公告没有标题，发布时会丢弃。");

            foreach (var item in AnnouncementEntries)
            {
                if (!HomeLinksLocalization.HasText(item.Entry.Title))
                    continue;

                bool hasLabel = HomeLinksLocalization.HasText(item.Entry.Action?.Label);
                bool hasUrl = !string.IsNullOrWhiteSpace(item.Entry.Action?.Url);
                if (hasLabel && !hasUrl)
                    warnings.Add($"公告「{item.DisplayTitle}」填写了按钮文字但没有链接，按钮不会显示。");
            }

            return warnings;
        }

        private void ReportAnnouncementPublishWarnings(ICollection<string>? previewLines = null)
        {
            var warnings = CollectAnnouncementPublishWarnings();
            if (warnings.Count == 0)
                return;

            foreach (var warning in warnings)
            {
                previewLines?.Add(warning);
                Log(warning);
            }

            ShowWarning(string.Join(Environment.NewLine, warnings));
        }

        private static string FingerprintAnnouncements(IEnumerable<HomeAnnouncementEntry>? entries)
        {
            if (entries == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                sb.Append(entry.Id?.Trim() ?? string.Empty).Append('\u001f');
                sb.Append(FingerprintLocalized(entry.Tag)).Append('\u001f');
                sb.Append(FingerprintLocalized(entry.Title)).Append('\u001f');
                sb.Append(FingerprintLocalized(entry.Body)).Append('\u001f');
                sb.Append(entry.Date?.Trim() ?? string.Empty).Append('\u001f');
                sb.Append(entry.StartDate?.Trim() ?? string.Empty).Append('\u001f');
                sb.Append(entry.EndDate?.Trim() ?? string.Empty).Append('\u001f');
                sb.Append(entry.Priority).Append('\u001f');
                sb.Append(string.IsNullOrWhiteSpace(entry.Theme) ? "blue" : entry.Theme.Trim()).Append('\u001f');
                sb.Append(HomeAnnouncementBackground.NormalizeMode(entry.BackgroundMode)).Append('\u001f');
                sb.Append(entry.BackgroundColor?.Trim() ?? string.Empty).Append('\u001f');
                sb.Append(entry.ShowPolygons ? '1' : '0').Append('\u001f');
                sb.Append(HomeAnnouncementBackground.NormalizePolygonStyle(entry.PolygonStyle)).Append('\u001f');
                sb.Append(entry.BackgroundImageUrl?.Trim() ?? string.Empty).Append('\u001f');
                AppendActionFingerprint(sb, entry.Action);
                sb.Append('\u001e');
            }

            return sb.ToString();
        }

        private static void AppendActionFingerprint(StringBuilder sb, HomeAnnouncementAction? action)
        {
            bool hasLabel = HomeLinksLocalization.HasText(action?.Label);
            bool hasUrl = !string.IsNullOrWhiteSpace(action?.Url);
            if (!hasLabel && !hasUrl)
            {
                sb.Append('-');
                return;
            }

            sb.Append(action!.Url?.Trim() ?? string.Empty)
                .Append('|')
                .Append(FingerprintLocalized(action.Label));
        }

        private static string FingerprintLocalized(LocalizedString? value)
        {
            if (value == null)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append(value.Default ?? string.Empty);
            if (value.Translations == null)
                return sb.ToString();

            foreach (var pair in value.Translations.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                sb.Append('\u001f').Append(pair.Key).Append('=').Append(pair.Value ?? string.Empty);

            return sb.ToString();
        }
    }

    /// <summary>
    /// 公告编辑器条目：包装 <see cref="HomeAnnouncementEntry"/> 供数据绑定，
    /// LocalizedString 属性写回底层模型（每次编辑会生成新的 LocalizedString 实例）。
    /// </summary>
    public partial class AnnouncementEditorItemViewModel : ObservableObject
    {
        private readonly ContentLanguageContext _languageContext;

        public HomeAnnouncementEntry Entry { get; }

        public AnnouncementEditorItemViewModel(HomeAnnouncementEntry entry, ContentLanguageContext languageContext)
        {
            Entry = entry;
            _languageContext = languageContext;
            Entry.Action ??= new HomeAnnouncementAction();
        }

        public string Id
        {
            get => Entry.Id;
            set { Entry.Id = value?.Trim() ?? string.Empty; OnPropertyChanged(); }
        }

        public LocalizedString Tag
        {
            get => Entry.Tag;
            set { Entry.Tag = value ?? new LocalizedString(); OnPropertyChanged(); }
        }

        public LocalizedString Title
        {
            get => Entry.Title;
            set { Entry.Title = value ?? new LocalizedString(); OnPropertyChanged(); RefreshDisplay(); }
        }

        public LocalizedString Body
        {
            get => Entry.Body;
            set { Entry.Body = value ?? new LocalizedString(); OnPropertyChanged(); }
        }

        public string Date
        {
            get => Entry.Date;
            set { Entry.Date = value?.Trim() ?? string.Empty; OnPropertyChanged(); RefreshDisplay(); }
        }

        public string StartDate
        {
            get => Entry.StartDate;
            set { Entry.StartDate = value?.Trim() ?? string.Empty; OnPropertyChanged(); }
        }

        public string EndDate
        {
            get => Entry.EndDate;
            set { Entry.EndDate = value?.Trim() ?? string.Empty; OnPropertyChanged(); }
        }

        public int Priority
        {
            get => Entry.Priority;
            set { Entry.Priority = value; OnPropertyChanged(); }
        }

        public string Theme
        {
            get => Entry.Theme;
            set
            {
                Entry.Theme = string.IsNullOrWhiteSpace(value) ? "blue" : value.Trim();
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackgroundSummary));
            }
        }

        public string BackgroundMode
        {
            get => HomeAnnouncementBackground.NormalizeMode(Entry.BackgroundMode);
            set
            {
                Entry.BackgroundMode = HomeAnnouncementBackground.NormalizeMode(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDefaultBackground));
                OnPropertyChanged(nameof(IsImageBackground));
                OnPropertyChanged(nameof(BackgroundSummary));
            }
        }

        public bool IsDefaultBackground => !IsImageBackground;

        public bool IsImageBackground => HomeAnnouncementBackground.IsImageMode(Entry.BackgroundMode);

        public string BackgroundColor
        {
            get => Entry.BackgroundColor ?? string.Empty;
            set
            {
                Entry.BackgroundColor = value?.Trim() ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackgroundSummary));
            }
        }

        public bool ShowPolygons
        {
            get => Entry.ShowPolygons;
            set
            {
                Entry.ShowPolygons = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackgroundSummary));
            }
        }

        public string PolygonStyle
        {
            get
            {
                string stored = Entry.PolygonStyle?.Trim() ?? string.Empty;
                if (stored.Length == 0
                    || string.Equals(stored, HomeAnnouncementBackground.PolygonStyleAuto, StringComparison.OrdinalIgnoreCase))
                    return HomeAnnouncementBackground.PolygonStyleAuto;

                if (HomeAnnouncementBackground.TryGetKnownPolygonStyle(stored, out string canonical))
                    return canonical;

                return stored;
            }
            set
            {
                string previous = Entry.PolygonStyle?.Trim() ?? string.Empty;
                bool previousUnknown = previous.Length > 0 && !HomeAnnouncementBackground.IsKnownPolygonStyle(previous);
                Entry.PolygonStyle = HomeAnnouncementBackground.NormalizePolygonStyle(value);
                string next = Entry.PolygonStyle?.Trim() ?? string.Empty;
                bool nextUnknown = next.Length > 0 && !HomeAnnouncementBackground.IsKnownPolygonStyle(next);
                OnPropertyChanged();
                if (previousUnknown != nextUnknown)
                    OnPropertyChanged(nameof(PolygonStyleOptions));
                OnPropertyChanged(nameof(BackgroundSummary));
            }
        }

        /// <summary>
        /// 下拉只列出本版本能画的样式。目录里已有的未知样式额外保留一项，避免保存时被改掉。
        /// </summary>
        public IReadOnlyList<AnnouncementEditorChoice> PolygonStyleOptions
        {
            get
            {
                var options = new List<AnnouncementEditorChoice>
                {
                    new(HomeAnnouncementBackground.PolygonStyleAuto, "自动")
                };

                foreach (var style in HomeAnnouncementBackground.KnownPolygonStyles)
                    options.Add(new AnnouncementEditorChoice(style.Id, style.Label));

                string stored = Entry.PolygonStyle?.Trim() ?? string.Empty;
                if (stored.Length > 0 && !HomeAnnouncementBackground.IsKnownPolygonStyle(stored))
                    options.Add(new AnnouncementEditorChoice(stored, $"保留未知样式（{stored}）"));

                return options;
            }
        }

        public string BackgroundImageUrl
        {
            get => Entry.BackgroundImageUrl ?? string.Empty;
            set
            {
                Entry.BackgroundImageUrl = value?.Trim() ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackgroundSummary));
            }
        }

        public string BackgroundSummary
        {
            get
            {
                if (IsImageBackground)
                    return "图片背景";

                string color = string.IsNullOrWhiteSpace(BackgroundColor) ? Theme : BackgroundColor;
                return ShowPolygons ? $"{color} · 多边形" : color;
            }
        }

        public LocalizedString ActionLabel
        {
            get => Entry.Action?.Label ?? new LocalizedString();
            set
            {
                Entry.Action ??= new HomeAnnouncementAction();
                Entry.Action.Label = value ?? new LocalizedString();
                OnPropertyChanged();
            }
        }

        public string ActionUrl
        {
            get => Entry.Action?.Url ?? string.Empty;
            set
            {
                Entry.Action ??= new HomeAnnouncementAction();
                Entry.Action.Url = value?.Trim() ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string DisplayTitle
        {
            get
            {
                string title = Entry.Title?.Get(_languageContext) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(title))
                    title = "(未填写标题)";
                return string.IsNullOrWhiteSpace(Entry.Date) ? title : $"{title}  ·  {Entry.Date}";
            }
        }

        public void RefreshDisplay() => OnPropertyChanged(nameof(DisplayTitle));
    }

    /// <summary>
    /// 发布器下拉项：存短值，界面显示中文。
    /// </summary>
    public sealed class AnnouncementEditorChoice
    {
        public AnnouncementEditorChoice(string value, string label)
        {
            Value = value;
            Label = label;
        }

        public string Value { get; }

        public string Label { get; }
    }
}
