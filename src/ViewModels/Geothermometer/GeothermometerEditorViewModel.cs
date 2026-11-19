using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 自定义温压计编辑器 ViewModel
    /// 用于新建和编辑自定义温压计，支持按示例测试脚本和导出
    /// 三步流程：基本信息 → 数据与脚本 → 帮助文档
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "WPF binding requires instance members.")]
    public partial class GeothermometerEditorViewModel : ObservableObject,
        IRecipient<DeveloperModeChangedMessage>,
        IRecipient<GeoTMineralCategoryUpdatedMessage>,
        IHelpDocEditorHost
    {
        // ==================== 步骤导航 ====================

        [ObservableProperty]
        private int currentStep;

        // ==================== 元数据（Step 0） ====================

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string selectedCategory = GeoTCategoryHelper.DefaultCategoryKey;

        public ObservableCollection<GeoTTagModel> TagParts { get; } = new();

        public ObservableCollection<GeoTCapabilityModel> CapabilityParts { get; } = new();

        public ObservableCollection<GeoTCategoryOption> CategoryOptions { get; } = new();

        [ObservableProperty]
        private string author = string.Empty;

        [ObservableProperty]
        private int year = DateTime.Now.Year;

        [ObservableProperty]
        private string reference = string.Empty;

        [ObservableProperty]
        private string formulaName = string.Empty;

        private const int MaxContentPatch = 9999;

        [ObservableProperty]
        private int patchVersion;

        /// <summary>编辑模式下打开时的原始模板版本（只读展示）。</summary>
        [ObservableProperty]
        private string originalTemplateVersion = string.Empty;

        /// <summary>当前软件支持的温压计格式版本 x.y.z（只读）。</summary>
        public string AppFormatVersion =>
            ContentVersionHelper.Normalize(ContentVersionHelper.GetGeothermometerFormatVersion());

        /// <summary>程序固定的格式基线 x.y.（用户不可编辑）</summary>
        public string FormatVersionPrefix
        {
            get
            {
                var normalized = ContentVersionHelper.Normalize(ContentVersionHelper.GetGeothermometerFormatVersion());
                if (ContentVersionHelper.TryGetPatch(normalized, out _))
                {
                    var parts = normalized.Split('.');
                    if (parts.Length >= 2)
                        return $"{parts[0]}.{parts[1]}.";
                }

                return "1.0.";
            }
        }

        /// <summary>保存后将写入的完整温压计版本 x.y.z。</summary>
        public string TemplateVersion => ContentVersionHelper.WithAppGeothermometerFormat(PatchVersion);

        /// <summary>保存后版本的展示。与打开时的版本不同时为「原版本 -> 当前版本」。</summary>
        public string SavedVersionText => ContentVersionHelper.FormatSavedVersion(
            OriginalTemplateVersion,
            TemplateVersion);

        /// <summary>温压计格式基线 x.y。</summary>
        public string FormatVersionBaseline =>
            ContentVersionHelper.FormatBaseline(ContentVersionHelper.GetGeothermometerFormatVersion());

        public string PreviewName => string.IsNullOrWhiteSpace(Name)
            ? LanguageService.Instance["geo_preview_name_placeholder"] ?? "Thermometer Name"
            : Name.Trim();

        public string PreviewMetaLine
        {
            get
            {
                var authorText = string.IsNullOrWhiteSpace(Author)
                    ? LanguageService.Instance["geo_preview_author_placeholder"] ?? "Author"
                    : Author.Trim();
                var yearText = Year > 0 ? Year.ToString() : DateTime.Now.Year.ToString();
                var format = LanguageService.Instance["geo_preview_meta_format"] ?? "{0} ({1})";
                return string.Format(format, authorText, yearText);
            }
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

        private void ShowSuccess(string message)
        {
            if (ShowSuccessMessage != null)
            {
                ShowSuccessMessage(message);
                return;
            }

            MessageHelper.Success(message);
        }

        public string PreviewReference => string.IsNullOrWhiteSpace(Reference)
            ? LanguageService.Instance["geo_preview_reference_placeholder"] ?? "Reference will appear here"
            : Reference.Trim();

        public ObservableCollection<string> PreviewTags
        {
            get
            {
                if (TagParts.Count == 0)
                {
                    return new ObservableCollection<string>
                    {
                        LanguageService.Instance["geo_preview_tags_placeholder"] ?? "Tags"
                    };
                }

                return new ObservableCollection<string>(TagParts.Select(t => t.DisplayName));
            }
        }

        public ObservableCollection<string> PreviewCapabilities =>
            new(CapabilityParts.Select(c => c.DisplayName));

        public string PreviewVersion => TemplateVersion;

        // ==================== 数据与脚本（Step 1） ====================

        /// <summary>输入列（顺序即脚本 calculate(args) 的参数顺序）。</summary>
        public ObservableCollection<EpmaEditorColumn> InputColumns { get; } = new();

        /// <summary>输出列（示例值为 Grid 公式，如 =MyFormula(A2,B2)）。</summary>
        public ObservableCollection<EpmaEditorColumn> OutputColumns { get; } = new();

        /// <summary>
        /// 表头真实顺序（输入/输出可交错）。保存时按此顺序写出 Headers / ExampleRow，
        /// 以免旧模板里按列字母引用的示例公式错位。
        /// </summary>
        private readonly List<EpmaEditorColumn> _layout = new();

        /// <summary>新增列后由命令触发，供表格控件滚动并聚焦到新列（true = 输入列）。</summary>
        public event Action<EpmaEditorColumn, bool>? ColumnAdded;

        [ObservableProperty]
        private string scriptContent = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasScriptStatus))]
        private string scriptStatus = string.Empty;

        [ObservableProperty]
        private bool scriptHasError;

        public bool HasScriptStatus => !string.IsNullOrWhiteSpace(ScriptStatus);

        public ObservableCollection<AdditionalFormulaItemViewModel> AdditionalFormulaItems { get; } = new();

        private const string MainFunctionNameValue = "calculate";

        /// <summary>主公式固定映射的 JS 函数名（供 UI 绑定）。</summary>
        public string MainFunctionName => MainFunctionNameValue;

        private static readonly Regex JsIdentifierRegex = new(@"^[a-zA-Z_$][a-zA-Z0-9_$]*$", RegexOptions.Compiled);

        // ==================== 帮助文档（Step 2） ====================

        /// <summary>
        /// 内部存储：语言代码 → RTF 内容
        /// </summary>
        private readonly Dictionary<string, string> _helpDocuments = new();

        /// <summary>
        /// 可编写帮助文档的语言代码（与基本信息页的语言一致，默认语言在前）
        /// </summary>
        public ObservableCollection<string> AddedLanguages { get; } = new();

        /// <summary>
        /// 当前选中的语言
        /// </summary>
        [ObservableProperty]
        private string? selectedLanguage;

        // ---- IHelpDocEditorHost：与 EPMA 共用的帮助文档富文本编辑器（EpmaHelpDocEditor）所需成员 ----

        /// <summary>编辑器语言胶囊绑定的当前语言（即 SelectedLanguage）。</summary>
        public string? SelectedHelpLanguage
        {
            get => SelectedLanguage;
            set => SelectedLanguage = value;
        }

        /// <summary>编辑器工具栏是否可见。</summary>
        public bool CanShowSave => !IsContentReadOnly;

        /// <summary>编辑器工具栏是否可用。</summary>
        public bool CanEdit => !IsContentReadOnly;

        /// <summary>编辑器是否只读。</summary>
        public bool IsReadOnly => IsContentReadOnly;

        /// <summary>编辑器挂接：通过回调读写当前语言的 RTF。</summary>
        public void AttachHelpEditor(Func<string?> read, Action<string?> write)
        {
            GetCurrentRtfContent = read;
            SetCurrentRtfContent = write;

            if (CurrentStep == 2 && SelectedLanguage != null)
            {
                LoadSelectedLanguageIntoEditor();
                _isHelpDocEditorSynced = true;
            }
        }

        /// <summary>编辑器内容变化时暂存草稿，避免只靠离开页面时读取。</summary>
        public void UpdateHelpDocumentDraft(string? rtf)
        {
            if (_isHelpDocEditorSynced && CurrentStep == 2 && SelectedLanguage != null)
            {
                _helpDocuments[SelectedLanguage] = rtf ?? string.Empty;
                RefreshTemplateStatus();
            }
        }

        /// <summary>“管理语言”：回到基本信息页的语言设置。</summary>
        public void ShowLanguageSettings() => CurrentStep = 0;

        // ==================== 多语言（名称、参考文献） ====================

        // Name / Reference 属性始终存放“默认语言（LanguageParts 第一项）”的文字，
        // 其余语言的文字保存在 _translations 中。
        private sealed class GeoTTranslation
        {
            public string Name { get; init; } = string.Empty;
            public string Reference { get; init; } = string.Empty;
        }

        private readonly Dictionary<string, GeoTTranslation> _translations = new(StringComparer.Ordinal);
        private readonly string _sourceLanguage;
        private string _activeDefaultLanguage = string.Empty;
        private bool _switchingLanguage;

        /// <summary>批量加入语言时先挂起逐项刷新，结束后再统一刷新。</summary>
        private bool _bulkLanguageUpdate;

        /// <summary>语言标签：默认语言始终在第一位并带标记。</summary>
        public ObservableCollection<LanguageTagModel> LanguageParts { get; } = new();

        /// <summary>添加语言弹出框中的候选语言。</summary>
        public ObservableCollection<EpmaEditorLanguageOption> LanguageCandidates { get; } = new();

        /// <summary>默认语言代码。</summary>
        public string DefaultLanguage => _activeDefaultLanguage;

        /// <summary>
        /// 由 View 设置的回调：获取当前 RichTextBox 中的 RTF 内容
        /// </summary>
        public Func<string?>? GetCurrentRtfContent { get; set; }

        /// <summary>
        /// 由 View 设置的回调：将 RTF 内容加载到 RichTextBox 中
        /// </summary>
        public Action<string?>? SetCurrentRtfContent { get; set; }

        /// <summary>
        /// 帮助文档 RichTextBox 是否已与当前语言完成加载/同步（仅在 Step 2 有效）。
        /// 防止未进入帮助文档页保存时，用空编辑器覆盖已有 RTF。
        /// </summary>
        private bool _isHelpDocEditorSynced;

        // ==================== 状态 ====================

        /// <summary>
        /// 是否为编辑模式（false = 新建模式）
        /// </summary>
        public bool IsEditMode { get; private set; }

        /// <summary>
        /// 当前编辑的实体是否为官方温压计（编辑模式下保留原标志，新建模式下为 false）
        /// </summary>
        private bool _isOfficial;

        /// <summary>
        /// 官方温压计查看模式：基本信息、公式脚本和帮助文档仅可查看与复制，不可修改。
        /// 开发者模式下编辑官方温压计时允许修改。
        /// </summary>
        [ObservableProperty]
        private bool isContentReadOnly;

        [ObservableProperty]
        private bool isDeveloperMode;

        private Guid _editingEntityId;
        private string _editingPluginId = string.Empty;

        /// <summary>
        /// 编辑器标题
        /// </summary>
        public string EditorTitle
        {
            get
            {
                if (IsEditMode && IsContentReadOnly)
                    return LanguageService.Instance["geo_editor_title_view"]
                           ?? LanguageService.Instance["geo_editor_title_edit"];
                return IsEditMode
                    ? LanguageService.Instance["geo_editor_title_edit"]
                    : LanguageService.Instance["geo_editor_title_new"];
            }
        }

        partial void OnIsContentReadOnlyChanged(bool value)
        {
            OnPropertyChanged(nameof(EditorTitle));
            AddAdditionalFormulaCommand.NotifyCanExecuteChanged();
            RemoveAdditionalFormulaCommand.NotifyCanExecuteChanged();
            NotifyColumnCommands();
            OnPropertyChanged(nameof(CanShowSave));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(IsReadOnly));
            DecreasePatchCommand?.NotifyCanExecuteChanged();
            IncreasePatchCommand?.NotifyCanExecuteChanged();
            SelectAllLanguagesCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 保存成功后的回调
        /// </summary>
        public Action? OnSaved { get; set; }

        /// <summary>
        /// 关闭窗口的回调
        /// </summary>
        public Action? OnCloseRequested { get; set; }

        /// <summary>
        /// 由 View 注入的错误提示回调；未设置时回退到全局消息。
        /// </summary>
        public Action<string>? ShowErrorMessage { get; set; }

        /// <summary>
        /// 由 View 注入的成功提示回调；未设置时回退到全局消息。
        /// </summary>
        public Action<string>? ShowSuccessMessage { get; set; }

        // JS 脚本模板（内容顶格写入，避免把 C# 源码缩进带进编辑器）
        private const string ScriptTemplate = @"/**
 * calculate(args) - Main calculation function
 * Called by ReoGrid cell formulas.
 * @param {number[]} args - Input values from InputColumns (in order)
 * @returns {number} - Calculated temperature value
 */
