using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels
{
    public sealed record GeoTEditorStatusItem(string Message, bool NeedsAttention = true);

    public partial class GeothermometerEditorViewModel
    {
        private bool _statusInitialized;
        private bool _draftNeedsValidation;
        private readonly List<EpmaEditorColumn> _statusColumns = new();
        private readonly List<AdditionalFormulaItemViewModel> _statusFormulas = new();

        /// <summary>左侧栏底部的模板状态。全部通过时只保留一条完成提示。</summary>
        public IReadOnlyList<GeoTEditorStatusItem> TemplateStatusItems
        {
            get
            {
                var items = new List<GeoTEditorStatusItem>();
                void Add(string key, params object?[] arguments) =>
                    items.Add(new(EpmaLocalizer.Format(key, arguments)));
                void Required(string? value, string labelKey)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        Add("epma_editor_state_required", Label(labelKey));
                }

                Required(Name, "geo_field_name");
                if (!GeoTCategoryHelper.IsValidCategoryKey(SelectedCategory))
                    Add("epma_editor_state_required", Label("geo_field_category"));
                if (TagParts.Count == 0)
                    Add("epma_editor_state_required", Label("geo_field_tags"));
                Required(Author, "geo_field_author");
                if (Year <= 0)
                    Add("epma_editor_state_required", Label("geo_field_year"));
                if (PatchVersion < 0)
                    AddText(items, "geo_validate_version_format");
                Required(Reference, "geo_field_reference");

                AddColumnStatus(items, InputColumns, "epma_json_field_inputs", required: true);
                AddColumnStatus(items, OutputColumns, "epma_json_field_outputs", required: false);

                if (string.IsNullOrWhiteSpace(ScriptContent))
                    Add("epma_editor_state_script_missing");
                else if (_draftNeedsValidation)
                    Add("epma_editor_state_script_pending");
                else if (ScriptHasError)
                    Add("epma_editor_state_script_error");

                string? formulaIssue = ValidateFormulas();
                if (!string.IsNullOrWhiteSpace(formulaIssue))
                    items.Add(new(formulaIssue));

                AddTranslationStatus(items);
                if (items.Count == 0)
                    items.Add(new(EpmaLocalizer.Format("epma_editor_state_ready"), false));
                return items;
            }
        }

        partial void OnScriptContentChanged(string value) => InvalidateDraftValidation();

        private void InitializeTemplateStatus(bool newTemplate)
        {
            if (!_statusInitialized)
            {
                _statusInitialized = true;
                PropertyChanged += StatusPropertyChanged;
                InputColumns.CollectionChanged += StatusColumnsChanged;
                OutputColumns.CollectionChanged += StatusColumnsChanged;
                AdditionalFormulaItems.CollectionChanged += StatusFormulasChanged;
                TagParts.CollectionChanged += StatusTagsChanged;
                RewireStatusColumns();
                RewireStatusFormulas();
            }

            _draftNeedsValidation = newTemplate;
            RefreshTemplateStatus();
        }

        private void ReleaseTemplateStatus()
        {
            if (!_statusInitialized)
                return;

            _statusInitialized = false;
            PropertyChanged -= StatusPropertyChanged;
            InputColumns.CollectionChanged -= StatusColumnsChanged;
            OutputColumns.CollectionChanged -= StatusColumnsChanged;
            AdditionalFormulaItems.CollectionChanged -= StatusFormulasChanged;
            TagParts.CollectionChanged -= StatusTagsChanged;
            foreach (var column in _statusColumns)
                column.PropertyChanged -= StatusColumnChanged;
            _statusColumns.Clear();
            foreach (var item in _statusFormulas)
                item.PropertyChanged -= StatusFormulaChanged;
            _statusFormulas.Clear();
        }

        private void RefreshTemplateStatus() => OnPropertyChanged(nameof(TemplateStatusItems));

        private void InvalidateDraftValidation()
        {
            if (!_statusInitialized)
                return;

            _draftNeedsValidation = true;
            RefreshTemplateStatus();
        }

        private void StatusPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Name) or nameof(SelectedCategory) or nameof(Author)
                or nameof(Year) or nameof(Reference) or nameof(PatchVersion)
                or nameof(FormulaName) or nameof(ScriptHasError))
                RefreshTemplateStatus();
        }

        private void StatusColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RewireStatusColumns();
            InvalidateDraftValidation();
        }

        private void StatusColumnChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EpmaEditorColumn.Id))
            {
                InvalidateDraftValidation();
                return;
            }

            // 示例测试只读取输入列的示例值；输出列示例公式跟随主公式名变化，不要求重新验证脚本。
            if (e.PropertyName == nameof(EpmaEditorColumn.ExampleValue)
                && sender is EpmaEditorColumn column
                && InputColumns.Contains(column))
                InvalidateDraftValidation();
        }

        private void RewireStatusColumns()
        {
            foreach (var column in _statusColumns)
                column.PropertyChanged -= StatusColumnChanged;
            _statusColumns.Clear();
            foreach (var column in _layout)
            {
                column.PropertyChanged += StatusColumnChanged;
                _statusColumns.Add(column);
            }
        }

        private void StatusFormulasChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RewireStatusFormulas();
            RefreshTemplateStatus();
        }

        private void StatusFormulaChanged(object? sender, PropertyChangedEventArgs e) => RefreshTemplateStatus();

        private void RewireStatusFormulas()
        {
            foreach (var item in _statusFormulas)
                item.PropertyChanged -= StatusFormulaChanged;
            _statusFormulas.Clear();
            foreach (var item in AdditionalFormulaItems)
            {
                item.PropertyChanged += StatusFormulaChanged;
                _statusFormulas.Add(item);
            }
        }

        private void StatusTagsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshTemplateStatus();

        private static void AddColumnStatus(
            List<GeoTEditorStatusItem> items,
            IReadOnlyCollection<EpmaEditorColumn> columns,
            string labelKey,
            bool required)
        {
            string label = Label(labelKey);
            if (columns.Count == 0)
            {
                if (required)
                    items.Add(new(EpmaLocalizer.Format("epma_editor_state_columns_missing", label)));
                return;
            }

            if (columns.Any(column => string.IsNullOrWhiteSpace(column.Id)))
                items.Add(new(EpmaLocalizer.Format("epma_editor_state_columns_invalid", label)));
        }

        private void AddTranslationStatus(List<GeoTEditorStatusItem> items)
        {
            if (LanguageParts.Count == 0)
                return;

            var defaults = CaptureDefault();
            bool hasHelp = LanguageParts.Any(part => HasHelp(part.Text));
            foreach (var part in LanguageParts)
            {
                var missing = new List<string>();
                if (!string.Equals(part.Text, DefaultLanguage, StringComparison.Ordinal))
                {
                    var wording = TranslationOf(part.Text, defaults);
                    if (!string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(wording.Name))
                        missing.Add(Label("geo_field_name"));
                    if (!string.IsNullOrWhiteSpace(Reference) && string.IsNullOrWhiteSpace(wording.Reference))
                        missing.Add(Label("geo_field_reference"));
                }

                if (hasHelp && !HasHelp(part.Text))
                    missing.Add(Label("epma_editor_documents"));

                if (missing.Count == 0)
                    continue;

                items.Add(new(EpmaLocalizer.Format(
                    "epma_editor_state_translation_missing",
                    EpmaTemplateEditorViewModel.LanguageName(part.Text),
                    string.Join(ListSeparator(), missing))));
            }
        }

        private bool HasHelp(string language) =>
            _helpDocuments.Any(pair =>
                pair.Key.Equals(language, StringComparison.OrdinalIgnoreCase) && !IsBlankRtf(pair.Value));

        private static void AddText(List<GeoTEditorStatusItem> items, string key)
        {
            string text = LanguageService.Instance[key];
            if (!string.IsNullOrWhiteSpace(text))
                items.Add(new(text));
        }

        private static string Label(string key)
        {
            string text = LanguageService.Instance[key];
            if (string.IsNullOrWhiteSpace(text))
                text = key;
            return text.Trim().TrimEnd('*').Trim();
        }

        private static string ListSeparator() =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is "zh" or "ja" ? "、" : ", ";
    }
}
