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
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 自定义温压计编辑器 ViewModel
    /// 用于新建和编辑自定义温压计，支持脚本测试和导出
    /// 三步流程：基本信息 → 公式脚本 → 帮助文档
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "WPF binding requires instance members.")]
    public partial class GeothermometerEditorViewModel : ObservableObject,
        IRecipient<DeveloperModeChangedMessage>,
        IRecipient<GeoTMineralCategoryUpdatedMessage>
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

        public ObservableCollection<GeoTCategoryOption> CategoryOptions { get; } = new();

        [ObservableProperty]
        private string author = string.Empty;

        [ObservableProperty]
        private int year = DateTime.Now.Year;

        [ObservableProperty]
        private string reference = string.Empty;

        [ObservableProperty]
        private string formulaName = string.Empty;

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

        /// <summary>版本区域收缩时显示的摘要。</summary>
        public string VersionSectionCollapsedSummary =>
            IsEditMode && !string.IsNullOrWhiteSpace(OriginalTemplateVersion)
                ? $"{OriginalTemplateVersion} → {TemplateVersion}"
                : TemplateVersion;

        [ObservableProperty]
        private bool _isVersionSectionExpanded;

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

        public string PreviewVersion => TemplateVersion;

        // ==================== 表格定义与脚本（Step 1） ====================

        [ObservableProperty]
        private string headersText = string.Empty;

        [ObservableProperty]
        private string exampleRowText = string.Empty;

        [ObservableProperty]
        private string inputColumnsText = string.Empty;

        [ObservableProperty]
        private string scriptContent = string.Empty;

        [ObservableProperty]
        private string testInputText = string.Empty;

        [ObservableProperty]
        private string testResultText = string.Empty;

        [ObservableProperty]
        private bool isTestSuccess;

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
        /// 已添加的语言列表（绑定到 UI 的 ComboBox）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> addedLanguages = new();

        /// <summary>
        /// 当前选中的语言
        /// </summary>
        [ObservableProperty]
        private string? selectedLanguage;

        /// <summary>
        /// 待添加的语言代码
        /// </summary>
        [ObservableProperty]
        private string newLanguageToAdd = "zh-CN";

        [ObservableProperty]
        private IReadOnlyList<CultureOption> _availableLanguageOptions = AppCultureRegistry.GetContentOptions();

        [ObservableProperty]
        private ObservableCollection<CultureOption> _addedLanguageOptions = new();

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
 * @returns {Object[]} - Array of step objects: {name, value, desc, descLang?, isResult}
 *   desc      - Default description (fallback for all languages)
 *   descLang  - Optional map of language code to description, e.g. { 'zh-CN': '...', 'en-US': '...' }
 */
