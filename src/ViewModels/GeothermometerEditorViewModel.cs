using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 自定义温度计编辑器 ViewModel
    /// 用于新建和编辑自定义温度计，支持脚本测试和导出
    /// 三步流程：基本信息 → 公式脚本 → 帮助文档
    /// </summary>
    public partial class GeothermometerEditorViewModel : ObservableObject
    {
        // ==================== 步骤导航 ====================

        [ObservableProperty]
        private int currentStep;

        // ==================== 元数据（Step 0） ====================

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string mineral = string.Empty;

        [ObservableProperty]
        private string author = string.Empty;

        [ObservableProperty]
        private int year = DateTime.Now.Year;

        [ObservableProperty]
        private string reference = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private string formulaName = string.Empty;

        [ObservableProperty]
        private string worksheetName = string.Empty;

        [ObservableProperty]
        private string version = "1.0.0";

        public string PreviewName => string.IsNullOrWhiteSpace(Name)
            ? "温度计名称预览"
            : Name.Trim();

        public string PreviewMetaLine
        {
            get
            {
                var mineralText = string.IsNullOrWhiteSpace(Mineral) ? "矿物分类" : Mineral.Trim();
                var authorText = string.IsNullOrWhiteSpace(Author) ? "作者" : Author.Trim();
                var yearText = Year > 0 ? Year.ToString() : DateTime.Now.Year.ToString();
                return $"{mineralText} - {authorText} ({yearText})";
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
            ? "这里将显示参考文献信息"
            : Reference.Trim();

        public string PreviewMineral => string.IsNullOrWhiteSpace(Mineral)
            ? "矿物分类"
            : Mineral.Trim();

        public string PreviewDescription => string.IsNullOrWhiteSpace(Description)
            ? "这里将显示简要描述，帮助你确认列表项生成后的整体内容效果。"
            : Description.Trim();

        public string PreviewVersion => string.IsNullOrWhiteSpace(Version)
            ? "1.0.0"
            : Version.Trim();

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

        // ==================== 帮助文档（Step 2） ====================

        /// <summary>
        /// 内部存储：语言代码 → RTF 内容
        /// </summary>
        private Dictionary<string, string> _helpDocuments = new();

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

        /// <summary>
        /// 所有可选语言代码
        /// </summary>
        public static List<string> AllLanguageCodes { get; } = new()
        {
            "zh-CN", "en-US", "ja-JP", "ko-KR", "de-DE", "fr-FR", "es-ES", "pt-BR", "ru-RU"
        };

        /// <summary>
        /// 由 View 设置的回调：获取当前 RichTextBox 中的 RTF 内容
        /// </summary>
        public Func<string?>? GetCurrentRtfContent { get; set; }

        /// <summary>
        /// 由 View 设置的回调：将 RTF 内容加载到 RichTextBox 中
        /// </summary>
        public Action<string?>? SetCurrentRtfContent { get; set; }

        // ==================== 状态 ====================

        /// <summary>
        /// 是否为编辑模式（false = 新建模式）
        /// </summary>
        public bool IsEditMode { get; private set; }

        /// <summary>
        /// 当前编辑的实体是否为官方温度计（编辑模式下保留原标志，新建模式下为 false）
        /// </summary>
        private bool _isOfficial;

        private Guid _editingEntityId;
        private string _editingPluginId = string.Empty;

        /// <summary>
        /// 编辑器标题
        /// </summary>
        public string EditorTitle => IsEditMode
            ? LanguageService.Instance["geo_editor_title_edit"]
            : LanguageService.Instance["geo_editor_title_new"];

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

        // JS 脚本模板
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
                                                 * @returns {Object[]} - Array of step objects: {name, value, desc, isResult}
                                                 */
                                                function calculateDetailed(inputs) {
                                                    var result = calculate(inputs);
                                                    return [
                                                        { name: 'Result', value: result.toFixed(2), desc: 'Final temperature', isResult: true }
                                                    ];
                                                }";

        public GeothermometerEditorViewModel()
        {
            ScriptContent = ScriptTemplate;
        }

        private void NotifyPreviewChanged()
        {
            OnPropertyChanged(nameof(PreviewName));
            OnPropertyChanged(nameof(PreviewMetaLine));
            OnPropertyChanged(nameof(PreviewReference));
            OnPropertyChanged(nameof(PreviewMineral));
            OnPropertyChanged(nameof(PreviewDescription));
            OnPropertyChanged(nameof(PreviewVersion));
        }

        partial void OnNameChanged(string value) => NotifyPreviewChanged();
        partial void OnMineralChanged(string value) => NotifyPreviewChanged();
        partial void OnAuthorChanged(string value) => NotifyPreviewChanged();
        partial void OnYearChanged(int value) => NotifyPreviewChanged();
        partial void OnReferenceChanged(string value) => NotifyPreviewChanged();
        partial void OnDescriptionChanged(string value) => NotifyPreviewChanged();
        partial void OnVersionChanged(string value) => NotifyPreviewChanged();

        // ==================== 步骤变更处理 ====================

        partial void OnCurrentStepChanged(int oldValue, int newValue)
        {
            // 离开帮助文档步骤时，保存当前语言的 RTF 内容
            if (oldValue == 2)
            {
                SaveCurrentLanguageRtf();
            }

            // 进入帮助文档步骤时，加载当前语言的 RTF 内容
            if (newValue == 2 && SelectedLanguage != null)
            {
                _helpDocuments.TryGetValue(SelectedLanguage, out var content);
                SetCurrentRtfContent?.Invoke(content);
            }
        }

        // ==================== 语言切换处理 ====================

        partial void OnSelectedLanguageChanged(string? oldValue, string? newValue)
        {
            // 保存旧语言的 RTF 内容（仅当语言仍在列表中时）
            if (oldValue != null && AddedLanguages.Contains(oldValue) && GetCurrentRtfContent != null)
            {
                var rtf = GetCurrentRtfContent();
                if (rtf != null)
                    _helpDocuments[oldValue] = rtf;
            }

            // 加载新语言的 RTF 内容
            if (newValue != null)
            {
                _helpDocuments.TryGetValue(newValue, out var content);
                SetCurrentRtfContent?.Invoke(content);
            }
            else
            {
                SetCurrentRtfContent?.Invoke(null);
            }
        }

        /// <summary>
        /// 保存当前选中语言的 RTF 内容到字典
        /// </summary>
        private void SaveCurrentLanguageRtf()
        {
            if (SelectedLanguage != null && AddedLanguages.Contains(SelectedLanguage) && GetCurrentRtfContent != null)
            {
                var rtf = GetCurrentRtfContent();
                if (rtf != null)
                    _helpDocuments[SelectedLanguage] = rtf;
            }
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
            if (string.IsNullOrWhiteSpace(Mineral))
                return LanguageService.Instance["geo_validate_mineral_required"];
            if (string.IsNullOrWhiteSpace(Author))
                return LanguageService.Instance["geo_validate_author_required"];
            if (Year <= 0)
                return LanguageService.Instance["geo_validate_year_required"];
            if (string.IsNullOrWhiteSpace(Version))
                return LanguageService.Instance["geo_validate_version_required"];
            if (!System.Text.RegularExpressions.Regex.IsMatch(Version.Trim(), @"^\d+\.\d+\.\d+$"))
                return LanguageService.Instance["geo_validate_version_format"];
            if (string.IsNullOrWhiteSpace(Reference))
                return LanguageService.Instance["geo_validate_reference_required"];
            if (string.IsNullOrWhiteSpace(FormulaName))
                return LanguageService.Instance["geo_validate_formula_name_required"];
            if (FormulaName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                return LanguageService.Instance["geo_validate_formula_name_invalid"];
            return null;
        }

        private string? ValidateStep1()
        {
            if (string.IsNullOrWhiteSpace(HeadersText))
                return LanguageService.Instance["geo_validate_headers_required"];
            if (string.IsNullOrWhiteSpace(ScriptContent))
                return LanguageService.Instance["geo_validate_script_required"];
            return null;
        }

        // ==================== 语言管理命令 ====================

        [RelayCommand]
        private void AddLanguage()
        {
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
        }

        [RelayCommand]
        private void RemoveLanguage()
        {
            if (SelectedLanguage == null) return;

            var lang = SelectedLanguage;
            _helpDocuments.Remove(lang);
            AddedLanguages.Remove(lang);
            SelectedLanguage = AddedLanguages.FirstOrDefault();
        }

        // ==================== 加载已有实体 ====================

        /// <summary>
        /// 以编辑模式加载已有的温度计实体
        /// </summary>
        public void LoadEntity(GeothermometerEntity entity)
        {
            if (entity == null) return;

            IsEditMode = true;
            _editingEntityId = entity.Id;
            _editingPluginId = entity.PluginId;
            _isOfficial = entity.IsOfficial;

            Name = entity.Name;
            Mineral = entity.Mineral;
            Author = entity.Author;
            Year = entity.Year;
            Reference = entity.Reference;
            Description = entity.Description;
            FormulaName = entity.FormulaName;
            WorksheetName = entity.WorksheetName;
            Version = entity.Version;

            HeadersText = string.Join(", ", entity.Headers ?? new List<string>());
            ExampleRowText = string.Join(", ", entity.ExampleRow ?? new List<string>());
            InputColumnsText = string.Join(", ", entity.InputColumns ?? new List<string>());

            ScriptContent = entity.ScriptContent ?? ScriptTemplate;

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
            if (entity.HelpDocuments != null && entity.HelpDocuments.Count > 0)
            {
                foreach (var kvp in entity.HelpDocuments)
                {
                    _helpDocuments[kvp.Key] = kvp.Value;
                    AddedLanguages.Add(kvp.Key);
                }
                SelectedLanguage = AddedLanguages.FirstOrDefault();
            }

            OnPropertyChanged(nameof(EditorTitle));
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
            var headers = HeadersText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            var exampleRow = ExampleRowText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            var inputColumns = InputColumnsText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            var entity = new GeothermometerEntity
            {
                Id = IsEditMode ? _editingEntityId : Guid.Empty,
                PluginId = IsEditMode ? _editingPluginId : string.Empty,
                Version = string.IsNullOrWhiteSpace(Version) ? "1.0.0" : Version.Trim(),
                IsOfficial = _isOfficial,
                Mineral = Mineral.Trim(),
                Name = Name.Trim(),
                Author = Author.Trim(),
                Year = Year,
                Reference = Reference.Trim(),
                Description = Description.Trim(),
                FormulaName = FormulaName.Trim(),
                WorksheetName = string.IsNullOrWhiteSpace(WorksheetName) ? Name.Trim() : WorksheetName.Trim(),
                Headers = headers,
                ExampleRow = exampleRow,
                InputColumns = inputColumns,
                AdditionalFormulas = new List<AdditionalFormula>(),
                ScriptContent = ScriptContent,
                HelpDocuments = new Dictionary<string, string>(_helpDocuments)
            };

            return entity;
        }

        // ==================== 保存与导出 ====================

        [RelayCommand]
        private void Save()
        {
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

                string filePath = await FileHelper.GetSaveFilePath2Async(
                    title: LanguageService.Instance["geo_msg_export_dialog_title"],
                    filter: "ZIP files (*.zip)|*.zip",
                    defaultExt: ".zip",
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

        [RelayCommand]
        private void Cancel()
        {
            OnCloseRequested?.Invoke();
        }
    }
}
