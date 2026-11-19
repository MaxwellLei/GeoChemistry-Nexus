using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels
{
    public sealed record DiagramEditorStatusItem(string Message, bool NeedsAttention = true);

    public partial class DiagramPlotEditorViewModel
    {
        private bool _statusInitialized;
        private bool _draftNeedsValidation;
        private ScriptDefinition? _statusScript;

        /// <summary>左侧栏底部的模板状态。全部通过时只保留一条完成提示。</summary>
        public IReadOnlyList<DiagramEditorStatusItem> TemplateStatusItems
        {
            get
            {
                var items = new List<DiagramEditorStatusItem>();
                void Add(string key, params object?[] arguments) =>
                    items.Add(new(EpmaLocalizer.Format(key, arguments)));

                if (LanguageParts.Count == 0)
                    Add("epma_editor_state_required", Label("epma_editor_supported_languages"));
                else
                {
                    var languages = LanguageParts.Select(part => part.Text).ToList();
                    if (languages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != languages.Count)
                        AddText(items, "language_setting_duplicate_found");
                }

                if (CategoryParts.Count < 2)
                    AddText(items, "category_structure_min_two");

                if (Mode == DiagramPlotEditorMode.Create && string.IsNullOrEmpty(SelectedPlotType))
                    Add("epma_editor_state_required", Label("plot_type"));

                if (PatchVersion < 0)
                    AddText(items, "geo_validate_version_format");

                // 分类未满两级时还没有工作模板，脚本和数据列尚未生成，不提前报缺失。
                if (ScriptDefinition != null || DataSeriesColumns.Count > 0)
                {
                    AddDataSeriesStatus(items);
                    string body = ScriptDefinition?.ScriptBody ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(body))
                        Add("epma_editor_state_script_missing");
                    else if (_draftNeedsValidation)
                        Add("epma_editor_state_script_pending");
                    else if (ScriptHasError)
                        Add("epma_editor_state_script_error");
                }

                AddTranslationStatus(items);
                if (items.Count == 0)
                    items.Add(new(EpmaLocalizer.Format("epma_editor_state_ready"), false));
                return items;
            }
        }

        private void InitializeTemplateStatus(bool newTemplate)
        {
            if (!_statusInitialized)
            {
                _statusInitialized = true;
                PropertyChanged += StatusPropertyChanged;
                DataSeriesColumns.CollectionChanged += StatusDataSeriesChanged;
                foreach (var column in DataSeriesColumns)
                    column.PropertyChanged += StatusDataSeriesColumnChanged;
            }

            HookStatusScript();
            _draftNeedsValidation = newTemplate;
            RefreshTemplateStatus();
        }

        private void ReleaseTemplateStatus()
        {
            if (!_statusInitialized)
                return;

            _statusInitialized = false;
            PropertyChanged -= StatusPropertyChanged;
            DataSeriesColumns.CollectionChanged -= StatusDataSeriesChanged;
            foreach (var column in DataSeriesColumns)
                column.PropertyChanged -= StatusDataSeriesColumnChanged;
            if (_statusScript != null)
                _statusScript.PropertyChanged -= StatusScriptChanged;
            _statusScript = null;
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
            if (e.PropertyName is nameof(SelectedPlotType) or nameof(PatchVersion) or nameof(Mode) or nameof(ScriptHasError))
                RefreshTemplateStatus();
        }

        private void StatusDataSeriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var column in DataSeriesColumns)
                {
                    column.PropertyChanged -= StatusDataSeriesColumnChanged;
                    column.PropertyChanged += StatusDataSeriesColumnChanged;
                }
            }
            else
            {
                if (e.OldItems != null)
                {
                    foreach (DiagramDataSeriesColumn column in e.OldItems)
                        column.PropertyChanged -= StatusDataSeriesColumnChanged;
                }

                if (e.NewItems != null)
                {
                    foreach (DiagramDataSeriesColumn column in e.NewItems)
                        column.PropertyChanged += StatusDataSeriesColumnChanged;
                }
            }

            // 重新载入列（Reset / Add）不把已有脚本打回待验证；删列会改变脚本所需数据。
            if (e.Action == NotifyCollectionChangedAction.Remove)
                InvalidateDraftValidation();
            else
                RefreshTemplateStatus();
        }

        private void StatusDataSeriesColumnChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DiagramDataSeriesColumn.Id))
                InvalidateDraftValidation();
        }

        private void HookStatusScript()
        {
            var script = ScriptDefinition;
            if (ReferenceEquals(script, _statusScript))
                return;

            string? previousBody = _statusScript?.ScriptBody;
            if (_statusScript != null)
                _statusScript.PropertyChanged -= StatusScriptChanged;
            _statusScript = script;
            if (script != null)
                script.PropertyChanged += StatusScriptChanged;
            if (_statusInitialized && !string.Equals(previousBody, script?.ScriptBody, StringComparison.Ordinal))
                InvalidateDraftValidation();
        }

        private void StatusScriptChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScriptDefinition.ScriptBody))
                InvalidateDraftValidation();
        }

        private void AddDataSeriesStatus(List<DiagramEditorStatusItem> items)
        {
            var ids = DataSeriesColumns.Select(column => column.Id?.Trim() ?? string.Empty).ToList();
            if (ids.Count == 0 || ids.All(string.IsNullOrEmpty))
            {
                items.Add(new(EpmaLocalizer.Format("epma_editor_state_columns_missing", Label("data_series"))));
                return;
            }

            if (ids.Any(string.IsNullOrEmpty))
                items.Add(new(EpmaLocalizer.Format("epma_editor_state_columns_invalid", Label("data_series"))));

            var named = ids.Where(id => id.Length > 0).ToList();
            if (named.Distinct(StringComparer.OrdinalIgnoreCase).Count() != named.Count)
                items.Add(new(EpmaLocalizer.Format("epma_editor_state_columns_duplicate", Label("data_series"))));
        }

        private void AddTranslationStatus(List<DiagramEditorStatusItem> items)
        {
            var languages = LanguageParts.Select(part => part.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
            if (languages.Count == 0)
                return;

            bool hasHelp = languages.Any(HasHelp);
            for (int index = 0; index < languages.Count; index++)
            {
                var missing = new List<string>();
                if (index > 0)
                {
                    foreach (var row in TranslationDisplayRows)
                    {
                        string source = row.Cells.Count > 0 ? row.Cells[0].Text : string.Empty;
                        string target = index < row.Cells.Count ? row.Cells[index].Text : string.Empty;
                        if (!string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(target) && !string.IsNullOrWhiteSpace(row.Label))
                            missing.Add(row.Label);
                    }
                }

                if (hasHelp && !HasHelp(languages[index]))
                    missing.Add(Label("epma_editor_documents"));

                if (missing.Count == 0)
                    continue;

                items.Add(new(EpmaLocalizer.Format(
                    "epma_editor_state_translation_missing",
                    EpmaTemplateEditorViewModel.LanguageName(languages[index]),
                    string.Join(ListSeparator(), missing.Distinct()))));
            }
        }

        private bool HasHelp(string language) =>
            _helpDocuments.Any(pair =>
                pair.Key.Equals(language, StringComparison.OrdinalIgnoreCase) && !IsBlankRtf(pair.Value));

        private static void AddText(List<DiagramEditorStatusItem> items, string key)
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
