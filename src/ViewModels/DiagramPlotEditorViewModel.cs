using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public enum DiagramPlotEditorMode
    {
        Create,
        Edit
    }

    public partial class DiagramPlotEditorViewModel : ObservableObject
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
        private string _diagramVersionText;

        public ObservableCollection<LanguageTagModel> LanguageParts { get; } = new();
        public ObservableCollection<CategoryPartModel> CategoryParts { get; } = new();
        public ObservableCollection<PlotTypeOption> PlotTypeOptions { get; } = new();

        private PlotTemplateCategoryConfig? _categoryConfig;
        private GraphMapTemplate? _workingTemplate;
        private GraphMapTemplateEntity? _editEntity;

        [ObservableProperty]
        private string _selectedPlotType = "2D_Plot";

        /// <summary>翻译表默认语言（与 LanguageParts 首项一致）。</summary>
        public string DefaultLanguageCode =>
            LanguageParts.FirstOrDefault()?.Text ?? string.Empty;

        public DiagramPlotEditorViewModel()
        {
            WindowTitle = LanguageService.Instance["new_template"] ?? "New Diagram";
            DiagramVersionText = UpdateHelper.GetCurrentVersionFloat().ToString("F1");
            InitializePlotTypes();
            LoadCategories();
            LanguageParts.CollectionChanged += (_, _) => OnLanguagesChanged();
            CategoryParts.CollectionChanged += (_, _) => RebuildTranslationPreview();
        }

        public void InitializeForCreate()
        {
            Mode = DiagramPlotEditorMode.Create;
            WindowTitle = LanguageService.Instance["new_template"] ?? "New Diagram";
            IsPlotTypeSelectionEnabled = true;
            _editEntity = null;
            _workingTemplate = null;
            ClearAll();
            SelectPlotType("2D_Plot");
            SelectedSectionIndex = 0;
        }

        public void InitializeForEdit(GraphMapTemplateEntity entity)
        {
            if (entity?.Content == null) return;

            Mode = DiagramPlotEditorMode.Edit;
            WindowTitle = LanguageService.Instance["edit_diagram_template"] ?? "Edit Diagram";
            IsPlotTypeSelectionEnabled = false;
            _editEntity = entity;
            _workingTemplate = entity.Content;

            ClearAll();

            SelectPlotType(entity.Content.TemplateType == "Ternary" ? "Ternary_Plot" : "2D_Plot");

            if (entity.Content.NodeList?.Translations != null)
            {
                foreach (var lang in entity.Content.NodeList.Translations.Keys)
                    LanguageParts.Add(new LanguageTagModel { Text = lang });
                UpdateLanguageDefaultStatus();
            }

            string defaultLang = entity.Content.DefaultLanguage;
            if (entity.NodeList?.Translations != null &&
                entity.NodeList.Translations.TryGetValue(defaultLang, out var path))
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
            SelectedSectionIndex = 0;
        }

        public void ClearAll()
        {
            LanguageParts.Clear();
            CategoryParts.Clear();
            TranslationTable = null;
            IsTranslationSectionEnabled = false;
            TranslationStatusMessage = string.Empty;
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
        private void AddTranslationLanguage(string? langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode) || TranslationTable == null) return;
            langCode = langCode.Trim();
            if (TranslationTable.Columns.Contains(langCode))
            {
                TranslationStatusMessage = $"{LanguageService.Instance["language_exists"] ?? "Language exists"}: {langCode}";
                return;
            }

            TranslationTable.Columns.Add(langCode, typeof(string));
            foreach (DataRow row in TranslationTable.Rows)
                row[langCode] = string.Empty;

            if (!LanguageParts.Any(p => p.Text.Equals(langCode, StringComparison.OrdinalIgnoreCase)))
            {
                LanguageParts.Add(new LanguageTagModel { Text = langCode });
                UpdateLanguageDefaultStatus();
            }

            RefreshTranslationTableBinding();
            SyncWorkingTemplateFromTable();
            TranslationStatusMessage = $"{LanguageService.Instance["add_language_column"]}: {langCode}";
        }

        [RelayCommand]
        private void RemoveTranslationLanguage(string? langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode) || TranslationTable == null) return;
            if (!TranslationTable.Columns.Contains(langCode)) return;

            TranslationTable.Columns.Remove(langCode);
            var tag = LanguageParts.FirstOrDefault(p => p.Text.Equals(langCode, StringComparison.OrdinalIgnoreCase));
            if (tag != null)
                LanguageParts.Remove(tag);

            UpdateLanguageDefaultStatus();
            RefreshTranslationTableBinding();
            SyncWorkingTemplateFromTable();
            TranslationStatusMessage = $"{LanguageService.Instance["delete_language_column"]}: {langCode}";
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
            if (Mode == DiagramPlotEditorMode.Edit && _workingTemplate != null)
            {
                template = _workingTemplate;
                template.NodeList = categoryNodeList;
                template.DefaultLanguage = languages.First();
            }
            else
            {
                template = GraphMapTemplate.CreateDefault(languages, SelectedPlotType, categoryNodeList);
                _workingTemplate = template;
            }

            if (TranslationTable != null)
                DiagramTemplateTranslationHelper.ApplyTranslationTable(TranslationTable, template);
            else
                DiagramTemplateTranslationHelper.SyncTemplateLanguages(template, languages);

            template.NodeList = categoryNodeList;
            foreach (var lang in languages)
            {
                if (!template.NodeList.Translations.ContainsKey(lang))
                {
                    var parts = CategoryParts.Select(p =>
                        p.LocalizedNames != null && p.LocalizedNames.TryGetValue(lang, out var n) ? n : p.DisplayName);
                    template.NodeList.Translations[lang] = string.Join(" > ", parts);
                }
            }

            return template;
        }

        public void OnTranslationCellChanged()
        {
            SyncWorkingTemplateFromTable();
        }

        private void OnLanguagesChanged()
        {
            UpdateLanguageDefaultStatus();
            RebuildTranslationPreview();
        }

        private void RebuildTranslationPreview()
        {
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
                if (Mode == DiagramPlotEditorMode.Edit && _workingTemplate != null)
                {
                    draft = _workingTemplate;
                    draft.NodeList = categoryNodeList;
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
            }
            catch (Exception ex)
            {
                TranslationStatusMessage = ex.Message;
            }
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
                Description = GetLocalizedDescription(
                    "适合常规 X-Y 坐标绘图，适用于散点、折线、函数等大多数图解模板。",
                    "Best for standard X-Y plotting."),
                SelectedLabel = GetLocalizedDescription("已选", "Selected")
            });
            PlotTypeOptions.Add(new PlotTypeOption
            {
                Key = "Ternary_Plot",
                Badge = "ABC",
                Title = LanguageService.Instance["ternary_phase_diagram"] ?? "Ternary",
                Description = GetLocalizedDescription(
                    "适合三组分配比数据，常用于三元判别图、相图和端元混合关系表达。",
                    "Best for ternary compositions and phase diagrams."),
                SelectedLabel = GetLocalizedDescription("已选", "Selected")
            });
        }

        private static string NormalizePlotType(string plotType) => plotType switch
        {
            "Ternary" => "Ternary_Plot",
            "Cartesian" => "2D_Plot",
            "Ternary_Plot" => "Ternary_Plot",
            _ => "2D_Plot"
        };

        private static string GetLocalizedDescription(string zhCnText, string enText) =>
            LanguageService.CurrentLanguage.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? zhCnText
                : enText;
    }
}