function calculate(args) {
    // Example: args[0] = first input column value
    // var T_K = 273.15 + some_formula(args[0]);
    // return T_K;
    return 0;
}

/**
 * calculateDetailed(inputs) - Detailed calculation with intermediate steps
 * Called when user selects a data row in the table.
 * @param {number[]} inputs - Same input values as calculate()
 * @returns {Object[]} - Array of step objects:
 *   name, value, desc, descLang?, nameLang?,
 *   isResult?, isHighlight?, isSeparator?, backgroundColor?, collapsed?
 *   desc           - Default description (fallback for all languages)
 *   descLang       - Optional map, e.g. { 'zh-CN': '...', 'en-US': '...' }
 *   nameLang       - Optional localized name (useful for section titles)
 *   isResult       - Mark final result row (light blue)
 *   isHighlight    - Highlight row (light yellow, same as CIPW)
 *   isSeparator    - Section title row (click to collapse/expand following rows)
 *   backgroundColor- Optional custom hex color (#RGB / #RRGGBB), overrides semantic colors
 *   collapsed      - Initial collapse state for separator groups (default false)
 */
function calculateDetailed(inputs) {
    var result = calculate(inputs);
    return [
        {
            name: '—— Intermediate ——',
            nameLang: { 'zh-CN': '—— 中间变量 ——' },
            isSeparator: true
        },
        {
            name: 'Example',
            value: inputs[0],
            desc: 'First input',
            descLang: { 'zh-CN': '第一个输入' }
        },
        {
            name: 'Result',
            value: result.toFixed(2),
            desc: 'Final temperature',
            descLang: { 'zh-CN': '最终温度', 'zh-TW': '最終溫度' },
            isResult: true
        }
    ];
}";

        public GeothermometerEditorViewModel()
        {
            ScriptContent = ScriptTemplate;
            TagParts.CollectionChanged += (_, _) => NotifyPreviewChanged();
            CapabilityParts.CollectionChanged += (_, _) => NotifyPreviewChanged();
            InputColumns.CollectionChanged += (_, _) => NotifyColumnCommands();
            OutputColumns.CollectionChanged += (_, _) => NotifyColumnCommands();
            ReloadCategoryOptions();
            if (bool.TryParse(ConfigHelper.GetConfig(ConfigKeys.DeveloperMode), out bool devMode))
                IsDeveloperMode = devMode;
            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
            WeakReferenceMessenger.Default.Register<DeveloperModeChangedMessage>(this);
            WeakReferenceMessenger.Default.Register<GeoTMineralCategoryUpdatedMessage>(this);

            // 新建模板默认使用软件当前语言作为默认语言；打开已有模板时由 LoadEntity 重新载入。
            _sourceLanguage = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            foreach (var option in AppCultureRegistry.GetOptions(false))
                LanguageCandidates.Add(new EpmaEditorLanguageOption(option.Code, EpmaTemplateEditorViewModel.LanguageName(option.Code)));
            LanguageParts.CollectionChanged += LanguagePartsChanged;
            LoadLanguages(new[] { _sourceLanguage }, new Dictionary<string, string>(), new Dictionary<string, string>());

            // 默认列：一个输入列 + 一个输出列
            AddDefaultColumns();
            InitializeTemplateStatus(newTemplate: true);
        }

        private void AddDefaultColumns()
        {
            var input = new EpmaEditorColumn { Id = "Input1", ExampleValue = "0" };
            var output = new EpmaEditorColumn { Id = "Output1" };
            _layout.Add(input);
            _layout.Add(output);
            InputColumns.Add(input);
            OutputColumns.Add(output);
        }

        private bool _released;

        /// <summary>
        /// 窗口关闭时退订静态语言事件和消息，避免编辑器被静态事件钉住。
        /// </summary>
        public void Release()
        {
            if (_released)
                return;

            _released = true;
            LanguageService.Instance.PropertyChanged -= OnLanguageChanged;
            WeakReferenceMessenger.Default.UnregisterAll(this);
            ReleaseTemplateStatus();
            OnSaved = null;
            OnCloseRequested = null;
            ShowErrorMessage = null;
            ShowSuccessMessage = null;
            OnTagSuggestionsNeedRefresh = null;
            ColumnAdded = null;
            GetCurrentRtfContent = null;
            SetCurrentRtfContent = null;
        }

        /// <summary>
        /// 标签/能力建议列表需要刷新时回调（窗口侧刷新 ComboBox.ItemsSource）。
        /// </summary>
        public Action? OnTagSuggestionsNeedRefresh { get; set; }

        public void Receive(GeoTMineralCategoryUpdatedMessage message)
        {
            ReloadCategoryOptions();
            RefreshTagDisplayNames();
            OnTagSuggestionsNeedRefresh?.Invoke();
        }

        private void ReloadCategoryOptions()
        {
            CategoryOptions.Clear();
            foreach (string key in GeoTCategoryHelper.GetCategoryKeys())
            {
                CategoryOptions.Add(new GeoTCategoryOption(key, GeoTCategoryHelper.GetDisplayName(key)));
            }

            if (!GeoTCategoryHelper.IsValidCategoryKey(SelectedCategory))
                SelectedCategory = GeoTCategoryHelper.DefaultCategoryKey;
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            IsDeveloperMode = message.Value;
            UpdateContentReadOnlyState();
        }

        private void UpdateContentReadOnlyState()
        {
            IsContentReadOnly = _isOfficial && !IsDeveloperMode;
        }

        /// <summary>语言增减后，保证帮助文档当前语言仍然有效。</summary>
        private void RefreshLanguageDisplayOptions()
        {
            string? keep = SelectedLanguage;
            SelectedLanguage = keep != null && AddedLanguages.Contains(keep)
                ? keep
                : AddedLanguages.FirstOrDefault();
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                ReloadCategoryOptions();
                RefreshTagDisplayNames();
                OnPropertyChanged(nameof(EditorTitle));
                NotifyPreviewChanged();
                RefreshTemplateStatus();
            }
        }

        private void RefreshTagDisplayNames()
        {
            var keys = TagParts.Select(t => t.StorageName).ToList();
            TagParts.Clear();
            foreach (var key in keys)
                TagParts.Add(new GeoTTagModel(key));
        }

        private void NotifyPreviewChanged()
        {
            OnPropertyChanged(nameof(PreviewName));
            OnPropertyChanged(nameof(PreviewMetaLine));
            OnPropertyChanged(nameof(PreviewReference));
            OnPropertyChanged(nameof(PreviewTags));
            OnPropertyChanged(nameof(PreviewCapabilities));
            OnPropertyChanged(nameof(PreviewVersion));
        }

        private void NotifyVersionPropertiesChanged()
        {
            OnPropertyChanged(nameof(TemplateVersion));
            OnPropertyChanged(nameof(SavedVersionText));
            OnPropertyChanged(nameof(PreviewVersion));
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

        private bool CanDecreasePatch() => PatchVersion > 0 && !IsContentReadOnly;

        private bool CanIncreasePatch() => PatchVersion < MaxContentPatch && !IsContentReadOnly;

        /// <summary>基本信息页右侧预览面板是否展开。</summary>
        [ObservableProperty]
        private bool isPreviewExpanded = true;

        [RelayCommand]
        private void TogglePreview()
        {
            IsPreviewExpanded = !IsPreviewExpanded;
        }

        partial void OnNameChanged(string value) => NotifyPreviewChanged();
        partial void OnFormulaNameChanged(string? oldValue, string newValue)
        {
            // 未被手动修改过的自动示例公式跟随主公式名更新
            string previous = BuildDefaultOutputFormula(oldValue);
            if (previous.Length == 0) return;
            string current = BuildDefaultOutputFormula(newValue);
            foreach (var output in OutputColumns)
            {
                if (string.Equals(output.ExampleValue, previous, StringComparison.Ordinal))
                    output.ExampleValue = current;
            }
        }
        partial void OnSelectedCategoryChanged(string value) => NotifyPreviewChanged();
        partial void OnAuthorChanged(string value) => NotifyPreviewChanged();
        partial void OnYearChanged(int value) => NotifyPreviewChanged();
        partial void OnReferenceChanged(string value) => NotifyPreviewChanged();
        partial void OnPatchVersionChanged(int value)
        {
            NotifyVersionPropertiesChanged();
            NotifyPreviewChanged();
            DecreasePatchCommand?.NotifyCanExecuteChanged();
            IncreasePatchCommand?.NotifyCanExecuteChanged();
        }

        // ==================== 步骤变更处理 ====================

        partial void OnCurrentStepChanged(int oldValue, int newValue)
        {
            // 离开帮助文档步骤时，保存当前语言的 RTF 内容
            if (oldValue == 2)
            {
                CommitCurrentLanguageRtf(fromLeavingHelpStep: true);
                _isHelpDocEditorSynced = false;
            }

            // 进入帮助文档步骤时，加载当前语言的 RTF 内容
            if (newValue == 2 && SelectedLanguage != null)
            {
                LoadSelectedLanguageIntoEditor();
                _isHelpDocEditorSynced = true;
            }

            RefreshTemplateStatus();
        }

        // ==================== 语言切换处理 ====================

        partial void OnSelectedLanguageChanged(string? oldValue, string? newValue)
        {
            OnPropertyChanged(nameof(SelectedHelpLanguage));

            if (CurrentStep != 2)
            {
                // 语言胶囊的选中项被移除时控件会回写 null，这里补回有效语言
                if (newValue == null && AddedLanguages.Count > 0)
                    SelectedLanguage = AddedLanguages[0];
                RefreshTemplateStatus();
                return;
            }

            // 保存旧语言的 RTF 内容（仅当编辑器已与旧语言同步时）
            if (oldValue != null &&
                _isHelpDocEditorSynced &&
                AddedLanguages.Contains(oldValue))
            {
                CommitLanguageRtf(oldValue);
            }

            // 加载新语言的 RTF 内容
            if (newValue != null)
            {
                LoadSelectedLanguageIntoEditor();
                _isHelpDocEditorSynced = true;
            }
            else
            {
                SetCurrentRtfContent?.Invoke(null);
                _isHelpDocEditorSynced = false;
            }

            RefreshTemplateStatus();
        }

        private void LoadSelectedLanguageIntoEditor()
        {
            if (SelectedLanguage == null)
                return;

            _helpDocuments.TryGetValue(SelectedLanguage, out var content);
            SetCurrentRtfContent?.Invoke(content);
        }

        /// <summary>
        /// 保存当前选中语言的 RTF 内容到字典（仅当处于帮助文档步骤且编辑器已同步）。
        /// </summary>
        private void SaveCurrentLanguageRtf()
        {
            CommitCurrentLanguageRtf(fromLeavingHelpStep: false);
        }

        private void CommitCurrentLanguageRtf(bool fromLeavingHelpStep)
        {
            if (!_isHelpDocEditorSynced || SelectedLanguage == null)
                return;

            if (!fromLeavingHelpStep && CurrentStep != 2)
                return;

            CommitLanguageRtf(SelectedLanguage);
        }

        private void CommitLanguageRtf(string languageCode)
        {
            if (!AddedLanguages.Contains(languageCode) || GetCurrentRtfContent == null)
                return;

            var rtf = GetCurrentRtfContent();
            if (rtf == null)
                return;

            // 仅在编辑器已与该语言同步时才会走到这里，清空文档即视为删除该语言的帮助文档
            _helpDocuments[languageCode] = rtf;
        }

        private static bool IsBlankRtf(string? rtf)
        {
            if (string.IsNullOrWhiteSpace(rtf))
                return true;

            string text = Regex.Replace(rtf, @"\\[a-z]+\d* ?|\\'[0-9a-f]{2}|\{|\}", string.Empty, RegexOptions.IgnoreCase);
            return string.IsNullOrWhiteSpace(text);
        }

        /// <summary>
        /// 提交前收集各语言 RTF 帮助文档（仅含非空内容，且不会从未同步的编辑器覆盖已有文档）。
        /// </summary>
        private Dictionary<string, string> GetHelpDocumentsForSubmit()
        {
            SaveCurrentLanguageRtf();

            var result = new Dictionary<string, string>();
            foreach (var lang in AddedLanguages)
            {
                if (_helpDocuments.TryGetValue(lang, out var rtf) && !IsBlankRtf(rtf))
                    result[lang] = rtf;
            }

            return result;
        }

        // ==================== 标签管理 ====================

        public bool TryAddTag(string text, Dictionary<string, string>? localizedNames = null)
        {
            if (IsContentReadOnly) return false;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string trimmed = text.Trim();
            if (TagParts.Any(t => string.Equals(t.StorageName, trimmed, StringComparison.OrdinalIgnoreCase)))
                return false;

            var invalidChars = Path.GetInvalidFileNameChars();
            if (trimmed.IndexOfAny(invalidChars) >= 0)
            {
                ShowError(LanguageService.Instance["invalid_filename_char"]);
                return false;
            }

            string storageName = ResolveTagStorageName(trimmed, localizedNames);
            TagParts.Add(new GeoTTagModel(storageName));
            return true;
        }

        public void RemoveTag(GeoTTagModel item)
        {
            if (IsContentReadOnly || item == null) return;
            TagParts.Remove(item);
        }

        // ==================== 能力标签管理 ====================

        public bool TryAddCapability(string text)
        {
            if (IsContentReadOnly) return false;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string trimmed = text.Trim();
            var invalidChars = Path.GetInvalidFileNameChars();
            if (trimmed.IndexOfAny(invalidChars) >= 0)
            {
                ShowError(LanguageService.Instance["invalid_filename_char"]);
                return false;
            }

            if (trimmed.Length > GeoTCapabilityHelper.MaxLength
                && !GeoTCapabilityHelper.IsBuiltIn(trimmed))
            {
                ShowError(string.Format(
                    LanguageService.Instance["geo_validate_capability_too_long"] ?? "Capability tag is too long (max {0}).",
                    GeoTCapabilityHelper.MaxLength));
                return false;
            }

            string? storageName = GeoTCapabilityHelper.Normalize(trimmed);
            if (string.IsNullOrEmpty(storageName))
                return false;

            if (CapabilityParts.Any(c => string.Equals(c.StorageName, storageName, StringComparison.OrdinalIgnoreCase)))
                return false;

            CapabilityParts.Add(new GeoTCapabilityModel(storageName));
            return true;
        }

        public void RemoveCapability(GeoTCapabilityModel item)
        {
            if (IsContentReadOnly || item == null) return;
            CapabilityParts.Remove(item);
        }

        /// <summary>
        /// 下拉建议：尚未选用的内置能力标签。
        /// </summary>
        public List<string> GetCapabilitySuggestions()
        {
            return GeoTCapabilityHelper.GetBuiltInSuggestions(
                CapabilityParts.Select(c => c.StorageName));
        }

        private static string ResolveTagStorageName(string text, Dictionary<string, string>? localizedNames)
        {
            if (localizedNames != null &&
                localizedNames.TryGetValue("zh-CN", out var zh) &&
                !string.IsNullOrWhiteSpace(zh))
            {
                return zh.Trim();
            }

            var entry = GeoTMineralCategoryHelper.FindEntry(text);
            if (entry != null &&
                entry.TryGetValue("zh-CN", out var matchedZh) &&
                !string.IsNullOrWhiteSpace(matchedZh))
            {
                return matchedZh.Trim();
            }

            return text.Trim();
        }

        /// <summary>
        /// 下拉建议：官方矿物标签，排除当前模板已选用的标签。
        /// </summary>
        public List<Dictionary<string, string>> GetTagSuggestions()
        {
            var selected = TagParts
                .Select(t => t.StorageName)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (selected.Count == 0)
                return GeoTMineralCategoryHelper.GetAllTagSuggestions();

            return GeoTMineralCategoryHelper.GetAllTagSuggestions()
                .Where(entry => entry != null && !IsTagEntrySelected(entry, selected))
                .ToList();
        }

        private static bool IsTagEntrySelected(
            Dictionary<string, string> entry,
            HashSet<string> selectedStorageNames)
        {
            if (entry.TryGetValue("zh-CN", out var zh) &&
                !string.IsNullOrWhiteSpace(zh) &&
                selectedStorageNames.Contains(zh.Trim()))
            {
                return true;
            }

            foreach (var value in entry.Values)
            {
                if (!string.IsNullOrWhiteSpace(value) &&
                    selectedStorageNames.Contains(value.Trim()))
                {
                    return true;
                }
            }

            return false;
        }

        // ==================== 步骤导航命令 ====================

        [RelayCommand]
        private void NextStep()
        {
            var error = ValidateCurrentStep();
            if (error != null)
            {
                ShowError(error);
                return;
            }
            if (CurrentStep < 2)
                CurrentStep++;
        }

        [RelayCommand]
        private void PrevStep()
        {
            if (CurrentStep > 0)
                CurrentStep--;
        }

        [RelayCommand]
        private void NavigateToSection(int step)
        {
            if (step < 0 || step > 2)
                return;
            CurrentStep = step;
        }

        [RelayCommand]
        private void Cancel()
        {
            OnCloseRequested?.Invoke();
        }

        private string? ValidateCurrentStep()
        {
            return CurrentStep switch
            {
                0 => ValidateStep0(),
                1 => ValidateStep1(),
                _ => null
            };
        }

        private string? ValidateStep0()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return LanguageService.Instance["geo_validate_name_required"];
            if (!GeoTCategoryHelper.IsValidCategoryKey(SelectedCategory))
                return LanguageService.Instance["geo_validate_category_required"];
            if (TagParts.Count == 0)
                return LanguageService.Instance["geo_validate_tags_required"];
            if (string.IsNullOrWhiteSpace(Author))
                return LanguageService.Instance["geo_validate_author_required"];
            if (Year <= 0)
                return LanguageService.Instance["geo_validate_year_required"];
            if (PatchVersion < 0)
                return LanguageService.Instance["geo_validate_version_format"];
            if (string.IsNullOrWhiteSpace(Reference))
                return LanguageService.Instance["geo_validate_reference_required"];
            return null;
        }

        private string? ValidateStep1()
        {
            if (InputColumns.Count == 0)
                return LanguageService.Instance["geo_validate_inputs_required"]
                       ?? LanguageService.Instance["geo_validate_headers_required"];
            if (_layout.Any(column => string.IsNullOrWhiteSpace(column.Id)))
                return LanguageService.Instance["geo_validate_headers_required"];
            if (string.IsNullOrWhiteSpace(ScriptContent))
                return LanguageService.Instance["geo_validate_script_required"];

            return ValidateFormulas();
        }

        private string? ValidateFormulas()
        {
            if (string.IsNullOrWhiteSpace(FormulaName))
                return LanguageService.Instance["geo_validate_formula_name_required"];
            if (FormulaName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                return LanguageService.Instance["geo_validate_formula_name_invalid"];

            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                FormulaName.Trim()
            };

            int rowIndex = 0;
            foreach (var item in AdditionalFormulaItems)
            {
                rowIndex++;
                string formulaName = item.FormulaName?.Trim() ?? string.Empty;
                string functionName = item.FunctionName?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(formulaName) && string.IsNullOrEmpty(functionName))
                    continue;

                if (string.IsNullOrEmpty(formulaName) || string.IsNullOrEmpty(functionName))
                {
                    return string.Format(
                        LanguageService.Instance["geo_validate_additional_formula_incomplete"],
                        rowIndex);
                }

                if (formulaName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                    return LanguageService.Instance["geo_validate_formula_name_invalid"];

                if (!IsValidJsIdentifier(functionName))
                {
                    return string.Format(
                        LanguageService.Instance["geo_validate_additional_function_name_invalid"],
                        functionName);
                }

                if (string.Equals(functionName, MainFunctionNameValue, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(functionName, "calculateDetailed", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format(
                        LanguageService.Instance["geo_validate_additional_function_reserved"],
                        functionName);
                }

                if (!seenNames.Add(formulaName))
                {
                    return string.Format(
                        LanguageService.Instance["geo_msg_formula_name_duplicate"],
                        formulaName,
                        Name);
                }
            }

            return null;
        }

        private static bool IsValidJsIdentifier(string name)
            => !string.IsNullOrEmpty(name) && JsIdentifierRegex.IsMatch(name);

        [RelayCommand(CanExecute = nameof(CanEditAdditionalFormulas))]
        private void AddAdditionalFormula()
        {
            AdditionalFormulaItems.Add(new AdditionalFormulaItemViewModel());
        }

        [RelayCommand(CanExecute = nameof(CanEditAdditionalFormulas))]
        private void RemoveAdditionalFormula(AdditionalFormulaItemViewModel? item)
        {
            if (item == null) return;
            AdditionalFormulaItems.Remove(item);
        }

        private bool CanEditAdditionalFormulas() => !IsContentReadOnly;

        // ==================== 数据列（输入 / 输出） ====================

        private bool CanEditColumns() => !IsContentReadOnly;

        private bool CanRemoveInput(EpmaEditorColumn? column)
            => !IsContentReadOnly && InputColumns.Count > 1 && column != null && InputColumns.Contains(column);

        private bool CanRemoveOutput(EpmaEditorColumn? column)
            => !IsContentReadOnly && OutputColumns.Count > 1 && column != null && OutputColumns.Contains(column);

        private void NotifyColumnCommands()
        {
            AddInputCommand.NotifyCanExecuteChanged();
            AddOutputCommand.NotifyCanExecuteChanged();
            RemoveInputCommand.NotifyCanExecuteChanged();
            RemoveOutputCommand.NotifyCanExecuteChanged();
        }

        private static string NextColumnId(IEnumerable<EpmaEditorColumn> columns, string prefix)
        {
            var existing = columns.Select(column => column.Id).ToHashSet(StringComparer.Ordinal);
            int index = 1;
            while (existing.Contains(prefix + index)) index++;
            return prefix + index;
        }

        private static string ColumnLetters(int index)
        {
            var letters = string.Empty;
            for (int value = index; value >= 0; value = value / 26 - 1)
                letters = (char)('A' + value % 26) + letters;
            return letters;
        }

        /// <summary>
        /// 按当前列顺序生成默认的输出示例公式，如 =MyFormula(A2,B2,C2)。
        /// 没有主公式名或输入列时返回空串。
        /// </summary>
        private string BuildDefaultOutputFormula(string? formulaName = null)
        {
            string formula = (formulaName ?? FormulaName)?.Trim() ?? string.Empty;
            if (formula.Length == 0) return string.Empty;

            var references = new List<string>();
            for (int i = 0; i < _layout.Count; i++)
            {
                if (InputColumns.Contains(_layout[i]))
                    references.Add(ColumnLetters(i) + "2");
            }

            return references.Count == 0
                ? string.Empty
                : $"={formula}({string.Join(",", references)})";
        }

        /// <summary>
        /// 修改列结构；列字母因此变化时，未被手动改过的自动示例公式同步更新。
        /// </summary>
        private void MutateLayout(Action change)
        {
            string before = BuildDefaultOutputFormula();
            var automatic = before.Length == 0
                ? new List<EpmaEditorColumn>()
                : OutputColumns.Where(column => string.Equals(column.ExampleValue, before, StringComparison.Ordinal)).ToList();

            change();

            if (automatic.Count == 0) return;
            string after = BuildDefaultOutputFormula();
            foreach (var column in automatic)
            {
                if (OutputColumns.Contains(column))
                    column.ExampleValue = after;
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditColumns))]
        private void AddInput()
        {
            var column = new EpmaEditorColumn { Id = NextColumnId(_layout, "Input"), ExampleValue = "0" };
            MutateLayout(() =>
            {
                // 新输入列紧跟最后一个输入列，保持“输入在前、输出在后”的习惯顺序
                int lastInput = _layout.FindLastIndex(item => InputColumns.Contains(item));
                _layout.Insert(lastInput + 1, column);
                InputColumns.Add(column);
            });
            ColumnAdded?.Invoke(column, true);
        }

        [RelayCommand(CanExecute = nameof(CanEditColumns))]
        private void AddOutput()
        {
            var column = new EpmaEditorColumn { Id = NextColumnId(_layout, "Output") };
            MutateLayout(() =>
            {
                _layout.Add(column);
                OutputColumns.Add(column);
            });
            column.ExampleValue = BuildDefaultOutputFormula();
            ColumnAdded?.Invoke(column, false);
        }

        [RelayCommand(CanExecute = nameof(CanRemoveInput))]
        private void RemoveInput(EpmaEditorColumn? column)
        {
            if (!CanRemoveInput(column)) return;
            MutateLayout(() =>
            {
                _layout.Remove(column!);
                InputColumns.Remove(column!);
            });
        }

        [RelayCommand(CanExecute = nameof(CanRemoveOutput))]
        private void RemoveOutput(EpmaEditorColumn? column)
        {
            if (!CanRemoveOutput(column)) return;
            MutateLayout(() =>
            {
                _layout.Remove(column!);
                OutputColumns.Remove(column!);
            });
        }

        // ==================== 语言管理 ====================

        /// <summary>
        /// 载入语言列表及各语言文字。languages 第一项为默认语言。
        /// </summary>
        private void LoadLanguages(
            IReadOnlyList<string> languages,
            IReadOnlyDictionary<string, string> names,
            IReadOnlyDictionary<string, string> references)
        {
            static string Wording(IReadOnlyDictionary<string, string> values, string code)
                => values.TryGetValue(code, out var text) ? text ?? string.Empty : string.Empty;

            _switchingLanguage = true;
            try
            {
                LanguageParts.Clear();
                _translations.Clear();

                var codes = languages.Count > 0 ? languages : new[] { _sourceLanguage };
                _activeDefaultLanguage = codes[0];
                for (int i = 0; i < codes.Count; i++)
                    LanguageParts.Add(new LanguageTagModel { Text = codes[i], IsDefault = i == 0 });

                Name = Wording(names, _activeDefaultLanguage);
                Reference = Wording(references, _activeDefaultLanguage);
                foreach (string code in codes.Skip(1))
                {
                    _translations[code] = new GeoTTranslation
                    {
                        Name = Wording(names, code),
                        Reference = Wording(references, code)
                    };
                }
            }
            finally
            {
                _switchingLanguage = false;
            }

            OnPropertyChanged(nameof(DefaultLanguage));
            RefreshLanguageLists();
        }

        /// <summary>语言增减后：更新候选语言状态，并让帮助文档页的语言列表跟随。</summary>
        private void RefreshLanguageLists()
        {
            var added = LanguageParts.Select(part => part.Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in LanguageCandidates)
            {
                candidate.IsAdded = added.Contains(candidate.Code);
                candidate.CanToggle = !candidate.IsAdded || LanguageParts.Count > 1;
            }

            SelectAllLanguagesCommand.NotifyCanExecuteChanged();

            AddedLanguages.Clear();
            foreach (var part in LanguageParts)
                AddedLanguages.Add(part.Text);
            RefreshLanguageDisplayOptions();
        }

        private GeoTTranslation CaptureDefault() => new() { Name = Name, Reference = Reference };

        private GeoTTranslation TranslationOf(string code, GeoTTranslation defaults)
            => code == _activeDefaultLanguage
                ? defaults
                : _translations.TryGetValue(code, out var known) ? known : new GeoTTranslation();

        /// <summary>添加语言；未知语言或已存在时返回 false。</summary>
        public bool TryAddLanguage(string code)
        {
            if (IsContentReadOnly || string.IsNullOrWhiteSpace(code)
                || !AppCultureRegistry.TryNormalize(code, out string normalized))
                return false;
            if (LanguageParts.Any(part => part.Text.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                return false;

            LanguageParts.Add(new LanguageTagModel { Text = normalized });
            return true;
        }

        /// <summary>移除语言（至少保留一种）；已填写的文字会保留，再次添加时恢复。</summary>
        public void RemoveLanguage(LanguageTagModel? language)
        {
            if (IsContentReadOnly || language == null || LanguageParts.Count <= 1) return;
            LanguageParts.Remove(language);
        }

        /// <summary>添加语言弹层中的“全选”：一次加入全部尚未添加的语言。</summary>
        [RelayCommand(CanExecute = nameof(CanSelectAllLanguages))]
        private void SelectAllLanguages()
        {
            if (IsContentReadOnly)
                return;

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

            LanguagePartsChanged(LanguageParts, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private bool CanSelectAllLanguages() =>
            !IsContentReadOnly && LanguageCandidates.Any(candidate => !candidate.IsAdded);

        /// <summary>“添加语言”弹层中点击候选项：已添加则移除，未添加则添加。</summary>
        [RelayCommand]
        private void ToggleLanguage(EpmaEditorLanguageOption? candidate)
        {
            if (IsContentReadOnly || candidate == null) return;

            if (candidate.IsAdded)
                RemoveLanguage(LanguageParts.FirstOrDefault(part => part.Text == candidate.Code));
            else
                TryAddLanguage(candidate.Code);
        }

        // 不依赖参数的 CanExecute：右键菜单常在参数绑定前就已打开。
        [RelayCommand]
        private void SetDefaultLanguage(LanguageTagModel? language)
        {
            if (IsContentReadOnly || language == null) return;
            int index = LanguageParts.IndexOf(language);
            if (index > 0) LanguageParts.Move(index, 0);
        }

        private void LanguagePartsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_bulkLanguageUpdate)
                return;

            if (_switchingLanguage || LanguageParts.Count == 0)
            {
                RefreshTemplateStatus();
                return;
            }

            for (int i = 0; i < LanguageParts.Count; i++)
                LanguageParts[i].IsDefault = i == 0;

            string value = LanguageParts[0].Text;
            if (value != _activeDefaultLanguage)
            {
                // 第一项变了：新默认语言的文字换入名称/参考文献输入框，旧默认语言的文字存入字典。
                _switchingLanguage = true;
                try
                {
                    _translations[_activeDefaultLanguage] = CaptureDefault();
                    var target = _translations.TryGetValue(value, out var known) ? known : new GeoTTranslation();
                    _activeDefaultLanguage = value;
                    Name = target.Name;
                    Reference = target.Reference;
                    OnPropertyChanged(nameof(DefaultLanguage));
                }
                finally
                {
                    _switchingLanguage = false;
                }
            }

            RefreshLanguageLists();
            RefreshTemplateStatus();
        }

        /// <summary>多语言文字表快照：每种语言一列，默认语言在最前。</summary>
        public EpmaLocalizationSheetViewModel CreateLocalizationSheet()
        {
            var languages = LanguageParts
                .Select(part => new CultureOption(part.Text, AppCultureRegistry.GetDisplayName(part.Text)))
                .ToList();
            var defaults = CaptureDefault();

            var rows = new List<EpmaLocalizationRow>
            {
                new(EpmaLocalizationRowKind.Name, EpmaLocalizer.Format("epma_loc_row_name"), languages.Count),
                new(EpmaLocalizationRowKind.Description, LanguageService.Instance["geo_loc_row_reference"] ?? "Reference", languages.Count)
            };

            for (int column = 0; column < languages.Count; column++)
            {
                var wording = TranslationOf(languages[column].Code, defaults);
                rows[0].Values[column] = wording.Name;
                rows[1].Values[column] = wording.Reference;
            }

            return new EpmaLocalizationSheetViewModel(languages, rows, IsContentReadOnly);
        }

        /// <summary>取回多语言文字表中的修改。</summary>
        public void ApplyLocalizationSheet(EpmaLocalizationSheetViewModel sheet)
        {
            if (sheet.IsReadOnly || IsContentReadOnly) return;

            var name = sheet.Rows.FirstOrDefault(row => row.Kind == EpmaLocalizationRowKind.Name);
            var reference = sheet.Rows.FirstOrDefault(row => row.Kind == EpmaLocalizationRowKind.Description);

            for (int column = 1; column < sheet.Languages.Count; column++)
            {
                string code = sheet.Languages[column].Code;
                var previous = _translations.TryGetValue(code, out var known) ? known : new GeoTTranslation();
                _translations[code] = new GeoTTranslation
                {
                    Name = name?.Values[column] ?? previous.Name,
                    Reference = reference?.Values[column] ?? previous.Reference
                };
            }

            if (name != null) Name = name.Values[0];
            if (reference != null) Reference = reference.Values[0];
            RefreshTemplateStatus();
        }

        // ==================== 加载已有实体 ====================

        /// <summary>
        /// 以编辑模式加载已有的温压计实体
        /// </summary>
        public void LoadEntity(GeothermometerEntity entity)
        {
            if (entity == null) return;

            IsEditMode = true;
            _editingEntityId = entity.Id;
            _editingPluginId = entity.PluginId;
            _isOfficial = entity.IsOfficial;
            UpdateContentReadOnlyState();

            // 帮助文档先载入：只有帮助文档的语言也要保留在语言列表里，不能被静默丢弃
            _helpDocuments.Clear();
            _isHelpDocEditorSynced = false;
            if (entity.HelpDocuments != null)
            {
                foreach (var kvp in entity.HelpDocuments)
                    _helpDocuments[kvp.Key] = kvp.Value;
            }

            LoadEntityLanguages(entity);
            SelectedCategory = GeoTCategoryHelper.NormalizeCategoryKey(entity.Category);
            TagParts.Clear();
            foreach (var tag in entity.Tags ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                TagParts.Add(new GeoTTagModel(tag.Trim()));
            }
            CapabilityParts.Clear();
            foreach (var capability in GeoTCapabilityHelper.NormalizeList(entity.Capabilities))
                CapabilityParts.Add(new GeoTCapabilityModel(capability));
            Author = entity.Author;
            Year = entity.Year;
            FormulaName = entity.FormulaName;
            OriginalTemplateVersion = ContentVersionHelper.Normalize(entity.Version);
            PatchVersion = ContentVersionHelper.TryGetPatch(entity.Version, out int patch) ? patch : 0;

            LoadColumns(entity);

            ScriptContent = entity.ScriptContent ?? ScriptTemplate;
            ScriptStatus = string.Empty;
            ScriptHasError = false;

            AdditionalFormulaItems.Clear();
            if (entity.AdditionalFormulas != null)
            {
                foreach (var additionalFormula in entity.AdditionalFormulas)
                {
                    if (string.IsNullOrWhiteSpace(additionalFormula.FormulaName)
                        && string.IsNullOrWhiteSpace(additionalFormula.FunctionName))
                    {
                        continue;
                    }

                    AdditionalFormulaItems.Add(new AdditionalFormulaItemViewModel
                    {
                        FormulaName = additionalFormula.FormulaName ?? string.Empty,
                        FunctionName = additionalFormula.FunctionName ?? string.Empty
                    });
                }
            }

            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(IsEditMode));
            NotifyVersionPropertiesChanged();
            InitializeTemplateStatus(newTemplate: false);
        }

        /// <summary>
        /// 载入模板的多语言设置。新格式模板使用保存的语言列表；旧模板没有语言信息，
        /// 以软件当前语言作为默认语言，名称与参考文献视为该语言的文字。
        /// </summary>
        private void LoadEntityLanguages(GeothermometerEntity entity)
        {
            var languages = (entity.Languages ?? new List<string>())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (languages.Count == 0)
                languages.Add(_sourceLanguage);

            string defaultLanguage = languages[0];
            var names = new Dictionary<string, string>(entity.NameLang ?? new Dictionary<string, string>(), StringComparer.Ordinal);
            var references = new Dictionary<string, string>(entity.ReferenceLang ?? new Dictionary<string, string>(), StringComparer.Ordinal);

            // Name / Reference 本身就是默认语言文字
            if (!names.TryGetValue(defaultLanguage, out var defaultName) || string.IsNullOrWhiteSpace(defaultName))
                names[defaultLanguage] = entity.Name ?? string.Empty;
            if (!references.TryGetValue(defaultLanguage, out var defaultReference) || string.IsNullOrWhiteSpace(defaultReference))
                references[defaultLanguage] = entity.Reference ?? string.Empty;

            // 只有帮助文档的语言也保留为可编辑语言
            foreach (string code in _helpDocuments.Keys)
            {
                if (!languages.Any(item => item.Equals(code, StringComparison.OrdinalIgnoreCase)))
                    languages.Add(code);
            }

            LoadLanguages(languages, names, references);
            SelectedLanguage = AddedLanguages.FirstOrDefault();
        }

        /// <summary>
        /// 把旧格式的 Headers / ExampleRow / InputColumns 拆成输入列与输出列，
        /// 同时记下真实列顺序，保存时原样写回。
        /// </summary>
        private void LoadColumns(GeothermometerEntity entity)
        {
            InputColumns.Clear();
            OutputColumns.Clear();
            _layout.Clear();

            var headers = entity.Headers ?? new List<string>();
            var units = entity.Units ?? new List<string>();
            var exampleRow = entity.ExampleRow ?? new List<string>();
            var inputColumns = entity.InputColumns ?? new List<string>();

            // 与运行时一致：输入列按从左到右的出现顺序匹配（表头可重复）。
            var inputIndices = new HashSet<int>();
            int searchStart = 0;
            foreach (string inputColumn in inputColumns)
            {
                for (int index = searchStart; index < headers.Count; index++)
                {
                    if (!string.Equals(headers[index], inputColumn, StringComparison.Ordinal))
                        continue;

                    inputIndices.Add(index);
                    searchStart = index + 1;
                    break;
                }
            }

            for (int index = 0; index < headers.Count; index++)
            {
                var column = new EpmaEditorColumn
                {
                    Id = headers[index] ?? string.Empty,
                    Unit = index < units.Count ? units[index] ?? string.Empty : string.Empty,
                    ExampleValue = index < exampleRow.Count ? exampleRow[index] ?? string.Empty : string.Empty
                };
                _layout.Add(column);
                if (inputIndices.Contains(index))
                    InputColumns.Add(column);
                else
                    OutputColumns.Add(column);
            }

            // 没有任何列的模板（理论上不会出现）给出默认列，保证表格可编辑
            if (_layout.Count == 0)
                AddDefaultColumns();
        }

        // ==================== 示例测试 ====================

        /// <summary>用示例行中的输入值运行 calculate(args)，不再提供单独的测试输入框。</summary>
        [RelayCommand]
        private void RunExampleTest()
        {
            if (string.IsNullOrWhiteSpace(ScriptContent))
            {
                SetScriptStatus(LanguageService.Instance["geo_msg_script_empty"], true);
                return;
            }

            if (InputColumns.Count == 0)
            {
                SetScriptStatus(LanguageService.Instance["geo_validate_inputs_required"], true);
                return;
            }

            var testInputs = new List<double>();
            foreach (var column in InputColumns)
            {
                if (!InvariantNumberParser.TryParse(column.ExampleValue?.Trim(), out double value))
                {
                    SetScriptStatus(
                        EpmaLocalizer.Format("epma_editor_invalid_number", column.ExampleValue ?? string.Empty),
                        true);
                    return;
                }

                testInputs.Add(value);
            }

            var (success, result, error) = GeothermometerService.TestScript(ScriptContent, testInputs.ToArray());
            if (success)
            {
                SetScriptStatus(
                    EpmaLocalizer.Format("epma_editor_test_passed") + Environment.NewLine +
                    $"calculate([{string.Join(", ", testInputs.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)))}]) = {result}",
                    false);
            }
            else
            {
                SetScriptStatus(string.Format(LanguageService.Instance["geo_msg_test_error"], error), true);
            }
        }

        private void SetScriptStatus(string? message, bool isError)
        {
            _draftNeedsValidation = false;
            ScriptHasError = isError;
            ScriptStatus = message ?? string.Empty;
            RefreshTemplateStatus();
        }

        // ==================== 构建实体 ====================

        private GeothermometerEntity BuildEntity()
        {
            var headers = _layout.Select(column => column.Id.Trim()).ToList();
            var exampleRow = _layout.Select(column => column.ExampleValue?.Trim() ?? string.Empty).ToList();
            var inputColumns = _layout
                .Where(column => InputColumns.Contains(column))
                .Select(column => column.Id.Trim())
                .ToList();

            // 默认语言文字 = Name / Reference；其余语言来自 _translations
            var languages = LanguageParts.Select(part => part.Text).ToList();
            var defaults = CaptureDefault();
            var nameLang = new Dictionary<string, string>(StringComparer.Ordinal);
            var referenceLang = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string code in languages)
            {
                var wording = TranslationOf(code, defaults);
                string nameText = wording.Name?.Trim() ?? string.Empty;
                string referenceText = wording.Reference?.Trim() ?? string.Empty;
                if (nameText.Length > 0) nameLang[code] = nameText;
                if (referenceText.Length > 0) referenceLang[code] = referenceText;
            }

            var entity = new GeothermometerEntity
            {
                Id = IsEditMode ? _editingEntityId : Guid.Empty,
                PluginId = IsEditMode ? _editingPluginId : string.Empty,
                Version = TemplateVersion,
                IsOfficial = _isOfficial,
                Category = GeoTCategoryHelper.NormalizeCategoryKey(SelectedCategory),
                Tags = TagParts.Select(t => t.StorageName).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Capabilities = GeoTCapabilityHelper.NormalizeList(CapabilityParts.Select(c => c.StorageName)),
                // Name / Reference 保持为默认语言文字，旧版软件仍可读取
                Name = Name.Trim(),
                Languages = languages,
                NameLang = nameLang,
                ReferenceLang = referenceLang,
                Author = Author.Trim(),
                Year = Year,
                Reference = Reference.Trim(),
                FormulaName = FormulaName.Trim(),
                Headers = headers,
                // 单位与表头同序；没有任何单位时为空列表（与格式 1.3 之前的模板一致）
                Units = CommaSeparatedListHelper.NormalizeUnits(headers, _layout.Select(column => column.Unit?.Trim() ?? string.Empty).ToList()),
                ExampleRow = exampleRow,
                InputColumns = inputColumns,
                AdditionalFormulas = AdditionalFormulaItems
                    .Select(item => new
                    {
                        FormulaName = item.FormulaName?.Trim() ?? string.Empty,
                        FunctionName = item.FunctionName?.Trim() ?? string.Empty
                    })
                    .Where(item => item.FormulaName.Length > 0 && item.FunctionName.Length > 0)
                    .Select(item => new AdditionalFormula
                    {
                        FormulaName = item.FormulaName,
                        FunctionName = item.FunctionName
                    })
                    .ToList(),
                ScriptContent = ScriptContent,
                HelpDocuments = GetHelpDocumentsForSubmit()
            };

            return entity;
        }

        // ==================== 保存与导出 ====================

        [RelayCommand]
        private void Save()
        {
            if (IsContentReadOnly) return;

            var error = ValidateStep0() ?? ValidateStep1();
            if (error != null)
            {
                ShowError(error);
                return;
            }

            SaveCurrentLanguageRtf();

            try
            {
                var entity = BuildEntity();
                GeothermometerService.SaveEntity(entity);
                ShowSuccess(LanguageService.Instance["geo_msg_save_success"]);
                OnSaved?.Invoke();
                OnCloseRequested?.Invoke();
            }
            catch (Exception ex)
            {
                ShowError(string.Format(LanguageService.Instance["geo_msg_save_failed"], ex.Message));
            }
        }

    }

    public class GeoTCategoryOption
    {
        public GeoTCategoryOption(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }

        public string Key { get; }
        public string DisplayName { get; }
    }

    public class GeoTTagModel
    {
        public GeoTTagModel(string storageName)
        {
            StorageName = storageName.Trim();
        }

        public string StorageName { get; }

        public string DisplayName => GeoTMineralCategoryHelper.GetDisplayName(StorageName);
    }

    public class GeoTCapabilityModel
    {
        public GeoTCapabilityModel(string storageName)
        {
            StorageName = GeoTCapabilityHelper.Normalize(storageName) ?? storageName.Trim();
        }

        public string StorageName { get; }

        public string DisplayName => GeoTCapabilityHelper.GetDisplayName(StorageName);

        public bool IsBuiltIn => GeoTCapabilityHelper.IsBuiltIn(StorageName);

        public string Tooltip => GeoTCapabilityHelper.GetTooltip(StorageName);
    }

    public partial class AdditionalFormulaItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string formulaName = string.Empty;

        [ObservableProperty]
        private string functionName = string.Empty;
    }
}
