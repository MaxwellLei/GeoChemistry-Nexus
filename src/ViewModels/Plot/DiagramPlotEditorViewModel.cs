using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public enum DiagramPlotEditorMode
    {
        Create,
        Edit
    }

    public enum DiagramTernaryDirection
    {
        Clockwise,
        CounterClockwise
    }

    /// <summary>
    /// 图解模板编辑器。侧栏顺序：基本信息 → 多语言 → 数据与脚本 → 帮助文档。
    /// </summary>
    public partial class DiagramPlotEditorViewModel : ObservableObject, IRecipient<DeveloperModeChangedMessage>, IHelpDocEditorHost
    {
        public const int SectionBasic = 0;
        public const int SectionTranslation = 1;
        public const int SectionScript = 2;
        public const int SectionHelp = 3;

        /// <summary>导出成功提示使用的通知中心。图解编辑窗口打开时赋值。</summary>
        public NotificationCenter? ExportNotificationCenter { get; set; }

        [ObservableProperty]
        private DiagramPlotEditorMode _mode = DiagramPlotEditorMode.Create;

        [ObservableProperty]
        private string _windowTitle;

        [ObservableProperty]
        private int _selectedSectionIndex;

        [ObservableProperty]
        private bool _isPlotTypeSelectionEnabled = true;

        [ObservableProperty]
        private DataTable? _translationTable;

        /// <summary>多语言页：与 EPMA 多语言编辑窗同款的列头（默认语言在前）。</summary>
        public ObservableCollection<EpmaLocalizationColumn> TranslationColumns { get; } = new();

        /// <summary>多语言页表格行（字段项 + 各语言单元格）。</summary>
        public ObservableCollection<EpmaLocalizationTableRow> TranslationDisplayRows { get; } = new();

        public EpmaLocalizationColumn? TranslationDefaultColumn => TranslationColumns.FirstOrDefault();

        public IReadOnlyList<EpmaLocalizationColumn> TranslationOtherColumns =>
            TranslationColumns.Skip(1).ToList();

        public int TranslationLanguageCount => TranslationColumns.Count;

        public bool HasTranslationContent => TranslationDisplayRows.Count > 0;

        partial void OnTranslationTableChanged(DataTable? value) => RebuildTranslationDisplay();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasScriptStatus))]
        private string _scriptStatus = string.Empty;

        public bool HasScriptStatus => !string.IsNullOrWhiteSpace(ScriptStatus);

        [ObservableProperty]
        private bool _scriptHasError;

        [RelayCommand]
        private void TestScript()
        {
            var script = ScriptDefinition;
            var result = string.IsNullOrWhiteSpace(script?.ScriptBody)
                ? (Success: false, Message: LanguageService.Instance["script_content_is_empty"] ?? "Script is empty.")
                : ScriptSyntaxValidator.ValidateJavaScript($"(function() {{\n{script.ScriptBody}\n}})();", script.RequiredDataSeries);
            _draftNeedsValidation = false;
            ScriptHasError = !result.Success;
            ScriptStatus = result.Message;
            RefreshTemplateStatus();
        }

        [ObservableProperty]
        private string _translationStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isTranslationSectionEnabled;

        [ObservableProperty]
        private bool _isHelpSectionEnabled;

        [ObservableProperty]
        private bool _isScriptSectionEnabled;

        /// <summary>当前工作模板中的图解脚本定义。</summary>
        public ScriptDefinition? ScriptDefinition => _workingTemplate?.Script;

        /// <summary>数据与脚本页：RequiredDataSeries 拆成的列名（与 EPMA 输入列表格同款交互）。</summary>
        public ObservableCollection<DiagramDataSeriesColumn> DataSeriesColumns { get; } = new();

        private bool _suppressDataSeriesSync;

        [ObservableProperty]
        private string? _selectedHelpLanguage;

        private const int MaxContentPatch = 9999;

        [ObservableProperty]
        private int _patchVersion;

        /// <summary>语言代码 → RTF 帮助文档内容。</summary>
        private readonly Dictionary<string, string> _helpDocuments = new();

        /// <summary>
        /// 帮助文档 RichTextBox 是否已与当前语言内容同步。
        /// 未打开帮助文档分区时为 false，避免用空白编辑器覆盖已有文档。
        /// </summary>
        private bool _isHelpDocEditorSynced;

        /// <summary>由 View 设置：获取当前 RichTextBox 中的 RTF 内容。</summary>
        public Func<string?>? GetCurrentRtfContent { get; set; }

        /// <summary>由 View 设置：将 RTF 内容加载到 RichTextBox。</summary>
        public Action<string?>? SetCurrentRtfContent { get; set; }

        /// <summary>编辑模式下打开时的原始模板版本（只读展示）。</summary>
        [ObservableProperty]
        private string _originalTemplateVersion = string.Empty;

        public bool IsEditMode => Mode == DiagramPlotEditorMode.Edit;

        /// <summary>程序图解格式基线 x.y.（只读）</summary>
        public string FormatVersionPrefix
        {
            get
            {
                var normalized = ContentVersionHelper.Normalize(ContentVersionHelper.GetDiagramFormatVersion());
                var parts = normalized.Split('.');
                return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}." : "1.0.";
            }
        }

        /// <summary>程序图解格式基线 x.y，不含修订号。</summary>
        public string FormatVersionBaseline =>
            ContentVersionHelper.FormatBaseline(ContentVersionHelper.GetDiagramFormatVersion());

        /// <summary>保存后将写入的完整模板版本 x.y.z。</summary>
        public string TemplateVersion => ContentVersionHelper.WithAppDiagramFormat(PatchVersion);

        /// <summary>保存后版本的展示。与打开时的版本不同时为「原版本 -> 当前版本」。</summary>
        public string SavedVersionText => ContentVersionHelper.FormatSavedVersion(
            OriginalTemplateVersion,
            TemplateVersion);

        public ObservableCollection<LanguageTagModel> LanguageParts { get; } = new();

        [ObservableProperty]
        private IReadOnlyList<CultureOption> _appLanguageOptions = AppCultureRegistry.GetOptions();

        public ObservableCollection<CategoryPartModel> CategoryParts { get; } = new();
        public ObservableCollection<PlotTypeOption> PlotTypeOptions { get; } = new();

        /// <summary>分类结构预览 TreeView 的虚拟根节点。</summary>
        public GraphMapTemplateNode CategoryStructurePreviewRoot { get; } = new();

        public bool HasCategoryStructurePreview => CategoryParts.Count > 0;

        private PlotTemplateCategoryConfig? _categoryConfig;
        private GraphMapTemplate? _workingTemplate;
        private GraphMapTemplateEntity? _editEntity;
        private bool _suppressTranslationRebuild;

        /// <summary>加载模板时改写方向，不把已有图元当成用户切换来重算。</summary>
        private bool _suppressClockwiseRemap;

        /// <summary>批量加入语言时先挂起逐项刷新，结束后再统一重建。</summary>
        private bool _bulkLanguageUpdate;

        [ObservableProperty]
        private string _selectedPlotType = "2D_Plot";

        /// <summary>三元相图端元环绕方向。默认顺时针；二维图解不使用。</summary>
        [ObservableProperty]
        private bool _isClockwise = true;

        public DiagramTernaryDirection TernaryDirection
        {
            get => IsClockwise ? DiagramTernaryDirection.Clockwise : DiagramTernaryDirection.CounterClockwise;
            set => IsClockwise = value == DiagramTernaryDirection.Clockwise;
        }

        public bool IsTernaryPlot =>
            string.Equals(SelectedPlotType, "Ternary_Plot", StringComparison.Ordinal);

        partial void OnIsClockwiseChanged(bool value)
        {
            OnPropertyChanged(nameof(TernaryDirection));
            if (_suppressClockwiseRemap)
                return;

            if (_workingTemplate != null
                && string.Equals(_workingTemplate.TemplateType, "Ternary", StringComparison.Ordinal)
                && _workingTemplate.Clockwise != value)
            {
                RemapWorkingTemplateGeometry(_workingTemplate.Clockwise, value);
            }

            ApplyClockwiseToWorkingTemplate();
        }

        /// <summary>各端元方向下的几何快照；用于来回切换方向时无损还原，避免浮点换算误差累积。</summary>
        private readonly Dictionary<bool, string> _ternaryGeometrySnapshots = new();
        private GraphMapTemplate? _ternarySnapshotOwner;

        /// <summary>
        /// 切换端元方向时重算几何。顺时针 ↔ 逆时针的坐标换算存在浮点误差，
        /// 因此离开某方向前保存其快照，切回时直接还原，保证内容（及 FileHash）与原来完全一致。
        /// </summary>
        private void RemapWorkingTemplateGeometry(bool fromClockwise, bool toClockwise)
        {
            var template = _workingTemplate;
            if (template == null)
                return;

            if (!ReferenceEquals(_ternarySnapshotOwner, template))
            {
                _ternaryGeometrySnapshots.Clear();
                _ternarySnapshotOwner = template;
            }

            var options = GraphMapTemplateService.CreateTemplateJsonOptions();
            _ternaryGeometrySnapshots[fromClockwise] =
                System.Text.Json.JsonSerializer.Serialize(template.Info, options);

            if (_ternaryGeometrySnapshots.TryGetValue(toClockwise, out var snapshot))
            {
                var restored = System.Text.Json.JsonSerializer.Deserialize<GraphMapInfo>(snapshot, options);
                if (restored != null)
                {
                    template.Info = restored;
                    return;
                }
            }

            TernaryCoordinateHelper.RemapGeometry(template.Info, fromClockwise, toClockwise);
        }

        /// <summary>翻译表默认语言（与 LanguageParts 首项一致）。</summary>
        public string DefaultLanguageCode =>
            LanguageParts.FirstOrDefault()?.Text ?? string.Empty;

        public DiagramPlotEditorViewModel()
        {
            WindowTitle = LanguageService.Instance["new_template"] ?? "New Diagram";
            InitializePlotTypes();
            RefreshAvailableLanguageOptions();
            LoadCategories();
            LanguageParts.CollectionChanged += (_, _) =>
            {
                if (!_bulkLanguageUpdate)
                    OnLanguagesChanged();
            };
            TranslationDisplayRows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTranslationContent));
            CategoryParts.CollectionChanged += (_, _) =>
            {
                RebuildCategoryStructurePreview();
                RebuildTranslationPreview();
                RefreshTemplateStatus();
            };
            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
            WeakReferenceMessenger.Default.Register<DeveloperModeChangedMessage>(this);
        }

        /// <summary>添加语言弹窗的候选语言（与 EPMA 模板编辑器一致）。</summary>
        public ObservableCollection<EpmaEditorLanguageOption> LanguageCandidates { get; } = new();

        private bool _released;

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Item[]")
            {
                InitializePlotTypes();
                OnPropertyChanged(nameof(ActivePageTitle));
                RefreshTemplateStatus();
            }
        }

        /// <summary>
        /// 编辑器关闭时退订静态语言事件和消息。
        /// </summary>
        public void Release()
        {
            if (_released)
                return;

            _released = true;
            LanguageService.Instance.PropertyChanged -= OnLanguageChanged;
            WeakReferenceMessenger.Default.UnregisterAll(this);
            ReleaseTemplateStatus();
            GetCurrentRtfContent = null;
            SetCurrentRtfContent = null;
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            RefreshLanguageDisplayNames();
        }

        public void RefreshLanguageDisplayNames()
        {
            RefreshAvailableLanguageOptions();
            foreach (var part in LanguageParts)
                part.NotifyDisplayTextChanged();
        }

        /// <summary>
        /// 添加语言下拉框只保留尚未加入 LanguageParts 的语言。
        /// </summary>
        private void RefreshAvailableLanguageOptions()
        {
            var added = LanguageParts
                .Select(part => part.Text)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            AppLanguageOptions = AppCultureRegistry.GetOptions()
                .Where(option => !added.Contains(option.Code))
                .ToList();

            if (LanguageCandidates.Count == 0)
            {
                foreach (var option in AppCultureRegistry.GetOptions(false))
                    LanguageCandidates.Add(new EpmaEditorLanguageOption(option.Code, EpmaTemplateEditorViewModel.LanguageName(option.Code)));
            }

            foreach (var candidate in LanguageCandidates)
            {
                candidate.IsAdded = added.Contains(candidate.Code);
                candidate.CanToggle = !candidate.IsAdded || LanguageParts.Count > 1;
            }

            SelectAllLanguagesCommand.NotifyCanExecuteChanged();
        }

        public void InitializeForCreate()
        {
            Mode = DiagramPlotEditorMode.Create;
            WindowTitle = LanguageService.Instance["new_template"] ?? "New Diagram";
            IsPlotTypeSelectionEnabled = true;
            _editEntity = null;
            _workingTemplate = null;
            OriginalTemplateVersion = string.Empty;
            ClearAll();
            TryAddLanguage(AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage));
            SelectPlotType("2D_Plot");
            SelectedSectionIndex = 0;
            NotifyVersionPropertiesChanged();
            InitializeTemplateStatus(newTemplate: true);
        }

        public void InitializeForEdit(GraphMapTemplateEntity entity)
        {
            if (entity?.Content == null) return;

            var clonedContent = GraphMapTemplateService.CloneTemplateContent(entity.Content);
            if (clonedContent == null) return;

            bool clockwise = clonedContent.Clockwise;
            _suppressClockwiseRemap = true;
            try
            {
                Mode = DiagramPlotEditorMode.Edit;
                WindowTitle = LanguageService.Instance["edit_diagram_template"] ?? "Edit Diagram";
                IsPlotTypeSelectionEnabled = false;
                _editEntity = entity;
                _workingTemplate = clonedContent;
                OriginalTemplateVersion = ContentVersionHelper.Normalize(entity.Content.Version);

                _suppressTranslationRebuild = true;
                try
                {
                    ClearAll();

                    SelectPlotType(entity.Content.TemplateType == "Ternary" ? "Ternary_Plot" : "2D_Plot");

                    if (_workingTemplate.NodeList?.Translations != null)
                    {
                        foreach (var lang in _workingTemplate.NodeList.Translations.Keys)
                            LanguageParts.Add(new LanguageTagModel { Text = lang });
                    }

                    // ★ 以模板 DefaultLanguage 为准；字典 Keys 顺序不等于默认语言
                    EnsureDefaultLanguageFirst(_workingTemplate.DefaultLanguage);
                    UpdateLanguageDefaultStatus();

                    string defaultLang = DefaultLanguageCode;
                    if (!string.IsNullOrEmpty(defaultLang) &&
                        _workingTemplate.NodeList?.Translations != null &&
                        _workingTemplate.NodeList.Translations.TryGetValue(defaultLang, out var path))
                    {
                        var parts = path.Split('>', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        foreach (var part in parts)
                        {
                            CategoryParts.Add(new CategoryPartModel
                            {
                                DisplayName = part,
                                LocalizedNames = null
                            });
                        }
                    }

                    TranslationTable = DiagramTemplateTranslationHelper.BuildTranslationTable(
                        _workingTemplate,
                        LanguageParts.Select(l => l.Text));
                    IsTranslationSectionEnabled = LanguageParts.Count > 0;
                    TranslationStatusMessage = LanguageService.Instance["translation_loaded"] ?? "Translation data loaded";
                }
                finally
                {
                    _suppressTranslationRebuild = false;
                }

                RebuildCategoryStructurePreview();
                LoadHelpDocumentsFromEntity(entity);
                IsScriptSectionEnabled = true;
                NotifyScriptDefinitionChanged();
                PatchVersion = ContentVersionHelper.TryGetPatch(entity.Content.Version, out int patch) ? patch : 0;
                ApplyPatchToWorkingTemplate();
                IsClockwise = clockwise;
            }
            finally
            {
                _suppressClockwiseRemap = false;
            }
            ApplyClockwiseToWorkingTemplate();
            SelectedSectionIndex = 0;
            NotifyVersionPropertiesChanged();
            InitializeTemplateStatus(newTemplate: false);
        }

        partial void OnModeChanged(DiagramPlotEditorMode value) => NotifyVersionPropertiesChanged();

        partial void OnOriginalTemplateVersionChanged(string value) => NotifyVersionPropertiesChanged();

        /// <summary>顶栏当前页标题。</summary>
        public string ActivePageTitle => SelectedSectionIndex switch
        {
            SectionScript => LanguageService.Instance["epma_editor_nav_script"] ?? "数据与脚本",
            SectionHelp => LanguageService.Instance["epma_editor_documents"] ?? "帮助文档",
            SectionTranslation => LanguageService.Instance["diagram_editor_content_translation"] ?? "多语言",
            _ => LanguageService.Instance["epma_editor_nav_basic"] ?? "基本信息",
        };

        partial void OnSelectedSectionIndexChanged(int oldValue, int newValue)
        {
            OnPropertyChanged(nameof(ActivePageTitle));

            // 离开帮助文档分区时，保存当前语言的 RTF，并标记编辑器未同步
            if (oldValue == SectionHelp)
            {
                CommitCurrentHelpLanguageRtf(fromLeavingHelpSection: true);
                _isHelpDocEditorSynced = false;
            }

            // 进入帮助文档分区时，加载当前语言的 RTF
            if (newValue == SectionHelp)
            {
                SyncHelpLanguagesWithBasicSettings();
                LoadSelectedHelpLanguageIntoEditor();
                _isHelpDocEditorSynced = true;
            }

            if (newValue == SectionScript)
                EnsureScriptEditable();

            RefreshTemplateStatus();
        }

        // ---- IHelpDocEditorHost：与 EPMA 共用的帮助文档富文本编辑器 ----

        public bool CanShowSave => true;
        public bool CanEdit => true;
        public bool IsReadOnly => false;

        /// <summary>供 EditorStyles 中 GeoTColumnCell / GeoTRemoveColumn 绑定（图解编辑器始终可编辑）。</summary>
        public bool IsContentReadOnly => false;

        public void AttachHelpEditor(Func<string?> read, Action<string?> write)
        {
            GetCurrentRtfContent = read;
            SetCurrentRtfContent = write;

            if (SelectedSectionIndex == SectionHelp && SelectedHelpLanguage != null)
            {
                LoadSelectedHelpLanguageIntoEditor();
                _isHelpDocEditorSynced = true;
            }
        }

        public void UpdateHelpDocumentDraft(string? rtf)
        {
            if (_isHelpDocEditorSynced && SelectedSectionIndex == SectionHelp && SelectedHelpLanguage != null)
            {
                _helpDocuments[SelectedHelpLanguage] = rtf ?? string.Empty;
                RefreshTemplateStatus();
            }
        }

        public void ShowLanguageSettings() => SelectedSectionIndex = SectionBasic;

        partial void OnSelectedHelpLanguageChanged(string? oldValue, string? newValue)
        {
            // 仅在帮助文档分区内处理语言切换，避免未打开帮助文档时用空白编辑器覆盖
            if (SelectedSectionIndex != SectionHelp)
                return;

            if (oldValue != null &&
                _isHelpDocEditorSynced &&
                LanguageParts.Any(p => p.Text.Equals(oldValue, StringComparison.OrdinalIgnoreCase)))
            {
                CommitHelpLanguageRtf(oldValue);
            }

            if (newValue != null)
            {
                LoadSelectedHelpLanguageIntoEditor();
                _isHelpDocEditorSynced = true;
            }
            else
            {
                SetCurrentRtfContent?.Invoke(null);
                _isHelpDocEditorSynced = false;
            }

            RefreshTemplateStatus();
        }

        public void ClearAll()
        {
            LanguageParts.Clear();
            CategoryParts.Clear();
            TranslationTable = null;
            IsTranslationSectionEnabled = false;
            IsHelpSectionEnabled = false;
            IsScriptSectionEnabled = false;
            TranslationStatusMessage = string.Empty;
            ScriptStatus = string.Empty;
            ScriptHasError = false;
            PatchVersion = 0;
            IsClockwise = true;
            _helpDocuments.Clear();
            _isHelpDocEditorSynced = false;
            SelectedHelpLanguage = null;
        }

        partial void OnPatchVersionChanged(int value)
        {
            NotifyVersionPropertiesChanged();
            ApplyPatchToWorkingTemplate();
            DecreasePatchCommand?.NotifyCanExecuteChanged();
            IncreasePatchCommand?.NotifyCanExecuteChanged();
        }

        private void NotifyVersionPropertiesChanged()
        {
            OnPropertyChanged(nameof(TemplateVersion));
            OnPropertyChanged(nameof(SavedVersionText));
            OnPropertyChanged(nameof(IsEditMode));
        }

        [RelayCommand(CanExecute = nameof(CanDecreasePatch))]
        private void DecreasePatch()
        {
            if (PatchVersion > 0)
                PatchVersion--;
        }

        [RelayCommand(CanExecute = nameof(CanIncreasePatch))]
        private void IncreasePatch()
        {
            if (PatchVersion < MaxContentPatch)
                PatchVersion++;
        }

        private bool CanDecreasePatch() => PatchVersion > 0;

        private bool CanIncreasePatch() => PatchVersion < MaxContentPatch;

        private void ApplyPatchToWorkingTemplate()
        {
            if (_workingTemplate == null) return;
            _workingTemplate.Version = ContentVersionHelper.WithAppDiagramFormat(PatchVersion);
        }

        private void ApplyClockwiseToWorkingTemplate()
        {
            if (_workingTemplate == null) return;
            _workingTemplate.Clockwise = IsClockwise;
        }

        public IEnumerable<CategoryPartModel> GetCategoryParts() => CategoryParts;

        public GraphMapTemplate? GetWorkingTemplate() => _workingTemplate;

        public GraphMapTemplateEntity? GetEditEntity() => _editEntity;

        public void SelectPlotType(string plotType)
        {
            SelectedPlotType = NormalizePlotType(plotType);
        }

        partial void OnSelectedPlotTypeChanged(string value)
        {
            OnPropertyChanged(nameof(IsTernaryPlot));
            RebuildTranslationPreview();
        }

        public void UpdateLanguageDefaultStatus()
        {
            for (int i = 0; i < LanguageParts.Count; i++)
                LanguageParts[i].IsDefault = i == 0;
            OnPropertyChanged(nameof(DefaultLanguageCode));
        }

        /// <summary>
        /// 将模板默认语言移到 LanguageParts 首位（用于加载时纠正字典 Keys 顺序）。
        /// </summary>
        private void EnsureDefaultLanguageFirst(string? defaultLanguage)
        {
            if (string.IsNullOrWhiteSpace(defaultLanguage) || LanguageParts.Count == 0)
                return;

            int idx = LanguageParts.ToList().FindIndex(
                p => p.Text.Equals(defaultLanguage, StringComparison.OrdinalIgnoreCase));
            if (idx > 0)
                LanguageParts.Move(idx, 0);
            else if (idx < 0)
                LanguageParts.Insert(0, new LanguageTagModel { Text = defaultLanguage });
        }

        /// <summary>
        /// 将指定语言移到 LanguageParts 首位，作为默认图解语言。
        /// 不使用带参 CanExecute：ContextMenu 打开前 CommandParameter 常为 null，会导致菜单项一直灰显。
        /// </summary>
        [RelayCommand]
        private void SetDefaultLanguage(LanguageTagModel? item)
        {
            if (item == null) return;

            int idx = LanguageParts.IndexOf(item);
            if (idx <= 0) return;

            LanguageParts.Move(idx, 0);
        }

        public bool TryAddLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            if (!AppCultureRegistry.TryNormalize(code, out string normalized))
                return false;
            if (LanguageParts.Any(p => p.Text.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                return false;

            LanguageParts.Add(new LanguageTagModel { Text = normalized });
            UpdateLanguageDefaultStatus();
            return true;
        }

        public void RemoveLanguage(LanguageTagModel item)
        {
            if (item == null) return;
            LanguageParts.Remove(item);
            UpdateLanguageDefaultStatus();
        }

        /// <summary>添加语言弹层中的“全选”：一次加入全部尚未添加的语言。</summary>
        [RelayCommand(CanExecute = nameof(CanSelectAllLanguages))]
        private void SelectAllLanguages()
        {
            var pending = LanguageCandidates
                .Where(candidate => !candidate.IsAdded)
                .Select(candidate => candidate.Code)
                .ToList();
            if (pending.Count == 0)
                return;

            _bulkLanguageUpdate = true;
            try
            {
                foreach (var code in pending)
                    TryAddLanguage(code);
            }
            finally
            {
                _bulkLanguageUpdate = false;
            }

            OnLanguagesChanged();
        }

        private bool CanSelectAllLanguages() =>
            LanguageCandidates.Any(candidate => !candidate.IsAdded);

        /// <summary>“添加语言”弹层中点击候选项：已添加则移除，未添加则添加。</summary>
        [RelayCommand]
        private void ToggleLanguage(EpmaEditorLanguageOption? candidate)
        {
            if (candidate == null) return;

            if (candidate.IsAdded)
            {
                var part = LanguageParts.FirstOrDefault(p => p.Text == candidate.Code);
                if (part != null)
                    RemoveLanguage(part);
            }
            else
            {
                TryAddLanguage(candidate.Code);
            }
        }

        public bool TryAddCategory(string text, Dictionary<string, string>? localizedNames = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (text.IndexOfAny(invalidChars) >= 0)
            {
                MessageHelper.Error(LanguageService.Instance["invalid_filename_char"]);
                return false;
            }

            CategoryParts.Add(new CategoryPartModel
            {
                DisplayName = text.Trim(),
                LocalizedNames = localizedNames
            });
            return true;
        }

        public void RemoveCategory(CategoryPartModel item)
        {
            if (item != null)
                CategoryParts.Remove(item);
        }

        public List<Dictionary<string, string>>? GetCategorySuggestions()
        {
            if (_categoryConfig == null) return null;
            int levelIndex = CategoryParts.Count + 1;
            string levelKey = $"Level{levelIndex}";
            return _categoryConfig.TryGetValue(levelKey, out var list) ? list : null;
        }

        public void ReloadCategories() => LoadCategories();

        public IReadOnlyList<CategoryStructureSelectNode> GetExistingCategoryStructureTree()
        {
            var entities = GraphMapDatabaseService.Instance.GetSummaries();
            Guid? excludeId = _editEntity?.Id;
            return PlotCategoryStructureHelper.BuildSelectableStructureTree(entities, excludeId);
        }

        public void ApplyExistingCategoryStructure(IReadOnlyList<CategoryPartModel> parts)
        {
            CategoryParts.Clear();
            foreach (var part in parts)
            {
                CategoryParts.Add(new CategoryPartModel
                {
                    DisplayName = part.DisplayName,
                    LocalizedNames = part.LocalizedNames
                });
            }
        }

        [RelayCommand]
        private void ExportTranslations()
        {
            if (TranslationTable == null)
            {
                TranslationStatusMessage = LanguageService.Instance["translation_export_table_missing"] ?? "Translation table is not ready.";
                return;
            }

            var languages = LanguageParts.Select(l => l.Text).ToList();
            if (languages.Count == 0)
            {
                TranslationStatusMessage = LanguageService.Instance["translation_export_no_languages"] ?? "Configure languages in basic settings first.";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = LanguageService.Instance["diagram_translation_export"] ?? "Export Translations",
                Filter = FileDialogFilterHelper.JsonOnly,
                DefaultExt = "json",
                FileName = "diagram-translations.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string json = DiagramTemplateTranslationHelper.ExportToJson(
                    TranslationTable,
                    languages,
                    DefaultLanguageCode);
                File.WriteAllText(dialog.FileName, json);
                string exportedMessage = string.Format(
                    LanguageService.Instance["translation_export_success"] ?? "Exported translations to {0}.",
                    dialog.FileName);
                TranslationStatusMessage = exportedMessage;
                MessageHelper.ExportSuccess(exportedMessage, dialog.FileName, ExportNotificationCenter);
            }
            catch (Exception ex)
            {
                TranslationStatusMessage = $"{LanguageService.Instance["translation_export_failed"] ?? "Export failed."} {ex.Message}";
            }
        }

        [RelayCommand]
        private void ImportTranslations()
        {
            if (TranslationTable == null)
            {
                TranslationStatusMessage = LanguageService.Instance["translation_import_table_missing"] ?? "Translation table is not ready.";
                return;
            }

            var languages = LanguageParts.Select(l => l.Text).ToList();
            if (languages.Count == 0)
            {
                TranslationStatusMessage = LanguageService.Instance["translation_import_no_languages"] ?? "Configure languages in basic settings first.";
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = LanguageService.Instance["diagram_translation_import"] ?? "Import Translations",
                Filter = FileDialogFilterHelper.JsonOnly,
                DefaultExt = "json"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string json = File.ReadAllText(dialog.FileName);
                var result = DiagramTemplateTranslationHelper.ImportFromJson(json, TranslationTable, languages);
                if (!result.IsSuccess)
                {
                    TranslationStatusMessage = result.ErrorMessage;
                    return;
                }

                RefreshTranslationTableBinding();
                SyncWorkingTemplateFromTable();
                TranslationStatusMessage = string.Format(
                    LanguageService.Instance["translation_import_success"] ?? "Imported {0} items, updated {1} cells.",
                    result.MatchedItems,
                    result.UpdatedCells);
            }
            catch (Exception ex)
            {
                TranslationStatusMessage = $"{LanguageService.Instance["translation_import_failed"] ?? "Import failed."} {ex.Message}";
            }
        }

        public bool Validate(out string errorMessage)
        {
            if (LanguageParts.Count == 0)
            {
                errorMessage = LanguageService.Instance["all_fields_required"] ?? string.Empty;
                return false;
            }

            if (CategoryParts.Count < 2)
            {
                errorMessage = LanguageService.Instance["category_structure_min_two"] ?? string.Empty;
                return false;
            }

            if (Mode == DiagramPlotEditorMode.Create && string.IsNullOrEmpty(SelectedPlotType))
            {
                errorMessage = LanguageService.Instance["all_fields_required"] ?? string.Empty;
                return false;
            }

            var langs = LanguageParts.Select(l => l.Text).ToList();
            if (langs.Distinct(StringComparer.OrdinalIgnoreCase).Count() != langs.Count)
            {
                errorMessage = LanguageService.Instance["language_setting_duplicate_found"] ?? string.Empty;
                return false;
            }

            if (PatchVersion < 0)
            {
                errorMessage = LanguageService.Instance["geo_validate_version_format"] ?? string.Empty;
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public LocalizedString BuildCategoryNodeList()
        {
            var languages = LanguageParts.Select(l => l.Text).ToList();
            return DiagramTemplateTranslationHelper.BuildCategoryNodeList(languages, CategoryParts.ToList());
        }

        public GraphMapTemplate BuildTemplateForSubmit()
        {
            var languages = LanguageParts.Select(l => l.Text).ToList();
            var categoryNodeList = BuildCategoryNodeList();

            GraphMapTemplate template;
            if (Mode == DiagramPlotEditorMode.Edit)
            {
                if (_workingTemplate == null)
                    throw new InvalidOperationException(LanguageService.Instance["diagram_template_source_missing"]);

                template = _workingTemplate;
            }
            else
            {
                // 新建模式：复用编辑过程中的工作模板，否则数据脚本、翻译表等改动会被全新的默认模板覆盖
                if (_workingTemplate != null && IsWorkingTemplateOfSelectedType(_workingTemplate))
                {
                    template = _workingTemplate;
                }
                else
                {
                    template = GraphMapTemplate.CreateDefault(languages, SelectedPlotType, categoryNodeList);
                    _workingTemplate = template;
                    ReloadDataSeriesColumnsFromScript();
                }
            }

            // 提交前把数据列表格的内容落到脚本定义上，避免依赖属性变更事件
            if (DataSeriesColumns.Count > 0)
                CommitDataSeriesColumnsToScript();

            if (TranslationTable != null)
            {
                DiagramTemplateTranslationHelper.ApplyTranslationTable(TranslationTable, template);
            }
            else
            {
                DiagramTemplateTranslationHelper.SyncTemplateLanguages(template, languages);
            }

            template.NodeList ??= new LocalizedString();
            MergeCategoryNodeListFromBasicSettings(template.NodeList, categoryNodeList, languages);

            template.DefaultLanguage = languages.First();
            template.Version = ContentVersionHelper.WithAppDiagramFormat(PatchVersion);
            template.Clockwise = IsClockwise;

            return GraphMapTemplateService.CloneTemplateContent(template) ?? template;
        }

        /// <summary>
        /// 提交前收集各语言 RTF 帮助文档（仅含非空内容，且不会从未同步的编辑器覆盖已有文档）。
        /// </summary>
        public Dictionary<string, string> GetHelpDocumentsForSubmit()
        {
            CommitCurrentHelpLanguageRtf(fromLeavingHelpSection: false);

            var result = new Dictionary<string, string>();
            foreach (var lang in LanguageParts.Select(l => l.Text))
            {
                if (_helpDocuments.TryGetValue(lang, out var rtf) && !IsBlankRtf(rtf))
                    result[lang] = rtf;
            }

            return result;
        }

        public void OnTranslationCellChanged()
        {
            SyncWorkingTemplateFromTable();
        }

        private void OnLanguagesChanged()
        {
            RefreshAvailableLanguageOptions();
            UpdateLanguageDefaultStatus();
            SyncHelpLanguagesWithBasicSettings();
            RebuildCategoryStructurePreview();
            RebuildTranslationPreview();
            RefreshTemplateStatus();
        }

        private void LoadHelpDocumentsFromEntity(GraphMapTemplateEntity entity)
        {
            _helpDocuments.Clear();
            _isHelpDocEditorSynced = false;
            if (entity.HelpDocuments != null)
            {
                foreach (var kvp in entity.HelpDocuments)
                    _helpDocuments[kvp.Key] = kvp.Value;
            }

            SyncHelpLanguagesWithBasicSettings();
            SelectedHelpLanguage = LanguageParts.FirstOrDefault()?.Text;
        }

        private void SyncHelpLanguagesWithBasicSettings()
        {
            var langs = LanguageParts.Select(l => l.Text).ToList();

            foreach (var key in _helpDocuments.Keys.ToList())
            {
                if (!langs.Any(l => l.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    _helpDocuments.Remove(key);
            }

            foreach (var lang in langs)
            {
                if (!_helpDocuments.ContainsKey(lang))
                    EnsureDefaultHelpDocForLanguage(lang);
            }

            IsHelpSectionEnabled = true;

            if (SelectedHelpLanguage == null ||
                !langs.Any(l => l.Equals(SelectedHelpLanguage, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedHelpLanguage = langs.FirstOrDefault();
            }
        }

        private void EnsureDefaultHelpDocForLanguage(string langCode)
        {
            if (_helpDocuments.TryGetValue(langCode, out var existing) && !string.IsNullOrWhiteSpace(existing))
                return;

            _helpDocuments[langCode] = TryLoadDefaultTemplateRtf() ?? string.Empty;
        }

        private static string? TryLoadDefaultTemplateRtf()
        {
            string sourceRtfPath = Path.Combine(FileHelper.GetAppPath(), "Data", "Documents", "template.rtf");
            if (!File.Exists(sourceRtfPath))
            {
                try
                {
                    string devPath = Path.GetFullPath(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "Documents", "template.rtf"));
                    if (File.Exists(devPath))
                        sourceRtfPath = devPath;
                }
                catch
                {
                    // ignore
                }
            }

            if (!File.Exists(sourceRtfPath))
                return null;

            try
            {
                return File.ReadAllText(sourceRtfPath);
            }
            catch
            {
                return null;
            }
        }

        private void LoadSelectedHelpLanguageIntoEditor()
        {
            if (SelectedHelpLanguage == null) return;
            _helpDocuments.TryGetValue(SelectedHelpLanguage, out var content);
            SetCurrentRtfContent?.Invoke(content);
        }

        /// <summary>
        /// 保存当前选中语言的 RTF 内容到字典（仅当处于帮助文档分区且编辑器已同步）。
        /// </summary>
        private void CommitCurrentHelpLanguageRtf(bool fromLeavingHelpSection)
        {
            if (!_isHelpDocEditorSynced || SelectedHelpLanguage == null)
                return;

            // 离开帮助文档分区时 SelectedSectionIndex 已变为新值，需用 fromLeavingHelpSection 放行
            if (!fromLeavingHelpSection && SelectedSectionIndex != SectionHelp)
                return;

            CommitHelpLanguageRtf(SelectedHelpLanguage);
        }

        private void CommitHelpLanguageRtf(string languageCode)
        {
            if (!LanguageParts.Any(p => p.Text.Equals(languageCode, StringComparison.OrdinalIgnoreCase)) ||
                GetCurrentRtfContent == null)
            {
                return;
            }

            var rtf = GetCurrentRtfContent();
            if (rtf == null)
                return;

            // 空白编辑器内容不得覆盖已有非空帮助文档（防止从未打开帮助文档就保存）
            if (IsBlankRtf(rtf) &&
                _helpDocuments.TryGetValue(languageCode, out var existing) &&
                !IsBlankRtf(existing))
            {
                return;
            }

            _helpDocuments[languageCode] = rtf;
        }

        private static bool IsBlankRtf(string? rtf)
        {
            if (string.IsNullOrWhiteSpace(rtf))
                return true;

            string text = Regex.Replace(rtf, @"\\[a-z]+\d* ?|\\'[0-9a-f]{2}|\{|\}", string.Empty, RegexOptions.IgnoreCase);
            return string.IsNullOrWhiteSpace(text);
        }

        private void RebuildCategoryStructurePreview()
        {
            CategoryStructurePreviewRoot.Children.Clear();

            if (CategoryParts.Count == 0)
            {
                OnPropertyChanged(nameof(HasCategoryStructurePreview));
                return;
            }

            var previewLanguage = DefaultLanguageCode;
            if (string.IsNullOrWhiteSpace(previewLanguage))
                previewLanguage = LanguageService.CurrentLanguage ?? AppCultureRegistry.DefaultAppLanguage;

            GraphMapTemplateNode currentNode = CategoryStructurePreviewRoot;
            for (int i = 0; i < CategoryParts.Count; i++)
            {
                var part = CategoryParts[i];
                bool isLeaf = i == CategoryParts.Count - 1;
                var node = new GraphMapTemplateNode
                {
                    Name = GetCategoryPartDisplayName(part, previewLanguage),
                    GraphMapPath = isLeaf ? "preview" : string.Empty,
                    IsExpanded = true,
                    Parent = currentNode
                };
                currentNode.Children.Add(node);
                currentNode = node;
            }

            OnPropertyChanged(nameof(HasCategoryStructurePreview));
        }

        private static string GetCategoryPartDisplayName(CategoryPartModel part, string languageCode)
        {
            if (!string.IsNullOrWhiteSpace(languageCode) &&
                part.LocalizedNames != null &&
                part.LocalizedNames.TryGetValue(languageCode, out var localized) &&
                !string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            return part.DisplayName;
        }

        private void RebuildTranslationPreview()
        {
            if (_suppressTranslationRebuild) return;

            var languages = LanguageParts.Select(l => l.Text).ToList();
            IsTranslationSectionEnabled = true;
            IsScriptSectionEnabled = true;

            if (languages.Count == 0 || CategoryParts.Count < 2)
            {
                TranslationTable = null;
                return;
            }

            try
            {
                var categoryNodeList = DiagramTemplateTranslationHelper.BuildCategoryNodeList(
                    languages, CategoryParts.ToList());

                GraphMapTemplate draft;
                if (Mode == DiagramPlotEditorMode.Edit)
                {
                    if (_workingTemplate == null) return;

                    draft = _workingTemplate;
                    draft.NodeList ??= new LocalizedString();
                    MergeCategoryNodeListFromBasicSettings(draft.NodeList, categoryNodeList, languages);
                    DiagramTemplateTranslationHelper.SyncTemplateLanguages(draft, languages);
                }
                else if (_workingTemplate != null && IsWorkingTemplateOfSelectedType(_workingTemplate))
                {
                    // 新建模式下语言/分类变化时复用已有草稿，保留已编辑的数据脚本与翻译
                    draft = _workingTemplate;
                    draft.NodeList ??= new LocalizedString();
                    MergeCategoryNodeListFromBasicSettings(draft.NodeList, categoryNodeList, languages);
                    DiagramTemplateTranslationHelper.SyncTemplateLanguages(draft, languages);
                }
                else
                {
                    draft = GraphMapTemplate.CreateDefault(languages, SelectedPlotType, categoryNodeList);
                    if (TranslationTable != null)
                        DiagramTemplateTranslationHelper.ApplyTranslationTable(TranslationTable, draft);
                }

                TranslationTable = DiagramTemplateTranslationHelper.SyncTableLanguages(
                    TranslationTable, languages, draft);
                _workingTemplate = draft;
                ApplyPatchToWorkingTemplate();
                ApplyClockwiseToWorkingTemplate();
                NotifyScriptDefinitionChanged();
            }
            catch (Exception ex)
            {
                TranslationStatusMessage = ex.Message;
            }
        }

        private bool IsWorkingTemplateOfSelectedType(GraphMapTemplate template)
        {
            string expected = SelectedPlotType == "Ternary_Plot" ? "Ternary" : "Cartesian";
            return string.Equals(template.TemplateType, expected, StringComparison.Ordinal);
        }

        private void NotifyScriptDefinitionChanged()
        {
            OnPropertyChanged(nameof(ScriptDefinition));
            EnsureScriptEditable();
            ReloadDataSeriesColumnsFromScript();
            HookStatusScript();
        }

        private void EnsureScriptEditable()
        {
            if (_workingTemplate?.Script != null)
                _workingTemplate.Script.IsReadOnly = false;
        }

        private void ReloadDataSeriesColumnsFromScript()
        {
            _suppressDataSeriesSync = true;
            try
            {
                DataSeriesColumns.Clear();
                string raw = ScriptDefinition?.RequiredDataSeries ?? string.Empty;
                // 单位按位置与列名一一对应（保留空项），脚本格式 1.3 之前的模板没有单位，对应位置即为空
                var units = (ScriptDefinition?.DataSeriesUnits ?? string.Empty).Split(',', StringSplitOptions.TrimEntries);
                int index = 0;
                foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    DataSeriesColumns.Add(new DiagramDataSeriesColumn(part) { Unit = index < units.Length ? units[index] : string.Empty });
                    index++;
                }

                if (DataSeriesColumns.Count == 0)
                    DataSeriesColumns.Add(new DiagramDataSeriesColumn(string.Empty));
            }
            finally
            {
                _suppressDataSeriesSync = false;
            }

            foreach (var column in DataSeriesColumns)
                HookDataSeriesColumn(column);
        }

        private void HookDataSeriesColumn(DiagramDataSeriesColumn column)
        {
            column.PropertyChanged -= OnDataSeriesColumnChanged;
            column.PropertyChanged += OnDataSeriesColumnChanged;
        }

        private void OnDataSeriesColumnChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DiagramDataSeriesColumn.Id) or nameof(DiagramDataSeriesColumn.Unit))
                CommitDataSeriesColumnsToScript();
        }

        private void CommitDataSeriesColumnsToScript()
        {
            if (_suppressDataSeriesSync || ScriptDefinition == null) return;
            // 只保留有列名的列；单位与列名同序写出，单位里的逗号会破坏按位置对应的存储格式，统一换成空格
            var named = DataSeriesColumns.Where(c => !string.IsNullOrWhiteSpace(c.Id?.Trim())).ToList();
            ScriptDefinition.RequiredDataSeries = string.Join(",", named.Select(c => c.Id.Trim()));
            var units = named.Select(c => (c.Unit ?? string.Empty).Replace(',', ' ').Replace(';', ' ').Trim()).ToList();
            // 所有列都没有单位时保持为空串，与 1.3 之前的模板内容一致
            ScriptDefinition.DataSeriesUnits = units.Any(u => u.Length > 0) ? string.Join(",", units) : string.Empty;
        }

        [RelayCommand]
        private void AddDataSeries()
        {
            var column = new DiagramDataSeriesColumn(string.Empty);
            HookDataSeriesColumn(column);
            DataSeriesColumns.Add(column);
        }

        [RelayCommand]
        private void RemoveDataSeries(DiagramDataSeriesColumn? column)
        {
            if (column == null) return;
            if (DataSeriesColumns.Count <= 1)
            {
                column.Id = string.Empty;
                CommitDataSeriesColumnsToScript();
                return;
            }

            column.PropertyChanged -= OnDataSeriesColumnChanged;
            DataSeriesColumns.Remove(column);
            CommitDataSeriesColumnsToScript();
        }

        /// <summary>
        /// 基本设置中的分类变更时，仅同步默认语言路径；其他语言保留翻译表中的自定义内容。
        /// </summary>
        private static void MergeCategoryNodeListFromBasicSettings(
            LocalizedString target,
            LocalizedString rebuiltFromBasicSettings,
            IList<string> languages)
        {
            if (languages.Count == 0) return;

            target.Translations ??= new Dictionary<string, string>();
            string defaultLang = languages[0];

            if (rebuiltFromBasicSettings.Translations.TryGetValue(defaultLang, out var defaultPath))
                target.Translations[defaultLang] = defaultPath;

            foreach (var lang in languages)
            {
                if (lang == defaultLang) continue;

                if (!target.Translations.TryGetValue(lang, out var existing) || string.IsNullOrWhiteSpace(existing))
                {
                    if (rebuiltFromBasicSettings.Translations.TryGetValue(lang, out var fallback))
                        target.Translations[lang] = fallback;
                }
            }

            target.Default = defaultLang;
        }

        private void SyncWorkingTemplateFromTable()
        {
            if (_workingTemplate == null || TranslationTable == null) return;
            DiagramTemplateTranslationHelper.ApplyTranslationTable(TranslationTable, _workingTemplate);
        }

        private void RefreshTranslationTableBinding()
        {
            var temp = TranslationTable;
            TranslationTable = null;
            TranslationTable = temp;
        }

        /// <summary>
        /// 将 DataTable 翻译表投影为 EPMA 多语言编辑窗同款的冻结列 + 横向滚动列结构。
        /// </summary>
        private void RebuildTranslationDisplay()
        {
            TranslationDisplayRows.Clear();
            TranslationColumns.Clear();

            if (TranslationTable == null)
            {
                OnPropertyChanged(nameof(TranslationDefaultColumn));
                OnPropertyChanged(nameof(TranslationOtherColumns));
                OnPropertyChanged(nameof(TranslationLanguageCount));
                RefreshTemplateStatus();
                return;
            }

            var languages = LanguageParts.Select(l => l.Text).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (languages.Count == 0)
            {
                languages = TranslationTable.Columns.Cast<DataColumn>()
                    .Where(c => DiagramTemplateTranslationHelper.IsLanguageColumn(c.ColumnName))
                    .Select(c => c.ColumnName)
                    .ToList();
            }

            for (int i = 0; i < languages.Count; i++)
            {
                string code = languages[i];
                string header = EpmaTemplateEditorViewModel.LanguageName(code);
                TranslationColumns.Add(new EpmaLocalizationColumn(header, i == 0));
            }

            string placeholder = LanguageService.Instance["epma_loc_placeholder"] ?? string.Empty;
            foreach (DataRow dataRow in TranslationTable.Rows)
            {
                string label = dataRow["Context"] as string ?? string.Empty;
                var source = new EpmaLocalizationRow(EpmaLocalizationRowKind.Name, label, languages.Count);
                var cells = new List<EpmaLocalizationCell>();
                for (int i = 0; i < languages.Count; i++)
                {
                    string lang = languages[i];
                    string value = TranslationTable.Columns.Contains(lang)
                        ? dataRow[lang] as string ?? string.Empty
                        : string.Empty;
                    source.Values[i] = value;
                    var cell = new EpmaLocalizationCell(value, placeholder, isMultiline: true, TranslationColumns[i]);
                    int langIndex = i;
                    string langCode = lang;
                    DataRow capturedRow = dataRow;
                    cell.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName != nameof(EpmaLocalizationCell.Text)) return;
                        if (TranslationTable != null && TranslationTable.Columns.Contains(langCode))
                            capturedRow[langCode] = cell.Text ?? string.Empty;
                        source.Values[langIndex] = cell.Text ?? string.Empty;
                        RefreshTranslationColumnProgress();
                        SyncWorkingTemplateFromTable();
                        RefreshTemplateStatus();
                    };
                    cells.Add(cell);
                }

                TranslationDisplayRows.Add(new EpmaLocalizationTableRow(source, cells));
            }

            RefreshTranslationColumnProgress();
            OnPropertyChanged(nameof(TranslationDefaultColumn));
            OnPropertyChanged(nameof(TranslationOtherColumns));
            OnPropertyChanged(nameof(TranslationLanguageCount));
            RefreshTemplateStatus();
        }

        private void RefreshTranslationColumnProgress()
        {
            int total = TranslationDisplayRows.Count;
            for (int column = 0; column < TranslationColumns.Count; column++)
            {
                int filled = TranslationDisplayRows.Count(row =>
                    column < row.Cells.Count && !string.IsNullOrWhiteSpace(row.Cells[column].Text));
                var target = TranslationColumns[column];
                target.Filled = filled;
                target.Total = total;
                target.State = filled == 0 ? EpmaLocalizationColumnState.Empty
                    : filled >= total ? EpmaLocalizationColumnState.Complete
                    : EpmaLocalizationColumnState.Partial;
            }
        }

        private void LoadCategories()
        {
            _categoryConfig = PlotCategoryHelper.LoadConfig();
        }

        private void InitializePlotTypes()
        {
            PlotTypeOptions.Clear();
            PlotTypeOptions.Add(new PlotTypeOption
            {
                Key = "2D_Plot",
                Badge = "XY",
                Title = LanguageService.Instance["two_dimensional_coordinate_plot"] ?? "2D Plot",
                Description = LanguageService.Instance["plot_type_2d_description"] ?? "Best for standard X-Y plotting.",
                SelectedLabel = LanguageService.Instance["selected_label"] ?? "Selected"
            });
            PlotTypeOptions.Add(new PlotTypeOption
            {
                Key = "Ternary_Plot",
                Badge = "ABC",
                Title = LanguageService.Instance["ternary_phase_diagram"] ?? "Ternary",
                Description = LanguageService.Instance["plot_type_ternary_description"] ?? "Best for ternary compositions and phase diagrams.",
                SelectedLabel = LanguageService.Instance["selected_label"] ?? "Selected"
            });
        }

        private static string NormalizePlotType(string plotType) => plotType switch
        {
            "Ternary" => "Ternary_Plot",
            "Cartesian" => "2D_Plot",
            "Ternary_Plot" => "Ternary_Plot",
            _ => "2D_Plot"
        };
    }

    /// <summary>图解「数据与脚本」页中的数据序列列名。</summary>
    public partial class DiagramDataSeriesColumn : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        /// <summary>该数据列的单位（可选，留空表示没有单位）。</summary>
        [ObservableProperty]
        private string _unit = string.Empty;

        public DiagramDataSeriesColumn(string id) => Id = id ?? string.Empty;
    }
}
