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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        // 主页里 blue（或留空）表示公告配色跟随软件当前主题色，并不是固定的蓝色
        internal static string ThemeLabel(string key) => key switch
        {
            "blue" => "默认（跟随软件主题色）",
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
            new AnnouncementEditorChoice(HomeAnnouncementBackground.ModeDefault, "颜色"),
            new AnnouncementEditorChoice(HomeAnnouncementBackground.ModeImage, "图片")
        };

        /// <summary>预览时用来切换“主页当前语言”的选项。</summary>
        public IReadOnlyList<CultureOption> AnnouncementPreviewLanguageOptions { get; } = AppCultureRegistry.GetOptions(false);

        public ObservableCollection<HomeAnnouncementDisplayViewModel> AnnouncementPreviewItems { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AnnouncementPreviewButtonText))]
        private bool isAnnouncementPreview;

        [ObservableProperty]
        private string announcementPreviewLanguage = AppCultureRegistry.DefaultContentLanguage;

        [ObservableProperty]
        private HomeAnnouncementDisplayViewModel? currentAnnouncementPreview;

        [ObservableProperty]
        private bool hasAnnouncementPreview;

        [ObservableProperty]
        private bool hasMultipleAnnouncementPreviews;

        [ObservableProperty]
        private string announcementPreviewCounterText = string.Empty;

        public string AnnouncementPreviewButtonText => IsAnnouncementPreview ? "返回编辑" : "预览";

        private int _announcementPreviewIndex;

        private CancellationTokenSource? _announcementPreviewImageCts;

        partial void OnSelectedAnnouncementsLanguageChanged(string value)
        {
            AnnouncementsLanguageContext.ContentLanguage = value;
            foreach (var item in AnnouncementEntries)
                item.RefreshDisplay();
        }

        partial void OnIsAnnouncementPreviewChanged(bool value)
        {
            if (value)
                RebuildAnnouncementPreview();
            else
                CancelAnnouncementPreviewImages();
        }

        partial void OnAnnouncementPreviewLanguageChanged(string value)
        {
            if (IsAnnouncementPreview && !string.IsNullOrWhiteSpace(value))
                RebuildAnnouncementPreview();
        }

        partial void OnCurrentAnnouncementPreviewChanged(HomeAnnouncementDisplayViewModel? value)
        {
            for (int i = 0; i < AnnouncementPreviewItems.Count; i++)
                AnnouncementPreviewItems[i].IsCurrent = ReferenceEquals(AnnouncementPreviewItems[i], value);

            if (value == null || AnnouncementPreviewItems.Count == 0)
            {
                AnnouncementPreviewCounterText = string.Empty;
                return;
            }

            int index = AnnouncementPreviewItems.IndexOf(value);
            if (index >= 0)
                _announcementPreviewIndex = index;

            AnnouncementPreviewCounterText = $"{_announcementPreviewIndex + 1} / {AnnouncementPreviewItems.Count}";
        }

        [RelayCommand]
        private void ToggleAnnouncementPreview()
        {
            IsAnnouncementPreview = !IsAnnouncementPreview;
        }

        [RelayCommand]
        private void ShowPreviousAnnouncementPreview()
            => ShowAnnouncementPreviewAt(_announcementPreviewIndex - 1);

        [RelayCommand]
        private void ShowNextAnnouncementPreview()
            => ShowAnnouncementPreviewAt(_announcementPreviewIndex + 1);

        [RelayCommand]
        private void OpenAnnouncementPreviewAction(HomeAnnouncementDisplayViewModel? item)
        {
            string? url = item?.ActionUrl;
            if (string.IsNullOrWhiteSpace(url))
                return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowWarning("OpenBrowserError: " + ex.Message);
            }
        }

        /// <summary>
        /// 把当前编辑器里的公告目录导出为 Announcements.json。
        /// </summary>
        [RelayCommand]
        private async Task ExportAnnouncementsCatalog()
        {
            string? path = await FileHelper.GetSaveFilePath2Async(
                "导出公告目录",
                FileDialogFilterHelper.JsonOnly,
                ".json",
                OfficialContentEndpoints.AnnouncementsFileName,
                OwnerWindow);
            if (string.IsNullOrWhiteSpace(path))
                return;

            var catalog = BuildAnnouncementsCatalogFromEditor();
            var options = JsonHelper.CreateRelaxedIndentedOptions(new LocalizedStringJsonConverter());
            File.WriteAllText(path, JsonSerializer.Serialize(catalog, options));
            Log("已导出公告目录");
            ShowSuccess("已导出公告目录");
        }

        /// <summary>
        /// 按预览语言重建主页公告卡片：只保留该语言会看到、并且今天在上架时间内的公告。
        /// </summary>
        private void RebuildAnnouncementPreview()
        {
            string? previousId = CurrentAnnouncementPreview?.Id;
            CancelAnnouncementPreviewImages();
            CurrentAnnouncementPreview = null;

            string language = AppCultureRegistry.ResolveAppLanguage(AnnouncementPreviewLanguage);
            var context = new ContentLanguageContext { ContentLanguage = language };
            var now = DateTime.Now;
            var displays = AnnouncementEntries
                .Select(item => item.Entry)
                .Where(entry => entry.IsVisibleForLanguage(language)
                    && entry.IsActive(now)
                    && HomeLinksLocalization.HasText(entry.Title))
                .OrderByDescending(entry => entry.Priority)
                .ThenByDescending(entry => entry.Date ?? string.Empty, StringComparer.Ordinal)
                .Select(entry => HomeAnnouncementDisplayViewModel.FromEntry(
                    entry,
                    value => HomeLinksLocalization.ResolveForContext(value, context)))
                .ToList();

            AnnouncementPreviewItems.Clear();
            foreach (var display in displays)
                AnnouncementPreviewItems.Add(display);

            HasAnnouncementPreview = AnnouncementPreviewItems.Count > 0;
            HasMultipleAnnouncementPreviews = AnnouncementPreviewItems.Count > 1;

            int index = 0;
            if (!string.IsNullOrEmpty(previousId))
            {
                int found = displays.FindIndex(item => item.Id == previousId);
                if (found >= 0)
                    index = found;
            }

            ShowAnnouncementPreviewAt(index);
            _ = LoadAnnouncementPreviewImagesAsync();
        }

        private void ShowAnnouncementPreviewAt(int index)
        {
            if (AnnouncementPreviewItems.Count == 0)
            {
                _announcementPreviewIndex = 0;
                CurrentAnnouncementPreview = null;
                AnnouncementPreviewCounterText = string.Empty;
                return;
            }

            int count = AnnouncementPreviewItems.Count;
            int next = ((index % count) + count) % count;
            _announcementPreviewIndex = next;
            CurrentAnnouncementPreview = AnnouncementPreviewItems[next];
        }

        private async Task LoadAnnouncementPreviewImagesAsync()
        {
            var cts = new CancellationTokenSource();
            _announcementPreviewImageCts = cts;
            var pending = AnnouncementPreviewItems.Where(item => item.UseImageBackground).ToList();
            if (pending.Count == 0)
                return;

            try
            {
                await Task.WhenAll(pending.Select(item => item.LoadBackgroundImageAsync(int.MaxValue, cts.Token)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnnouncementPreview] images failed: {ex.Message}");
            }
        }

        private void CancelAnnouncementPreviewImages()
        {
            _announcementPreviewImageCts?.Cancel();
            _announcementPreviewImageCts = null;
        }

        /// <summary>
        /// 从 CDN 拉取当前线上的公告目录填充编辑器（编辑器始终以线上版本为基准）。
        /// </summary>
        private async Task LoadAnnouncementsCatalogFromServerCoreAsync()
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
                    // 下载失败时保留编辑器里已有的内容，不要用空目录覆盖掉
                    if (known || AnnouncementEntries.Count == 0)
                        ReplaceAnnouncementEntries(catalog ?? new HomeAnnouncementCatalog());
                    if (known || !_announcementsCatalogRemoteKnown)
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
            PublishAnnouncement = true;
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
                Action = new HomeAnnouncementAction()
            };

            var item = new AnnouncementEditorItemViewModel(entry, AnnouncementsLanguageContext);
            AnnouncementEntries.Insert(0, item);
            SelectedAnnouncementEntry = item;
        }

        /// <summary>
        /// 以当前选中的公告为模板新建一条：文案和样式照搬，编号、日期重新生成，上下架时间清空。
        /// </summary>
        [RelayCommand]
        private void DuplicateAnnouncementEntry()
        {
            if (SelectedAnnouncementEntry == null)
                return;

            var options = JsonHelper.CreateRelaxedIndentedOptions(new LocalizedStringJsonConverter());
            string json = JsonSerializer.Serialize(SelectedAnnouncementEntry.Entry, options);
            var copy = JsonSerializer.Deserialize<HomeAnnouncementEntry>(json, options);
            if (copy == null)
                return;

            copy.Id = CreateAnnouncementId();
            copy.Date = DateTime.Now.ToString("yyyy-MM-dd");
            copy.StartDate = string.Empty;
            copy.EndDate = string.Empty;

            var item = new AnnouncementEditorItemViewModel(copy, AnnouncementsLanguageContext);
            AnnouncementEntries.Insert(0, item);
            SelectedAnnouncementEntry = item;
        }

        /// <summary>
        /// 编辑区域上方的实时预览：始终显示当前选中的这一条，按当前编辑语言解析，不受上下架时间影响。
        /// </summary>
        [ObservableProperty]
        private HomeAnnouncementDisplayViewModel? selectedAnnouncementLivePreview;

        public bool HasSelectedAnnouncementEntry => SelectedAnnouncementEntry != null;

        private CancellationTokenSource? _livePreviewImageCts;

        partial void OnSelectedAnnouncementEntryChanged(AnnouncementEditorItemViewModel? value)
        {
            OnPropertyChanged(nameof(HasSelectedAnnouncementEntry));
            RefreshAnnouncementTranslationChips();
            RebuildAnnouncementLivePreview();
        }

        private void RebuildAnnouncementLivePreview()
        {
            _livePreviewImageCts?.Cancel();
            _livePreviewImageCts = null;

            var entry = SelectedAnnouncementEntry?.Entry;
            if (entry == null)
            {
                SelectedAnnouncementLivePreview = null;
                return;
            }

            var context = new ContentLanguageContext { ContentLanguage = SelectedEditLanguage };
            var display = HomeAnnouncementDisplayViewModel.FromEntry(
                entry,
                value => HomeLinksLocalization.ResolveForContext(value, context));

            // 图片地址没变时沿用已下载的图片，避免每敲一个字都重新下载
            var previous = SelectedAnnouncementLivePreview;
            if (display.UseImageBackground
                && previous?.BackgroundImage != null
                && string.Equals(previous.BackgroundImageUrl, display.BackgroundImageUrl, StringComparison.Ordinal))
            {
                display.BackgroundImage = previous.BackgroundImage;
            }

            SelectedAnnouncementLivePreview = display;
            if (display.UseImageBackground && display.BackgroundImage == null)
                _ = LoadLivePreviewImageAsync(display);
        }

        private async Task LoadLivePreviewImageAsync(HomeAnnouncementDisplayViewModel display)
        {
            var cts = new CancellationTokenSource();
            _livePreviewImageCts = cts;
            try
            {
                // 稍等一下，地址还在输入时不要逐字发请求
                await Task.Delay(500, cts.Token);
                await display.LoadBackgroundImageAsync(int.MaxValue, cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnnouncementLivePreview] image failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 当前选中公告在每种语言下的填写情况：标题（和正文，如果写过）都有为“完整”，只有一部分为“缺失”，都没有为“空”。
        /// </summary>
        public ObservableCollection<TranslationChipViewModel> AnnouncementTranslationChips { get; } =
            new(AppCultureRegistry.SupportedCodes.Select(code =>
                new TranslationChipViewModel(code, AppCultureRegistry.GetDisplayName(code, false))));

        private void RefreshAnnouncementTranslationChips()
        {
            var entry = SelectedAnnouncementEntry?.Entry;
            bool bodyNeeded = entry?.Body?.Translations?.Values.Any(text => !string.IsNullOrWhiteSpace(text)) ?? false;

            foreach (var chip in AnnouncementTranslationChips)
            {
                bool hasTitle = HasTranslation(entry?.Title, chip.Code);
                bool hasBody = HasTranslation(entry?.Body, chip.Code);

                chip.State = entry == null
                    ? TranslationChipViewModel.StateEmpty
                    : hasTitle && (!bodyNeeded || hasBody)
                        ? TranslationChipViewModel.StateFull
                        : hasTitle || hasBody
                            ? TranslationChipViewModel.StatePartial
                            : TranslationChipViewModel.StateEmpty;
                chip.IsCurrent = string.Equals(chip.Code, SelectedEditLanguage, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool HasTranslation(LocalizedString? value, string code)
        {
            if (value?.Translations == null)
                return false;

            return value.Translations.Any(pair =>
                string.Equals(pair.Key, code, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(pair.Value));
        }

        [RelayCommand]
        private void SelectEditLanguage(string? code)
        {
            if (!string.IsNullOrWhiteSpace(code))
                SelectedEditLanguage = code;
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
                var languages = HomeAnnouncementLanguages.Normalize(entry.Languages);
                entry.Languages = languages.Count == 0 ? null : languages;
                StripRetiredAnnouncementFields(entry);
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

                        StripRetiredAnnouncementFields(entry);
                        AnnouncementEntries.Add(new AnnouncementEditorItemViewModel(entry, AnnouncementsLanguageContext));
                    }
                }

                SelectedAnnouncementEntry = AnnouncementEntries.FirstOrDefault();
            }
            finally
            {
                _suppressAnnouncementCatalogTracking = false;
            }

            if (IsAnnouncementPreview)
                RebuildAnnouncementPreview();
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
            {
                // 增删公告也算编辑，自动把“公告与版本”纳入本次发布
                PublishAnnouncement = true;
                UpdateServerConfigChangeState();
            }

            if (IsAnnouncementPreview && !_suppressAnnouncementCatalogTracking)
                RebuildAnnouncementPreview();
        }

        private void OnAnnouncementEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_suppressAnnouncementCatalogTracking)
                return;

            if (e.PropertyName is nameof(AnnouncementEditorItemViewModel.DisplayTitle)
                or nameof(AnnouncementEditorItemViewModel.BackgroundSummary)
                or nameof(AnnouncementEditorItemViewModel.LanguageSummary)
                or nameof(AnnouncementEditorItemViewModel.IsDefaultBackground)
                or nameof(AnnouncementEditorItemViewModel.IsImageBackground)
                or nameof(AnnouncementEditorItemViewModel.StatusKey)
                or nameof(AnnouncementEditorItemViewModel.StatusText))
                return;

            if (ReferenceEquals(sender, SelectedAnnouncementEntry))
            {
                if (e.PropertyName is nameof(AnnouncementEditorItemViewModel.Title)
                    or nameof(AnnouncementEditorItemViewModel.Body))
                {
                    RefreshAnnouncementTranslationChips();
                }

                RebuildAnnouncementLivePreview();
            }

            PublishAnnouncement = true;
            UpdateServerConfigChangeState();
            if (IsAnnouncementPreview)
                RebuildAnnouncementPreview();
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

        /// <summary>
        /// 主页不再读取装饰图形开关和样式。旧目录里的这两个字段在载入和发布时丢掉。
        /// </summary>
        private static void StripRetiredAnnouncementFields(HomeAnnouncementEntry entry)
        {
            if (entry.ExtensionData == null || entry.ExtensionData.Count == 0)
                return;

            foreach (var key in entry.ExtensionData.Keys.ToList())
            {
                if (key.Equals("showPolygons", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("polygonStyle", StringComparison.OrdinalIgnoreCase))
                    entry.ExtensionData.Remove(key);
            }
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
                sb.Append(string.Join(",", HomeAnnouncementLanguages.Normalize(entry.Languages))).Append('\u001f');
                sb.Append(string.IsNullOrWhiteSpace(entry.Theme) ? "blue" : entry.Theme.Trim()).Append('\u001f');
                sb.Append(HomeAnnouncementBackground.NormalizeMode(entry.BackgroundMode)).Append('\u001f');
                sb.Append(entry.BackgroundColor?.Trim() ?? string.Empty).Append('\u001f');
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

        public ObservableCollection<AnnouncementLanguageChoice> LanguageChoices { get; }

        public AnnouncementEditorItemViewModel(HomeAnnouncementEntry entry, ContentLanguageContext languageContext)
        {
            Entry = entry;
            _languageContext = languageContext;
            Entry.Action ??= new HomeAnnouncementAction();

            var selected = new HashSet<string>(
                HomeAnnouncementLanguages.Normalize(entry.Languages),
                StringComparer.OrdinalIgnoreCase);
            LanguageChoices = new ObservableCollection<AnnouncementLanguageChoice>(
                AppCultureRegistry.SupportedCodes.Select(code =>
                    new AnnouncementLanguageChoice(
                        code,
                        AppCultureRegistry.GetDisplayName(code, false),
                        selected.Contains(code),
                        OnLanguageChoiceChanged)));
        }

        private void OnLanguageChoiceChanged()
        {
            var selected = LanguageChoices.Where(choice => choice.IsSelected).Select(choice => choice.Code).ToList();
            Entry.Languages = selected.Count == 0 ? null : selected;
            OnPropertyChanged(nameof(LanguageSummary));
            OnPropertyChanged(nameof(Languages));
        }

        /// <summary>已选择的展示语言。空列表表示所有语言。</summary>
        public IReadOnlyList<string> Languages => HomeAnnouncementLanguages.Normalize(Entry.Languages);

        public string LanguageSummary
        {
            get
            {
                var codes = HomeAnnouncementLanguages.Normalize(Entry.Languages);
                if (codes.Count == 0)
                    return "全部语言";

                return string.Join("、", codes.Select(code => AppCultureRegistry.GetDisplayName(code, false)));
            }
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
            set { Entry.Title = value ?? new LocalizedString(); OnPropertyChanged(); RefreshDisplay(); RefreshStatus(); }
        }

        /// <summary>上架状态键：live（展示中）/ scheduled（未上架）/ expired（已下架）/ empty（没有标题）。</summary>
        public string StatusKey
        {
            get
            {
                if (!HomeLinksLocalization.HasText(Entry.Title))
                    return "empty";

                DateTime today = DateTime.Now.Date;
                if (DateTime.TryParse(Entry.EndDate, out var end) && !string.IsNullOrWhiteSpace(Entry.EndDate) && today > end.Date)
                    return "expired";

                if (DateTime.TryParse(Entry.StartDate, out var start) && !string.IsNullOrWhiteSpace(Entry.StartDate) && today < start.Date)
                    return "scheduled";

                return "live";
            }
        }

        public string StatusText => StatusKey switch
        {
            "live" => "展示中",
            "scheduled" => "未上架",
            "expired" => "已下架",
            _ => "无标题"
        };

        private void RefreshStatus()
        {
            OnPropertyChanged(nameof(StatusKey));
            OnPropertyChanged(nameof(StatusText));
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
            set { Entry.StartDate = value?.Trim() ?? string.Empty; OnPropertyChanged(); RefreshStatus(); }
        }

        public string EndDate
        {
            get => Entry.EndDate;
            set { Entry.EndDate = value?.Trim() ?? string.Empty; OnPropertyChanged(); RefreshStatus(); }
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

                return string.IsNullOrWhiteSpace(BackgroundColor)
                    ? OfficialTemplatePublisherViewModel.ThemeLabel(Theme)
                    : BackgroundColor;
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
    /// 公告展示语言的勾选项。
    /// </summary>
    public sealed class AnnouncementLanguageChoice : ObservableObject
    {
        private readonly Action _onChanged;
        private bool _isSelected;

        public AnnouncementLanguageChoice(string code, string label, bool isSelected, Action onChanged)
        {
            Code = code;
            Label = label;
            _isSelected = isSelected;
            _onChanged = onChanged;
        }

        public string Code { get; }

        public string Label { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                OnPropertyChanged();
                _onChanged();
            }
        }
    }

    /// <summary>
    /// 语言填写情况标签：点击即可切换当前编辑语言。
    /// </summary>
    public sealed partial class TranslationChipViewModel : ObservableObject
    {
        public const string StateEmpty = "empty";
        public const string StatePartial = "partial";
        public const string StateFull = "full";

        public TranslationChipViewModel(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        public string Code { get; }

        public string DisplayName { get; }

        [ObservableProperty]
        private string state = StateEmpty;

        [ObservableProperty]
        private bool isCurrent;
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