function calculateDetailed(inputs) {
    var result = calculate(inputs);
    return [
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
            ReloadCategoryOptions();
            if (bool.TryParse(ConfigHelper.GetConfig("developer_mode"), out bool devMode))
                IsDeveloperMode = devMode;
            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
            WeakReferenceMessenger.Default.Register<DeveloperModeChangedMessage>(this);
            WeakReferenceMessenger.Default.Register<GeoTMineralCategoryUpdatedMessage>(this);
            RefreshLanguageDisplayOptions();
        }

        public void Receive(GeoTMineralCategoryUpdatedMessage message)
        {
            ReloadCategoryOptions();
            RefreshTagDisplayNames();
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
            RefreshLanguageDisplayOptions();
        }

        private void UpdateContentReadOnlyState()
        {
            IsContentReadOnly = _isOfficial && !IsDeveloperMode;
        }

        private void RefreshLanguageDisplayOptions()
        {
            AvailableLanguageOptions = AppCultureRegistry.GetContentOptions();
            AddedLanguageOptions.Clear();
            foreach (var code in AddedLanguages)
                AddedLanguageOptions.Add(new CultureOption(code, AppCultureRegistry.GetDisplayName(code)));
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                ReloadCategoryOptions();
                RefreshTagDisplayNames();
                OnPropertyChanged(nameof(EditorTitle));
                NotifyPreviewChanged();
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
            OnPropertyChanged(nameof(PreviewVersion));
        }

        private void NotifyVersionPropertiesChanged()
        {
            OnPropertyChanged(nameof(TemplateVersion));
            OnPropertyChanged(nameof(PreviewVersion));
            OnPropertyChanged(nameof(VersionSectionCollapsedSummary));
        }

        [RelayCommand]
        private void ToggleVersionSection()
        {
            IsVersionSectionExpanded = !IsVersionSectionExpanded;
        }

        partial void OnNameChanged(string value) => NotifyPreviewChanged();
        partial void OnSelectedCategoryChanged(string value) => NotifyPreviewChanged();
        partial void OnAuthorChanged(string value) => NotifyPreviewChanged();
        partial void OnYearChanged(int value) => NotifyPreviewChanged();
        partial void OnReferenceChanged(string value) => NotifyPreviewChanged();
        partial void OnPatchVersionChanged(int value)
        {
            NotifyVersionPropertiesChanged();
            NotifyPreviewChanged();
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
        }

        // ==================== 语言切换处理 ====================

        partial void OnSelectedLanguageChanged(string? oldValue, string? newValue)
        {
            if (CurrentStep != 2)
                return;

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

        public static List<Dictionary<string, string>> GetTagSuggestions()
        {
            return GeoTMineralCategoryHelper.GetAllTagSuggestions();
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
            if (string.IsNullOrWhiteSpace(HeadersText))
                return LanguageService.Instance["geo_validate_headers_required"];
            if (string.IsNullOrWhiteSpace(ScriptContent))
                return LanguageService.Instance["geo_validate_script_required"];

            var headers = CommaSeparatedListHelper.Split(HeadersText);
            if (headers.Count == 0)
                return LanguageService.Instance["geo_validate_headers_required"];

            if (!string.IsNullOrWhiteSpace(ExampleRowText))
            {
                var exampleRow = CommaSeparatedListHelper.Split(ExampleRowText);
                if (exampleRow.Count != headers.Count)
                {
                    return string.Format(
                        LanguageService.Instance["geo_validate_example_row_column_mismatch"]
                        ?? "Sample row must have the same number of columns as headers ({0} expected, {1} found).",
                        headers.Count,
                        exampleRow.Count);
                }
            }

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

        // ==================== 语言管理命令 ====================

        [RelayCommand]
        private void AddLanguage()
        {
            if (IsContentReadOnly) return;
            if (string.IsNullOrEmpty(NewLanguageToAdd)) return;

            if (AddedLanguages.Contains(NewLanguageToAdd))
            {
                // 已存在，直接切换
                SelectedLanguage = NewLanguageToAdd;
                return;
            }

            SaveCurrentLanguageRtf();
            AddedLanguages.Add(NewLanguageToAdd);
            _helpDocuments[NewLanguageToAdd] = string.Empty;
            SelectedLanguage = NewLanguageToAdd;
            RefreshLanguageDisplayOptions();
        }

        [RelayCommand]
        private void RemoveLanguage()
        {
            if (IsContentReadOnly) return;
            if (SelectedLanguage == null) return;

            var lang = SelectedLanguage;
            _helpDocuments.Remove(lang);
            AddedLanguages.Remove(lang);
            SelectedLanguage = AddedLanguages.FirstOrDefault();
            RefreshLanguageDisplayOptions();
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

            Name = entity.Name;
            SelectedCategory = GeoTCategoryHelper.NormalizeCategoryKey(entity.Category);
            TagParts.Clear();
            foreach (var tag in entity.Tags ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                TagParts.Add(new GeoTTagModel(tag.Trim()));
            }
            Author = entity.Author;
            Year = entity.Year;
            Reference = entity.Reference;
            FormulaName = entity.FormulaName;
            OriginalTemplateVersion = ContentVersionHelper.Normalize(entity.Version);
            PatchVersion = ContentVersionHelper.TryGetPatch(entity.Version, out int patch) ? patch : 0;

            var headers = entity.Headers ?? new List<string>();
            HeadersText = CommaSeparatedListHelper.Join(headers);
            ExampleRowText = CommaSeparatedListHelper.Join(
                CommaSeparatedListHelper.AlignToHeaderCount(headers, entity.ExampleRow));
            InputColumnsText = CommaSeparatedListHelper.Join(entity.InputColumns);

            ScriptContent = entity.ScriptContent ?? ScriptTemplate;

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

            // 构造测试输入（从示例行提取输入列对应的值）
            if (entity.InputColumns != null && entity.Headers != null && entity.ExampleRow != null)
            {
                var testValues = new List<string>();
                foreach (var col in entity.InputColumns)
                {
                    int idx = entity.Headers.IndexOf(col);
                    if (idx >= 0 && idx < entity.ExampleRow.Count)
                        testValues.Add(entity.ExampleRow[idx]);
                }
                TestInputText = string.Join(", ", testValues);
            }

            // 加载帮助文档
            _helpDocuments.Clear();
            AddedLanguages.Clear();
            _isHelpDocEditorSynced = false;
            if (entity.HelpDocuments != null && entity.HelpDocuments.Count > 0)
            {
                foreach (var kvp in entity.HelpDocuments)
                {
                    _helpDocuments[kvp.Key] = kvp.Value;
                    AddedLanguages.Add(kvp.Key);
                }
                SelectedLanguage = AddedLanguages.FirstOrDefault();
            }

            RefreshLanguageDisplayOptions();
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(IsEditMode));
            NotifyVersionPropertiesChanged();
        }

        // ==================== 脚本测试 ====================

        [RelayCommand]
        private void TestScript()
        {
            if (string.IsNullOrWhiteSpace(ScriptContent))
            {
                TestResultText = LanguageService.Instance["geo_msg_script_empty"];
                IsTestSuccess = false;
                return;
            }

            double[] testInputs;
            try
            {
                testInputs = TestInputText
                    .Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => double.Parse(s.Trim()))
                    .ToArray();
            }
            catch
            {
                TestResultText = LanguageService.Instance["geo_msg_test_input_invalid"];
                IsTestSuccess = false;
                return;
            }

            if (testInputs.Length == 0)
            {
                TestResultText = LanguageService.Instance["geo_msg_test_input_required"];
                IsTestSuccess = false;
                return;
            }

            var (success, result, error) = GeothermometerService.TestScript(ScriptContent, testInputs);
            if (success)
            {
                TestResultText = $"calculate([{TestInputText}]) = {result}";
                IsTestSuccess = true;
            }
            else
            {
                TestResultText = string.Format(LanguageService.Instance["geo_msg_test_error"], error);
                IsTestSuccess = false;
            }
        }

        // ==================== 构建实体 ====================

        private GeothermometerEntity BuildEntity()
        {
            var headers = CommaSeparatedListHelper.Split(HeadersText);
            var exampleRow = CommaSeparatedListHelper.AlignToHeaderCount(
                headers, CommaSeparatedListHelper.Split(ExampleRowText));
            var inputColumns = CommaSeparatedListHelper.Split(InputColumnsText);

            var entity = new GeothermometerEntity
            {
                Id = IsEditMode ? _editingEntityId : Guid.Empty,
                PluginId = IsEditMode ? _editingPluginId : string.Empty,
                Version = TemplateVersion,
                IsOfficial = _isOfficial,
                Category = GeoTCategoryHelper.NormalizeCategoryKey(SelectedCategory),
                Tags = TagParts.Select(t => t.StorageName).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Name = Name.Trim(),
                Author = Author.Trim(),
                Year = Year,
                Reference = Reference.Trim(),
                FormulaName = FormulaName.Trim(),
                Headers = headers,
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

        [RelayCommand]
        private async Task SaveAndExport()
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
                var saved = GeothermometerService.SaveEntity(entity);

                string? filePath = await FileHelper.GetSaveFilePath2Async(
                    title: LanguageService.Instance["geo_msg_export_dialog_title"],
                    filter: FileDialogFilterHelper.GeothermometerPackageFiles,
                    defaultExt: TemplatePackageFileExtensions.GeothermometerPrimary,
                    defaultFileName: Name.Trim());
                if (string.IsNullOrEmpty(filePath)) return;

                var entityId = saved.Id;
                await Task.Run(() => GeothermometerService.ExportToZip(entityId, filePath));
                ShowSuccess(LanguageService.Instance["geo_msg_export_success"]);
                OnSaved?.Invoke();
                OnCloseRequested?.Invoke();
            }
            catch (Exception ex)
            {
                ShowError(string.Format(LanguageService.Instance["geo_msg_export_failed"], ex.Message));
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

    public partial class AdditionalFormulaItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string formulaName = string.Empty;

        [ObservableProperty]
        private string functionName = string.Empty;
    }
}
