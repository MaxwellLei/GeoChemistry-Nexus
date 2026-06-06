using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public enum DiagramPlotEditorMode
    {
        Create,
        Edit
    }

    public partial class DiagramPlotEditorViewModel : ObservableObject, IRecipient<DeveloperModeChangedMessage>
    {
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

        [ObservableProperty]
        private string _translationStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isTranslationSectionEnabled;

        [ObservableProperty]
        private bool _isHelpSectionEnabled;

        [ObservableProperty]
        private string? _selectedHelpLanguage;

        [ObservableProperty]
        private int _patchVersion;

        /// <summary>语言代码 → RTF 帮助文档内容。</summary>
        private readonly Dictionary<string, string> _helpDocuments = new();

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

        /// <summary>侧边栏显示的版本摘要（编辑=当前版本，新建=保存后版本）。</summary>
        public string SidebarVersionText =>
            IsEditMode && !string.IsNullOrWhiteSpace(OriginalTemplateVersion)
                ? OriginalTemplateVersion
                : TemplateVersion;

        /// <summary>保存后将写入的完整模板版本 x.y.z。</summary>
        public string TemplateVersion => ContentVersionHelper.WithAppDiagramFormat(PatchVersion);

        public ObservableCollection<LanguageTagModel> LanguageParts { get; } = new();

        [ObservableProperty]
        private IReadOnlyList<CultureOption> _appLanguageOptions = AppCultureRegistry.GetAppUiOptions();

        public ObservableCollection<CategoryPartModel> CategoryParts { get; } = new();
        public ObservableCollection<PlotTypeOption> PlotTypeOptions { get; } = new();

        private PlotTemplateCategoryConfig? _categoryConfig;
        private GraphMapTemplate? _workingTemplate;
        private GraphMapTemplateEntity? _editEntity;
        private bool _suppressTranslationRebuild;

        [ObservableProperty]
        private string _selectedPlotType = "2D_Plot";

        /// <summary>翻译表默认语言（与 LanguageParts 首项一致）。</summary>
        public string DefaultLanguageCode =>
            LanguageParts.FirstOrDefault()?.Text ?? string.Empty;

        public DiagramPlotEditorViewModel()
        {
            WindowTitle = LanguageService.Instance["new_template"] ?? "New Diagram";
            InitializePlotTypes();
            LoadCategories();
            LanguageParts.CollectionChanged += (_, _) => OnLanguagesChanged();
            CategoryParts.CollectionChanged += (_, _) => RebuildTranslationPreview();
            LanguageService.Instance.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "Item[]")
                {
                    InitializePlotTypes();
                }
            };
            WeakReferenceMessenger.Default.Register<DeveloperModeChangedMessage>(this);
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            RefreshLanguageDisplayNames();
        }

        public void RefreshLanguageDisplayNames()
        {
            AppLanguageOptions = AppCultureRegistry.GetAppUiOptions();
            foreach (var part in LanguageParts)
                part.NotifyDisplayTextChanged();
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
            SelectPlotType("2D_Plot");
            SelectedSectionIndex = 0;
            NotifyVersionPropertiesChanged();
        }

        public void InitializeForEdit(GraphMapTemplateEntity entity)
        {
            if (entity?.Content == null) return;

            var clonedContent = GraphMapTemplateService.CloneTemplateContent(entity.Content);
            if (clonedContent == null) return;

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
                    UpdateLanguageDefaultStatus();
                }

                string defaultLang = _workingTemplate.DefaultLanguage;
                if (_workingTemplate.NodeList?.Translations != null &&
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

            LoadHelpDocumentsFromEntity(entity);
            PatchVersion = ContentVersionHelper.TryGetPatch(entity.Content.Version, out int patch) ? patch : 0;
            ApplyPatchToWorkingTemplate();
            SelectedSectionIndex = 0;
            NotifyVersionPropertiesChanged();
        }

        partial void OnModeChanged(DiagramPlotEditorMode value) => NotifyVersionPropertiesChanged();

        partial void OnOriginalTemplateVersionChanged(string value) => NotifyVersionPropertiesChanged();

        partial void OnSelectedSectionIndexChanged(int oldValue, int newValue)
        {
            if (oldValue == 2)
                SaveCurrentHelpLanguageRtf();

            if (newValue == 2)
            {
                SyncHelpLanguagesWithBasicSettings();
                LoadSelectedHelpLanguageIntoEditor();
            }
        }

        partial void OnSelectedHelpLanguageChanged(string? oldValue, string? newValue)
        {
            if (oldValue != null &&
                LanguageParts.Any(p => p.Text.Equals(oldValue, StringComparison.OrdinalIgnoreCase)) &&
                GetCurrentRtfContent != null)
            {
                var rtf = GetCurrentRtfContent();
                if (rtf != null)
                    _helpDocuments[oldValue] = rtf;
            }

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

        public void ClearAll()
        {
            LanguageParts.Clear();
            CategoryParts.Clear();
            TranslationTable = null;
            IsTranslationSectionEnabled = false;
            IsHelpSectionEnabled = false;
            TranslationStatusMessage = string.Empty;
            PatchVersion = 0;
            _helpDocuments.Clear();
            SelectedHelpLanguage = null;
        }

        partial void OnPatchVersionChanged(int value)
        {
            NotifyVersionPropertiesChanged();
            ApplyPatchToWorkingTemplate();
        }

        private void NotifyVersionPropertiesChanged()
        {
            OnPropertyChanged(nameof(TemplateVersion));
            OnPropertyChanged(nameof(SidebarVersionText));
            OnPropertyChanged(nameof(IsEditMode));
        }

        private void ApplyPatchToWorkingTemplate()
        {
            if (_workingTemplate == null) return;
            _workingTemplate.Version = ContentVersionHelper.WithAppDiagramFormat(PatchVersion);
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
            RebuildTranslationPreview();
        }

        public void UpdateLanguageDefaultStatus()
        {
            for (int i = 0; i < LanguageParts.Count; i++)
                LanguageParts[i].IsDefault = i == 0;
            OnPropertyChanged(nameof(DefaultLanguageCode));
        }

        public bool TryAddLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            code = code.Trim();
            if (!AppCultureRegistry.IsValidAppUiCode(code))
                return false;
            if (LanguageParts.Any(p => p.Text.Equals(code, StringComparison.OrdinalIgnoreCase)))
                return false;

            LanguageParts.Add(new LanguageTagModel { Text = code });
            UpdateLanguageDefaultStatus();
            return true;
        }

        public void RemoveLanguage(LanguageTagModel item)
        {
            if (item == null) return;
            LanguageParts.Remove(item);
            UpdateLanguageDefaultStatus();
        }

        public bool TryAddCategory(string text, Dictionary<string, string>? localizedNames = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (text.IndexOfAny(invalidChars) >= 0)
            {
                HandyControl.Controls.MessageBox.Show(
                    LanguageService.Instance["invalid_filename_char"],
                    LanguageService.Instance["error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

            var dialog = new VistaSaveFileDialog
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
                TranslationStatusMessage = string.Format(
                    LanguageService.Instance["translation_export_success"] ?? "Exported translations to {0}.",
                    dialog.FileName);
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

            var dialog = new VistaOpenFileDialog
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

        [RelayCommand]
        private void SwapTranslationLanguages(Tuple<string, string>? args)
        {
            if (args == null || TranslationTable == null) return;
            string lang1 = args.Item1;
            string lang2 = args.Item2;
            if (!TranslationTable.Columns.Contains(lang1) || !TranslationTable.Columns.Contains(lang2))
                return;

            foreach (DataRow row in TranslationTable.Rows)
            {
                var temp = row[lang1];
                row[lang1] = row[lang2];
                row[lang2] = temp;
            }

            int idx1 = LanguageParts.ToList().FindIndex(p => p.Text == lang1);
            int idx2 = LanguageParts.ToList().FindIndex(p => p.Text == lang2);
            if (idx1 >= 0 && idx2 >= 0)
                LanguageParts.Move(idx1, idx2);

            UpdateLanguageDefaultStatus();
            RefreshTranslationTableBinding();
            SyncWorkingTemplateFromTable();
            TranslationStatusMessage = LanguageService.Instance["swap_language_columns"] ?? "Languages swapped";
        }

        public bool Validate(out string errorMessage)
        {
            if (LanguageParts.Count == 0)
            {
                errorMessage = LanguageService.Instance["all_fields_required"];
                return false;
            }

            if (CategoryParts.Count < 2)
            {
                errorMessage = LanguageService.Instance["category_structure_min_two"];
                return false;
            }

            if (Mode == DiagramPlotEditorMode.Create && string.IsNullOrEmpty(SelectedPlotType))
            {
                errorMessage = LanguageService.Instance["all_fields_required"];
                return false;
            }

            var langs = LanguageParts.Select(l => l.Text).ToList();
            if (langs.Distinct(StringComparer.OrdinalIgnoreCase).Count() != langs.Count)
            {
                errorMessage = LanguageService.Instance["language_setting_duplicate_found"];
                return false;
            }

            if (PatchVersion < 0)
            {
                errorMessage = LanguageService.Instance["geo_validate_version_format"];
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
                template = GraphMapTemplate.CreateDefault(languages, SelectedPlotType, categoryNodeList);
                _workingTemplate = template;
            }

            if (TranslationTable != null)
            {
                DiagramTemplateTranslationHelper.ApplyTranslationTable(TranslationTable, template);
            }
            else
            {
                template.NodeList = categoryNodeList;
                DiagramTemplateTranslationHelper.SyncTemplateLanguages(template, languages);
            }

            template.DefaultLanguage = languages.First();
            template.Version = ContentVersionHelper.WithAppDiagramFormat(PatchVersion);

            return GraphMapTemplateService.CloneTemplateContent(template) ?? template;
        }

        /// <summary>提交前收集各语言 RTF 帮助文档（仅含非空内容）。</summary>
        public Dictionary<string, string> GetHelpDocumentsForSubmit()
        {
            SaveCurrentHelpLanguageRtf();

            var result = new Dictionary<string, string>();
            foreach (var lang in LanguageParts.Select(l => l.Text))
            {
                if (_helpDocuments.TryGetValue(lang, out var rtf) && !string.IsNullOrWhiteSpace(rtf))
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
            UpdateLanguageDefaultStatus();
            SyncHelpLanguagesWithBasicSettings();
            RebuildTranslationPreview();
        }

        private void LoadHelpDocumentsFromEntity(GraphMapTemplateEntity entity)
        {
            _helpDocuments.Clear();
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

            IsHelpSectionEnabled = langs.Count > 0;

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

        private void SaveCurrentHelpLanguageRtf()
        {
            if (SelectedHelpLanguage == null ||
                !LanguageParts.Any(p => p.Text.Equals(SelectedHelpLanguage, StringComparison.OrdinalIgnoreCase)) ||
                GetCurrentRtfContent == null)
            {
                return;
            }

            var rtf = GetCurrentRtfContent();
            if (rtf != null)
                _helpDocuments[SelectedHelpLanguage] = rtf;
        }

        private void RebuildTranslationPreview()
        {
            if (_suppressTranslationRebuild) return;

            var languages = LanguageParts.Select(l => l.Text).ToList();
            IsTranslationSectionEnabled = languages.Count > 0 && CategoryParts.Count >= 2;

            if (!IsTranslationSectionEnabled)
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
            }
            catch (Exception ex)
            {
                TranslationStatusMessage = ex.Message;
            }
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
}
