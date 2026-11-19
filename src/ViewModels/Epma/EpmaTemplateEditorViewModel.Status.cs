using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels;

public sealed record EpmaEditorStatusItem(string Message, bool NeedsAttention = true);

public partial class EpmaTemplateEditorViewModel
{
    private bool _statusInitialized;
    private bool _draftNeedsValidation;
    private static bool ValidEditorIdentifier(string value) => Regex.IsMatch(value.Trim(), @"\A[A-Za-z][A-Za-z0-9_.-]{0,79}\z");
    public IReadOnlyList<EpmaEditorStatusItem> TemplateStatusItems
    {
        get
        {
            var items = new List<EpmaEditorStatusItem>();
            void Add(string key, params object?[] arguments) => items.Add(new(EpmaLocalizer.Format(key, arguments)));
            void Required(string value, string label)
            {
                if (string.IsNullOrWhiteSpace(value)) Add("epma_editor_state_required", EpmaLocalizer.Format(label).Trim().TrimEnd('*').Trim());
            }
            Required(TemplateName, "epma_loc_row_name"); Required(TemplateId, "epma_editor_id"); Required(Version, "epma_editor_version");
            if (!string.IsNullOrWhiteSpace(TemplateId) && !ValidEditorIdentifier(TemplateId)) Add("epma_editor_state_id_invalid");
            void Columns(IReadOnlyCollection<EpmaEditorColumn> columns, string label)
            {
                string name = EpmaLocalizer.Format(label);
                if (columns.Count == 0) Add("epma_editor_state_columns_missing", name);
                else
                {
                    if (columns.Any(column => !ValidEditorIdentifier(column.Id))) Add("epma_editor_state_columns_invalid", name);
                    if (columns.Select(column => column.Id.Trim()).Distinct(System.StringComparer.OrdinalIgnoreCase).Count() != columns.Count)
                        Add("epma_editor_state_columns_duplicate", name);
                }
            }
            Columns(InputColumns, "epma_json_field_inputs"); Columns(OutputColumns, "epma_json_field_outputs");
            var tags = SplitTags(Tags);
            if (tags.Count > MaximumTags) Add("epma_editor_state_tags_limit");
            bool hasHelp = LanguageParts.Any(part => _helpDocuments.TryGetValue(part.Text, out string? document) && !string.IsNullOrWhiteSpace(document));
            foreach (var part in LanguageParts)
            {
                var missing = new List<string>();
                if (part.Text != DefaultLanguage)
                {
                    _translations.TryGetValue(part.Text, out var translation);
                    if (!string.IsNullOrWhiteSpace(TemplateName) && string.IsNullOrWhiteSpace(translation?.Name)) missing.Add(EpmaLocalizer.Format("epma_loc_row_name"));
                    if (!string.IsNullOrWhiteSpace(Description) && string.IsNullOrWhiteSpace(translation?.Description)) missing.Add(EpmaLocalizer.Format("epma_editor_description"));
                    if (tags.Count > 0 && (translation is null || tags.Where((_, index) => index >= translation.Tags.Count || string.IsNullOrWhiteSpace(translation.Tags[index])).Any()))
                        missing.Add(EpmaLocalizer.Format("epma_editor_search_tags"));
                }
                if (hasHelp && (!_helpDocuments.TryGetValue(part.Text, out string? document) || string.IsNullOrWhiteSpace(document))) missing.Add(EpmaLocalizer.Format("epma_editor_documents"));
                if (missing.Count > 0) Add("epma_editor_state_translation_missing", LanguageName(part.Text),
                    string.Join(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is "zh" or "ja" ? "、" : ", ", missing));
            }
            if (string.IsNullOrWhiteSpace(Files.FirstOrDefault(file => file.IsScript)?.Text)) Add("epma_editor_state_script_missing");
            else if (IsBusy) Add(_isScriptRun ? "epma_editor_testing" : "epma_editor_saving");
            else if (_draftNeedsValidation) Add("epma_editor_state_script_pending");
            else if (ScriptHasError) Add("epma_editor_state_script_error");
            if (items.Count == 0) items.Add(new(EpmaLocalizer.Format("epma_editor_state_ready"), false));
            return items;
        }
    }
    private void RefreshTemplateStatus() => OnPropertyChanged(nameof(TemplateStatusItems));
    private void InitializeTemplateStatus(bool newTemplate)
    {
        _draftNeedsValidation = newTemplate; _statusInitialized = true;
        PropertyChanged += StatusPropertyChanged;
        LanguageParts.CollectionChanged += StatusLanguagesChanged;
        foreach (var file in Files) file.PropertyChanged += StatusFileChanged;
    }
    private void ReleaseTemplateStatus()
    {
        PropertyChanged -= StatusPropertyChanged;
        LanguageParts.CollectionChanged -= StatusLanguagesChanged;
        foreach (var file in Files) file.PropertyChanged -= StatusFileChanged;
    }
    private void StatusPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TemplateName) or nameof(TemplateId) or nameof(Version) or nameof(Description) or nameof(Tags) or nameof(IsBusy) or nameof(ScriptHasError)) RefreshTemplateStatus();
    }
    private void StatusLanguagesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshTemplateStatus();
    private void StatusFileChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EpmaEditableFile.Text) && !_syncingExamples) InvalidateDraftValidation();
    }
    private void InvalidateDraftValidation()
    {
        if (!_statusInitialized) return;
        _draftNeedsValidation = true; RefreshTemplateStatus();
    }
    public void UpdateHelpDocumentDraft(string? rtf)
    {
        string? language = _helpEditorLanguage ?? SelectedHelpLanguage;
        if (IsReadOnly || language is null) return;
        if (string.IsNullOrEmpty(rtf)) _helpDocuments.Remove(language);
        else _helpDocuments[language] = rtf;
        RefreshTemplateStatus();
    }
}
