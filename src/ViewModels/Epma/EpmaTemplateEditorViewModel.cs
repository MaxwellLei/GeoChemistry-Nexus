using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;
using GeoChemistryNexus.Services.Epma.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Messages;
using CommunityToolkit.Mvvm.Messaging;
using LanguageTagModel = GeoChemistryNexus.Models.LanguageTagModel;

namespace GeoChemistryNexus.ViewModels;

public sealed record EpmaTrialValue(string Name, string Value, string Unit);

public partial class EpmaEditorLanguageOption(string code, string displayName) : ObservableObject
{
    public string Code { get; } = code;
    public string DisplayName { get; } = displayName;
    [ObservableProperty] private bool _isAdded;
    [ObservableProperty] private bool _canToggle = true;

    /// <summary>按语言名称或文化代码搜索（忽略大小写）；关键字为空时匹配全部。</summary>
    public bool Matches(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return true;
        string text = keyword.Trim();
        return DisplayName.Contains(text, StringComparison.CurrentCultureIgnoreCase)
            || Code.Contains(text, StringComparison.OrdinalIgnoreCase);
    }
}

public partial class EpmaEditorExampleCell : ObservableObject
{
    [ObservableProperty] private string _value = "";
}

public partial class EpmaEditorColumn : ObservableObject
{
    [ObservableProperty] private string _id = "";
    /// <summary>The mandatory first "Sample" input: its name cannot be changed and the column cannot be removed.</summary>
    [ObservableProperty] private bool _isFixed;
    /// <summary>Optional unit text of the column (empty means no unit). Shown under the column name in the worksheet header.</summary>
    [ObservableProperty] private string _unit = "";
    /// <summary>Unit text when the template was loaded; used to tell whether the user changed it (a stored unit may be a localized object).</summary>
    public string UnitBaseline { get; set; } = "";
    /// <summary>Optional group heading of a column in the template's default language (empty means none); neighbouring columns with the same text share one band.</summary>
    [ObservableProperty] private string _group = "";
    /// <summary>Group heading in every other language, kept here until the template is saved. Edited in the localization table.</summary>
    public Dictionary<string, string> GroupTranslations { get; } = new(StringComparer.Ordinal);
    /// <summary>Group wording per language when the template was loaded; the stored group is rewritten only when the wording differs from it.</summary>
    public Dictionary<string, string> GroupBaseline { get; set; } = new(StringComparer.Ordinal);
    public ObservableCollection<EpmaEditorExampleCell> Examples { get; } = [new()];
    public string[] ExampleValues => Examples.Select(cell => cell.Value).ToArray();
    public string ExampleValue { get => Examples[0].Value; set => Examples[0].Value = value; }
    public EpmaEditorColumn()
    {
        foreach (var cell in Examples) cell.PropertyChanged += ExampleChanged;
        Examples.CollectionChanged += (_, change) =>
        {
            if (change.OldItems is not null)
                foreach (EpmaEditorExampleCell cell in change.OldItems) cell.PropertyChanged -= ExampleChanged;
            if (change.NewItems is not null)
                foreach (EpmaEditorExampleCell cell in change.NewItems) cell.PropertyChanged += ExampleChanged;
            ExampleChanged(this, new(null));
        };
    }
    private void ExampleChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ExampleValue)); OnPropertyChanged(nameof(ExampleValues));
    }
}

public partial class EpmaTemplateEditorViewModel : ObservableObject, IHelpDocEditorHost
{
    public void ShowLanguageSettings() => Step = 0;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Preview))]
    [NotifyPropertyChangedFor(nameof(TemplateIconCount))] private string _templateIcon = "Σ";
    public string TemplateIconCount => StringInfo.ParseCombiningCharacters(TemplateIcon).Length + "/3";
    partial void OnTemplateIconChanged(string value)
    {
        string normalized = EpmaTemplateIcon.Normalize(value);
        if (normalized != value) TemplateIcon = normalized;
    }
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsPreviewVisible))]
    [NotifyPropertyChangedFor(nameof(PreviewActionLabel))] private bool _previewExpanded = true;
    public bool IsPreviewVisible => Step == 0 && PreviewExpanded;
    public string PreviewActionLabel => EpmaLocalizer.Format(PreviewExpanded ? "epma_editor_hide_preview" : "epma_editor_show_preview");
    public string ActivePageTitle => EpmaLocalizer.Format(Step switch { 1 => "epma_editor_nav_script", 2 => "epma_editor_documents", _ => "epma_editor_nav_basic" });
    partial void OnStepChanged(int value) { OnPropertyChanged(nameof(IsPreviewVisible)); OnPropertyChanged(nameof(ActivePageTitle)); }
    [RelayCommand] private void TogglePreview() => PreviewExpanded = !PreviewExpanded;
    public ObservableCollection<EpmaEditorLanguageOption> LanguageCandidates { get; } = [];
    /// <summary>语言显示名：与图解操作界面“切换语言”弹窗共用 <see cref="LanguageService.GetLanguageDisplayName"/>，统一由 AppCultureRegistry 维护。</summary>
    public static string LanguageName(string code) => LanguageService.GetLanguageDisplayName(code);
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Preview))] private string _templateId = ContentPackageIdHelper.CreateNew(false);
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Preview))] private string _templateName = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Preview))] private string _version = "1.0.0";
    private const int MaxContentPatch = 9999;
    [ObservableProperty] private int _patchVersion;
    public string FormatVersionBaseline => ContentVersionHelper.FormatBaseline(ContentVersionHelper.GetEpmaFormatVersion());
    public string TemplateVersion => ContentVersionHelper.WithAppEpmaFormat(PatchVersion);
    private readonly string _originalTemplateVersion;
    /// <summary>保存后版本的展示。与打开时的版本不同时为「原版本 -> 当前版本」。</summary>
    public string SavedVersionText => ContentVersionHelper.FormatSavedVersion(
        _originalTemplateVersion,
        TemplateVersion);
    partial void OnPatchVersionChanged(int value)
    {
        if (value < 0) { PatchVersion = 0; return; }
        if (value > MaxContentPatch) { PatchVersion = MaxContentPatch; return; }
        Version = TemplateVersion;
        OnPropertyChanged(nameof(SavedVersionText));
        DecreasePatchCommand?.NotifyCanExecuteChanged();
        IncreasePatchCommand?.NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute = nameof(CanDecreasePatch))]
    private void DecreasePatch() { if (PatchVersion > 0) PatchVersion--; }
    [RelayCommand(CanExecute = nameof(CanIncreasePatch))]
    private void IncreasePatch() { if (PatchVersion < MaxContentPatch) PatchVersion++; }
    private bool CanDecreasePatch() => CanEdit && PatchVersion > 0;
    private bool CanIncreasePatch() => CanEdit && PatchVersion < MaxContentPatch;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Preview))] private string _author = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Preview))] private string _description = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Preview))] private string _tags = "";
    // TemplateName, Description and Tags above are written in the first language of LanguageParts (the default
    // language); the wording of every other language is kept here.
    private sealed class EpmaTranslation
    {
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public List<string> Tags { get; init; } = [];
    }
    private readonly Dictionary<string, EpmaTranslation> _translations = new(StringComparer.Ordinal);
    private readonly string _sourceLanguage;
    private string _activeDefaultLanguage = "";
    private bool _switchingLanguage;
    /// <summary>批量加入语言时先挂起逐项刷新，结束后再统一刷新。</summary>
    private bool _bulkLanguageUpdate;
    /// <summary>Language tags like the diagram editor: the default language is always first and marked.</summary>
    public ObservableCollection<LanguageTagModel> LanguageParts { get; } = [];
    /// <summary>Languages still available in the add-language box. Already added languages are left out.</summary>
    [ObservableProperty] private IReadOnlyList<CultureOption> _languageOptions = AppCultureRegistry.GetOptions();
    public string DefaultLanguage => _activeDefaultLanguage;

    // ---- Multilingual help document: one RTF document per language set in the basic information page. ----
    private readonly Dictionary<string, string> _helpDocuments = new(StringComparer.OrdinalIgnoreCase);
    private Func<string?>? _readHelpEditor;
    private Action<string?>? _writeHelpEditor;
    // Language whose document is shown in the rich text editor, and the editor's own text right after loading it.
    // A document is stored back only when the editor text differs from that baseline.
    private string? _helpEditorLanguage;
    private string _helpEditorBaseline = "";
    private bool _refreshingHelpLanguages;
    /// <summary>Languages that can carry a help document: the language tags of the basic information page.</summary>
    public ObservableCollection<CultureOption> HelpLanguageOptions { get; } = [];
    [ObservableProperty] private string? _selectedHelpLanguage;

    /// <summary>Called by the help page: the rich text editor reads and writes RTF through these callbacks.</summary>
    public void AttachHelpEditor(Func<string?> read, Action<string?> write)
    {
        _readHelpEditor = read; _writeHelpEditor = write;
        LoadHelpEditor();
    }
    private void LoadHelpEditor()
    {
        if (_writeHelpEditor is null) return;
        _helpEditorLanguage = SelectedHelpLanguage;
        string? rtf = null;
        if (SelectedHelpLanguage is not null) _helpDocuments.TryGetValue(SelectedHelpLanguage, out rtf);
        _writeHelpEditor(rtf);
        _helpEditorBaseline = _readHelpEditor?.Invoke() ?? "";
    }
    private void CommitHelpEditor()
    {
        if (_readHelpEditor is null || _helpEditorLanguage is null) return;
        string rtf = _readHelpEditor() ?? "";
        if (rtf == _helpEditorBaseline) return;
        if (rtf.Length == 0) _helpDocuments.Remove(_helpEditorLanguage);
        else _helpDocuments[_helpEditorLanguage] = rtf;
        _helpEditorBaseline = rtf;
        RefreshTemplateStatus();
    }
    partial void OnSelectedHelpLanguageChanged(string? oldValue, string? newValue)
    {
        if (_refreshingHelpLanguages) return;
        CommitHelpEditor();
        LoadHelpEditor();
    }
    /// <summary>Rebuilds the language switcher after languages were added, removed or reordered.</summary>
    private void RefreshHelpLanguages()
    {
        CommitHelpEditor();
        string? keep = _helpEditorLanguage ?? SelectedHelpLanguage;
        _refreshingHelpLanguages = true;
        try
        {
            HelpLanguageOptions.Clear();
            foreach (var part in LanguageParts)
                HelpLanguageOptions.Add(new CultureOption(part.Text, AppCultureRegistry.GetDisplayName(part.Text)));
            SelectedHelpLanguage = HelpLanguageOptions.Any(option => option.Code == keep) ? keep : HelpLanguageOptions.FirstOrDefault()?.Code;
        }
        finally { _refreshingHelpLanguages = false; }
        if (_helpEditorLanguage != SelectedHelpLanguage) LoadHelpEditor();
    }
    private IReadOnlyDictionary<string, string> HelpDocumentsToSave()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in LanguageParts)
            if (_helpDocuments.TryGetValue(part.Text, out var rtf) && rtf.Length > 0) result[part.Text] = rtf;
        return result;
    }

    private const int MaximumTags = 16;
    [ObservableProperty] private string _status = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasScriptStatus))] private string _scriptStatus = "";
    [ObservableProperty] private bool _scriptHasError;
    public bool HasScriptStatus => !string.IsNullOrWhiteSpace(ScriptStatus);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int _step;
    [ObservableProperty] private bool _isDeveloperMode;
    public ObservableCollection<EpmaEditorColumn> InputColumns { get; } = [];
    public ObservableCollection<EpmaEditorColumn> OutputColumns { get; } = [];
    public ObservableCollection<int> ExampleRows { get; } = [1];
    public const int MaximumExampleRows = 30;
    public bool CanAddExample => CanEdit && ExampleRows.Count < MaximumExampleRows;
    private bool _syncingExamples;
    // Keep the text API for package tooling; the editor binds individual cells.
    public string InputsText
    {
        get => string.Join(", ", InputColumns.Select(column => column.Id));
        set => ReplaceColumns(InputColumns, value);
    }
    public string OutputsText
    {
        get => string.Join(", ", OutputColumns.Select(column => column.Id));
        set => ReplaceColumns(OutputColumns, value);
    }
    private int? _exampleTokenCount;
    public string ExampleRowText
    {
        get => string.Join(", ", InputColumns.Select(column => column.ExampleValue));
        set
        {
            var tokens = ExampleTokens(value);
            for (int i = 0; i < InputColumns.Count; i++) InputColumns[i].ExampleValue = i < tokens.Length ? tokens[i] : "";
            _exampleTokenCount = tokens.Length;
            OnPropertyChanged();
        }
    }
    private void ReplaceColumns(ObservableCollection<EpmaEditorColumn> columns, string text)
    {
        var previous = columns.ToArray();
        while (columns.Count > 0) columns.RemoveAt(columns.Count - 1);
        foreach (string id in text.Split(new[] { ',', '，' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var old = previous.FirstOrDefault(item => item.Id == id);
            var column = new EpmaEditorColumn
            {
                Id = id, Unit = old?.Unit ?? "", UnitBaseline = old?.UnitBaseline ?? "",
                Group = old?.Group ?? "",
            };
            if (old is not null)
            {
                foreach (var pair in old.GroupTranslations) column.GroupTranslations[pair.Key] = pair.Value;
                column.GroupBaseline = new Dictionary<string, string>(old.GroupBaseline, StringComparer.Ordinal);
            }
            column.ExampleValue = old?.ExampleValue ?? "";
            for (int row = 1; row < ExampleRows.Count; row++)
                column.Examples.Add(new() { Value = old is not null && row < old.Examples.Count ? old.Examples[row].Value : "" });
            columns.Add(column);
        }
    }
    private void ColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (EpmaEditorColumn column in e.OldItems) column.PropertyChanged -= ColumnChanged;
        if (e.NewItems is not null)
            foreach (EpmaEditorColumn column in e.NewItems) column.PropertyChanged += ColumnChanged;
        ColumnChanged(sender, new PropertyChangedEventArgs(null));
        NotifyColumnCommands();
    }
    private void ColumnChanged(object? sender, PropertyChangedEventArgs e)
    {
        _exampleTokenCount = null;
        InvalidateDraftValidation();
        OnPropertyChanged(nameof(InputsText)); OnPropertyChanged(nameof(OutputsText)); OnPropertyChanged(nameof(ExampleRowText));
    }
    private static string NextColumnId(ObservableCollection<EpmaEditorColumn> columns, string prefix)
    {
        int index = 1;
        while (columns.Any(column => column.Id == prefix + index)) index++;
        return prefix + index;
    }
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void AddInput()
    {
        var column = new EpmaEditorColumn { Id = NextColumnId(InputColumns, "Input"), ExampleValue = "0" };
        for (int row = 1; row < ExampleRows.Count; row++) column.Examples.Add(new() { Value = "0" });
        InputColumns.Add(column);
    }
    [RelayCommand(CanExecute = nameof(CanAddExample))]
    private void AddExample()
    {
        if (!CanAddExample) return;
        foreach (var column in InputColumns) column.Examples.Add(new() { Value = column.IsFixed ? "Example" : "0" });
        ExampleRows.Add(ExampleRows.Count + 1);
        NotifyExampleCommands();
    }
    private bool CanRemoveExample(int number) => CanEdit && ExampleRows.Count > 1 && ExampleRows.Contains(number);
    [RelayCommand(CanExecute = nameof(CanRemoveExample))]
    private void RemoveExample(int number)
    {
        if (!CanRemoveExample(number)) return;
        int index = ExampleRows.IndexOf(number);
        foreach (var column in InputColumns) column.Examples.RemoveAt(index);
        ExampleRows.RemoveAt(index);
        for (int row = index; row < ExampleRows.Count; row++) ExampleRows[row] = row + 1;
        NotifyExampleCommands();
    }
    /// <summary>把从 Excel 等复制的多行多列内容按行列铺开写入输入/输出表格，列或示例行不够时自动补足。</summary>
    public void PasteGrid(EditorGridTable table, int row, int column, string[][] cells)
    {
        if (!CanEdit) return;
        EditorColumnGridPasteHelper.Apply(table, row, column, cells, InputColumns, OutputColumns, ExampleRows,
            MaximumExampleRows, AddInput, AddOutput, AddExample);
    }
    private void NotifyExampleCommands()
    {
        OnPropertyChanged(nameof(CanAddExample)); AddExampleCommand.NotifyCanExecuteChanged();
        RemoveExampleCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void AddOutput() => OutputColumns.Add(new() { Id = NextColumnId(OutputColumns, "Output") });
    private bool CanRemoveInput(EpmaEditorColumn? column) => CanEdit && InputColumns.Count > 1 && column is { IsFixed: false } && InputColumns.Contains(column);
    private bool CanRemoveOutput(EpmaEditorColumn? column) => CanEdit && OutputColumns.Count > 1 && column is not null && OutputColumns.Contains(column);
    [RelayCommand(CanExecute = nameof(CanRemoveInput))]
    private void RemoveInput(EpmaEditorColumn? column) { if (CanRemoveInput(column)) InputColumns.Remove(column!); }
    [RelayCommand(CanExecute = nameof(CanRemoveOutput))]
    private void RemoveOutput(EpmaEditorColumn? column) { if (CanRemoveOutput(column)) OutputColumns.Remove(column!); }
    private void NotifyColumnCommands()
    {
        AddInputCommand.NotifyCanExecuteChanged(); AddOutputCommand.NotifyCanExecuteChanged();
        RemoveInputCommand.NotifyCanExecuteChanged(); RemoveOutputCommand.NotifyCanExecuteChanged();
        NotifyExampleCommands();
    }
    public bool IsReadOnly => _original is { IsCustom: false } && !IsDeveloperMode;
    public bool IsEditing => _original is not null;
    public bool CanEdit => !IsBusy && !IsReadOnly;
    public bool CanNavigate => !IsBusy;
    public bool CanShowSave => !IsReadOnly;
    public string ModuleVersionLabel => "v" + FormatVersionBaseline;
    public string EditorTitle => EpmaLocalizer.Format(IsEditing ? "epma_editor_edit_title" : "epma_editor_title");
    public EpmaTemplateDetails Preview
    {
        get
        {
            var original = _original?.Package.Manifest;
            if (IsReadOnly && original is not null) return EpmaTemplateDetails.FromManifest(original);
            var originalTags = original?.Tags.Select(EpmaScriptText.Format).ToArray();
            var tags = originalTags is not null && Tags == string.Join(", ", originalTags) ? originalTags
                : Tags.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string? author = Author.Trim();
            if (original is not null && author == (original.Author ?? "")) author = original.Author;
            return new(TemplateId.Trim(), TemplateName.Trim(), Version.Trim(), Description.Trim(), tags, author,
                DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), TemplateIcon);
        }
    }
    public ObservableCollection<EpmaEditableFile> Files { get; } = [];
    public EpmaEditableFile ScriptFile => Files.Single(file => file.IsScript);
    private EpmaInstalledTemplate? _original;
    private readonly EpmaPackageEditorDocument _document;
    private readonly string _definitionSnapshot;
    private readonly EpmaScriptManifest _sourceManifest;
    private readonly EpmaTemplateCatalog _catalog;
    public ObservableCollection<EpmaTrialValue> TrialValues { get; } = [];
    public EpmaInstalledTemplate? CreatedTemplate { get; private set; }
    public event Action? Saved;
    private CancellationTokenSource? _operation;
    public EpmaTemplateEditorViewModel(EpmaInstalledTemplate? template = null, EpmaTemplateCatalog? catalog = null)
    {
        _catalog = catalog ?? EpmaCompositionRoot.Templates;
        _original = template;
        IsDeveloperMode = AppCultureRegistry.IsDeveloperModeEnabled();
        WeakReferenceMessenger.Default.Register<EpmaTemplateEditorViewModel, DeveloperModeChangedMessage>(this, static (recipient, message) => recipient.IsDeveloperMode = message.Value);
        var package = template?.Package ?? EpmaTemplateBuilder.CreateStarter(TemplateId);
        var m = package.Manifest;
        TemplateIcon = m.Icon;
        foreach (var language in AppCultureRegistry.GetOptions(false))
            LanguageCandidates.Add(new(language.Code, LanguageName(language.Code)));
        _sourceManifest = m;
        _document = new(package);
        TemplateId = m.PackageId; Author = m.Author ?? "";
        _originalTemplateVersion = template is null ? string.Empty : ContentVersionHelper.Normalize(m.Version);
        int patch = ContentVersionHelper.TryGetPatch(m.Version, out int parsed) ? parsed : 0;
        PatchVersion = Math.Clamp(patch, 0, MaxContentPatch);
        Version = ContentVersionHelper.WithAppEpmaFormat(PatchVersion);
        // A new template starts in the software language; an opened one keeps the languages it was saved with.
        _sourceLanguage = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
        foreach (var document in EpmaHelpDocuments.Read(m.Help)) _helpDocuments[document.Key] = document.Value;
        LoadLanguages(template is null ? null : EpmaBasicLocalization.FromManifest(m, _sourceLanguage));
        _definitionSnapshot = _document.DefinitionJson;
        foreach (var file in _document.Files) Files.Add(file);
        InputColumns.CollectionChanged += ColumnsChanged;
        OutputColumns.CollectionChanged += ColumnsChanged;
        LoadColumns(m);
        Files.Single(file => file.Path == "examples.json").PropertyChanged += ExamplesFileChanged;
        InitializeTemplateStatus(template is null);
    }
    private void LoadLanguages(EpmaBasicLocalization? localization)
    {
        _switchingLanguage = true;
        _activeDefaultLanguage = localization?.DefaultLanguage ?? _sourceLanguage;
        LanguageParts.Add(new LanguageTagModel { Text = _activeDefaultLanguage, IsDefault = true });
        if (localization is not null)
        {
            string defaultCode = _activeDefaultLanguage;
            static string Wording(Dictionary<string, string> values, string code) => values.TryGetValue(code, out var text) ? text : "";
            TemplateName = Wording(localization.Name, defaultCode);
            Description = Wording(localization.Description, defaultCode);
            // A tag without wording in the default language shows its first available wording instead.
            Tags = string.Join(", ", localization.Tags.Select(tag => tag.TryGetValue(defaultCode, out var text) ? text : tag.Values.FirstOrDefault() ?? ""));
            foreach (string code in localization.Languages.Skip(1))
            {
                _translations[code] = new EpmaTranslation
                {
                    Name = Wording(localization.Name, code), Description = Wording(localization.Description, code),
                    Tags = localization.Tags.Select(tag => Wording(tag, code)).ToList(),
                };
                LanguageParts.Add(new LanguageTagModel { Text = code });
            }
        }
        // A language that only has a help document stays editable instead of silently dropping the document.
        foreach (string code in _helpDocuments.Keys)
            if (!LanguageParts.Any(part => part.Text.Equals(code, StringComparison.OrdinalIgnoreCase)))
                LanguageParts.Add(new LanguageTagModel { Text = code });
        _switchingLanguage = false;
        LanguageParts.CollectionChanged += LanguagePartsChanged;
        LanguageParts.CollectionChanged += (_, _) =>
        {
            if (_bulkLanguageUpdate) return;
            RefreshAvailableLanguageOptions();
            RefreshHelpLanguages();
        };
        RefreshAvailableLanguageOptions();
        RefreshHelpLanguages();
    }
    /// <summary>The add-language box lists only languages that are not already on the template.</summary>
    private void RefreshAvailableLanguageOptions()
    {
        var added = LanguageParts.Select(part => part.Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
        LanguageOptions = AppCultureRegistry.GetOptions().Where(option => !added.Contains(option.Code)).ToList();
        foreach (var candidate in LanguageCandidates)
        {
            candidate.IsAdded = added.Contains(candidate.Code);
            candidate.CanToggle = !candidate.IsAdded || LanguageParts.Count > 1;
        }
        SelectAllLanguagesCommand.NotifyCanExecuteChanged();
    }
    private static List<string> SplitTags(string text)
        => text.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    private EpmaTranslation CaptureDefault() => new() { Name = TemplateName, Description = Description, Tags = SplitTags(Tags) };
    /// <summary>Adds a language typed or picked in the language box; false when it is unknown or already present.</summary>
    public bool TryAddLanguage(string code)
    {
        if (!CanEdit || string.IsNullOrWhiteSpace(code) || !AppCultureRegistry.TryNormalize(code, out string normalized)) return false;
        if (LanguageParts.Any(part => part.Text.Equals(normalized, StringComparison.OrdinalIgnoreCase))) return false;
        LanguageParts.Add(new LanguageTagModel { Text = normalized });
        return true;
    }
    public void RemoveLanguage(LanguageTagModel? language)
    {
        // The last language stays; wording of a removed one is kept, so adding it again brings it back.
        if (!CanEdit || language is null || LanguageParts.Count <= 1) return;
        LanguageParts.Remove(language);
    }
    /// <summary>Add-language popup “select all”: adds every language that is not on the template yet.</summary>
    [RelayCommand(CanExecute = nameof(CanSelectAllLanguages))]
    private void SelectAllLanguages()
    {
        if (!CanEdit) return;
        var pending = LanguageCandidates.Where(candidate => !candidate.IsAdded).Select(candidate => candidate.Code).ToList();
        if (pending.Count == 0) return;

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
        RefreshAvailableLanguageOptions();
        RefreshHelpLanguages();
    }
    private bool CanSelectAllLanguages() => CanEdit && LanguageCandidates.Any(candidate => !candidate.IsAdded);
    /// <summary>Click on a candidate in the add-language popup: removes it when already added, adds it otherwise.</summary>
    [RelayCommand]
    private void ToggleLanguage(EpmaEditorLanguageOption? candidate)
    {
        if (!CanEdit || candidate is null) return;
        if (candidate.IsAdded) RemoveLanguage(LanguageParts.FirstOrDefault(part => part.Text == candidate.Code));
        else TryAddLanguage(candidate.Code);
    }
    // Not using a CanExecute that depends on the parameter: context menus often open before it is bound.
    [RelayCommand]
    private void SetDefaultLanguage(LanguageTagModel? language)
    {
        if (!CanEdit || language is null) return;
        int index = LanguageParts.IndexOf(language);
        if (index > 0) LanguageParts.Move(index, 0);
    }
    private void LanguagePartsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_bulkLanguageUpdate || _switchingLanguage || LanguageParts.Count == 0) return;
        for (int i = 0; i < LanguageParts.Count; i++) LanguageParts[i].IsDefault = i == 0;
        string value = LanguageParts[0].Text;
        if (value == _activeDefaultLanguage) return;
        // The first language changed: its wording moves into the name, description and tags boxes.
        _switchingLanguage = true;
        try
        {
            string previous = _activeDefaultLanguage;
            var captured = CaptureDefault();
            _translations[previous] = captured;
            var target = _translations.TryGetValue(value, out var known) ? known : new EpmaTranslation();
            // Keep every tag: positions the new default language has not translated yet reuse the previous wording.
            var tags = target.Tags.ToList();
            for (int i = tags.Count; i < captured.Tags.Count; i++) tags.Add(captured.Tags[i]);
            _activeDefaultLanguage = value;
            TemplateName = target.Name; Description = target.Description; Tags = string.Join(", ", tags);
            foreach (var column in InputColumns.Concat(OutputColumns)) EpmaColumnGroupEditing.SwitchDefault(column, previous, value);
            OnPropertyChanged(nameof(DefaultLanguage));
        }
        finally { _switchingLanguage = false; }
    }
    private EpmaBasicLocalization BuildLocalization()
    {
        var languages = LanguageParts.Select(part => part.Text).ToList();
        var defaults = CaptureDefault();
        EpmaTranslation Of(string code) => code == DefaultLanguage ? defaults : _translations.TryGetValue(code, out var known) ? known : new();
        var result = new EpmaBasicLocalization { Languages = languages, SourceLanguage = _sourceLanguage };
        foreach (string code in languages) { result.Name[code] = Of(code).Name; result.Description[code] = Of(code).Description; }
        for (int i = 0; i < defaults.Tags.Count; i++)
        {
            var tag = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string code in languages)
                if (Of(code).Tags is var tags && i < tags.Count) tag[code] = tags[i];
            result.Tags.Add(tag);
        }
        return result;
    }
    /// <summary>Snapshot for the localization table: one column per language, the default language first.</summary>
    public EpmaLocalizationSheetViewModel CreateLocalizationSheet()
    {
        var languages = LanguageParts.Select(part => new CultureOption(part.Text, AppCultureRegistry.GetDisplayName(part.Text))).ToList();
        var defaults = CaptureDefault();
        EpmaTranslation Of(string code) => code == DefaultLanguage ? defaults : _translations.TryGetValue(code, out var known) ? known : new();
        // Name and description always have a row, so a new template with nothing filled in yet still has cells to type into.
        // Tags and groups only exist where the basic information page or the column tables already hold default wording.
        var rows = new List<EpmaLocalizationRow>
        {
            // Row labels reuse the basic information page wording (template name / description); the page marks the name as required with "*".
            new(EpmaLocalizationRowKind.Name, EpmaLocalizer.Format("epma_editor_name").TrimEnd('*', ' '), languages.Count),
            new(EpmaLocalizationRowKind.Description, EpmaLocalizer.Format("epma_editor_description"), languages.Count),
        };
        for (int i = 0; i < defaults.Tags.Count; i++)
            rows.Add(new(EpmaLocalizationRowKind.Tag, EpmaLocalizer.Format("epma_loc_row_tag", i + 1), languages.Count));
        for (int column = 0; column < languages.Count; column++)
        {
            var wording = Of(languages[column].Code);
            int tagIndex = 0;
            foreach (var row in rows)
                row.Values[column] = row.Kind switch
                {
                    EpmaLocalizationRowKind.Name => wording.Name,
                    EpmaLocalizationRowKind.Description => wording.Description,
                    _ => tagIndex < wording.Tags.Count ? wording.Tags[tagIndex++] : "",
                };
        }
        EpmaColumnGroupEditing.AddSheetRows(rows, languages, DefaultLanguage, InputColumns.Concat(OutputColumns));
        return new EpmaLocalizationSheetViewModel(languages, rows, IsReadOnly);
    }
    private void ApplyGroupRows(EpmaLocalizationSheetViewModel sheet) => EpmaColumnGroupEditing.ApplySheetRows(sheet, InputColumns.Concat(OutputColumns));
    /// <summary>Takes the table back. Clearing a default-language tag removes that tag in every language.</summary>
    public void ApplyLocalizationSheet(EpmaLocalizationSheetViewModel sheet)
    {
        if (sheet.IsReadOnly) return;
        var name = sheet.Rows.FirstOrDefault(row => row.Kind == EpmaLocalizationRowKind.Name);
        var description = sheet.Rows.FirstOrDefault(row => row.Kind == EpmaLocalizationRowKind.Description);
        bool hasTagRows = sheet.Rows.Any(row => row.Kind == EpmaLocalizationRowKind.Tag);
        var tags = sheet.Rows.Where(row => row.Kind == EpmaLocalizationRowKind.Tag && !string.IsNullOrWhiteSpace(row.Values[0])).ToList();
        for (int column = 1; column < sheet.Languages.Count; column++)
        {
            // Items that had no row keep their stored wording.
            var previous = _translations.TryGetValue(sheet.Languages[column].Code, out var known) ? known : new EpmaTranslation();
            _translations[sheet.Languages[column].Code] = new EpmaTranslation
            {
                Name = name?.Values[column] ?? previous.Name,
                Description = description?.Values[column] ?? previous.Description,
                Tags = hasTagRows ? tags.Select(row => row.Values[column]).ToList() : previous.Tags,
            };
        }
        if (name is not null) TemplateName = name.Values[0];
        if (description is not null) Description = description.Values[0];
        // The default tags live in a comma separated box, so a comma inside one tag would split it.
        if (hasTagRows) Tags = string.Join(", ", tags.Select(row => row.Values[0].Replace(',', ' ').Replace('，', ' ').Trim()));
        ApplyGroupRows(sheet);
        RefreshTemplateStatus();
    }
    private void LoadColumns(EpmaScriptManifest manifest)
    {
        foreach (var input in manifest.Inputs)
        {
            string unit = EpmaScriptText.Format(input.Unit);
            var column = new EpmaEditorColumn { Id = input.Id, Unit = unit, UnitBaseline = unit, IsFixed = InputColumns.Count == 0 && input.Id == EpmaScriptPackage.SampleInputId };
            LoadGroup(column, input.Group);
            InputColumns.Add(column);
        }
        LoadExamples();
        foreach (var output in manifest.Outputs)
        {
            var column = new EpmaEditorColumn { Id = output.Id };
            LoadGroup(column, output.Group);
            OutputColumns.Add(column);
        }
    }
    /// <summary>
    /// Reads a stored group heading: text in the default language box, the other languages in <see cref="EpmaEditorColumn.GroupTranslations"/>.
    /// A plain string belongs to the default language. A heading without default wording shows its first available wording instead (like a tag).
    /// </summary>
    private void LoadGroup(EpmaEditorColumn column, JsonElement group)
    {
        var wording = EpmaBasicLocalization.Read(group, DefaultLanguage);
        if (wording.Count > 0 && !wording.ContainsKey(DefaultLanguage)) wording[DefaultLanguage] = wording.Values.First();
        column.Group = wording.TryGetValue(DefaultLanguage, out var text) ? text : "";
        foreach (var pair in wording)
            if (pair.Key != DefaultLanguage) column.GroupTranslations[pair.Key] = pair.Value;
        column.GroupBaseline = new Dictionary<string, string>(wording, StringComparer.Ordinal);
    }
    private void ExamplesFileChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_syncingExamples && e.PropertyName == nameof(EpmaEditableFile.Text)) LoadExamples();
    }
    private void LoadExamples()
    {
        var rows = JsonNode.Parse(Files.Single(file => file.Path == "examples.json").Text)!["inputs"]!.AsArray();
        ExampleRows.Clear();
        for (int row = 0; row < Math.Max(1, rows.Count); row++) ExampleRows.Add(row + 1);
        foreach (var column in InputColumns)
        {
            // Retain the first cell so active bindings are never temporarily empty.
            while (column.Examples.Count > 1) column.Examples.RemoveAt(column.Examples.Count - 1);
            column.ExampleValue = rows.Count > 0 ? FormatCell(rows[0]?[column.Id]) : "";
            for (int row = 1; row < rows.Count; row++) column.Examples.Add(new() { Value = FormatCell(rows[row]?[column.Id]) });
        }
        NotifyExampleCommands();
    }
    public void Release()
    {
        ReleaseTemplateStatus();
        CancelPending(); WeakReferenceMessenger.Default.UnregisterAll(this);
        Files.Single(file => file.Path == "examples.json").PropertyChanged -= ExamplesFileChanged;
    }
    partial void OnIsDeveloperModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsReadOnly)); OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanShowSave));
        DecreasePatchCommand?.NotifyCanExecuteChanged();
        IncreasePatchCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(Preview));
        SaveCommand.NotifyCanExecuteChanged(); NotifyColumnCommands();
        SelectAllLanguagesCommand.NotifyCanExecuteChanged();
        RefreshAvailableLanguageOptions();
        foreach (var part in LanguageParts) part.NotifyDisplayTextChanged();
    }
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanNavigate));
        DecreasePatchCommand?.NotifyCanExecuteChanged();
        IncreasePatchCommand?.NotifyCanExecuteChanged();
        TestCommand.NotifyCanExecuteChanged(); SaveCommand.NotifyCanExecuteChanged(); NotifyColumnCommands();
        SelectAllLanguagesCommand.NotifyCanExecuteChanged();
    }
    private bool CanRun() => !IsBusy;
    private bool CanSave() => CanEdit;
    public void CancelPending() => _operation?.Cancel();
    [RelayCommand(CanExecute = nameof(CanRun))] private Task Test() => RunScriptAsync();
    [RelayCommand(CanExecute = nameof(CanSave))] private Task Save() => RunAsync();

    public EpmaScriptPackage BuildPackage() => BuildRawPackage(true);
    private EpmaScriptPackage BuildRawPackage(bool syncExamples)
    {
        string[] inputs = Names(InputColumns);
        if (syncExamples) { SyncExamples(inputs); CommitHelpEditor(); }
        return _document.Build(TemplateName, Version, Author, Description, Tags, ComposeDefinitions(inputs, Names(OutputColumns)), TemplateId, BuildLocalization(), HelpDocumentsToSave(), TemplateIcon);
    }
    private static string[] Names(ObservableCollection<EpmaEditorColumn> columns)
    {
        var names = columns.Select(column => column.Id.Trim()).ToArray();
        if (names.Length == 0 || names.Any(name => name.Length == 0 || name.IndexOfAny([',', '，']) >= 0))
            throw new InvalidDataException(EpmaLocalizer.Format("epma_editor_fields_required"));
        return names;
    }
    private static string[] ExampleTokens(string text)
    {
        // A trailing separator represents a missing value in the last column.
        // Keep it so reopened examples with nullable trailing inputs can run unchanged.
        return text.Trim().Split(new[] { ',', '，' }).Select(part => part.Trim()).ToArray();
    }
    private string ComposeDefinitions(string[] inputs, string[] outputs)
    {
        var previous = JsonNode.Parse(_definitionSnapshot)!.AsObject();
        var definitions = new JsonObject
        {
            ["inputs"] = Columns("inputs", inputs, previous, InputColumns, ApplyInputFields),
            ["outputs"] = Columns("outputs", outputs, previous, OutputColumns, ApplyOutputFields),
        };
        return definitions.ToJsonString(new() { WriteIndented = true });
    }
    private static JsonArray Columns(string section, string[] ids, JsonObject previous, IReadOnlyList<EpmaEditorColumn>? editable = null,
        Action<JsonObject, EpmaEditorColumn, bool>? apply = null)
    {
        var existing = previous[section] as JsonArray;
        var columns = new JsonArray();
        for (int index = 0; index < ids.Length; index++)
        {
            string id = ids[index];
            var old = existing?.OfType<JsonObject>().FirstOrDefault(row => row["id"]?.GetValue<string>() == id);
            var copy = old?.DeepClone().AsObject() ?? new JsonObject { ["id"] = id };
            copy["id"] = id;
            copy.Remove("type");
            // The first input is always the text column "Sample"; every other column is numeric by default.
            if (section == "inputs" && index == 0) copy["type"] = "text";
            copy.Remove("when"); // conditional columns no longer exist; an old package loses them here
            if (editable is not null && apply is not null && index < editable.Count) apply(copy, editable[index], old is not null);
            columns.Add(copy);
        }
        return columns;
    }
    /// <summary>
    /// Writes the unit cell of an input column into its definition. A unit the user did not touch is kept as stored
    /// (it may be a localized object); a changed one replaces the text shown for the current language.
    /// </summary>
    private static void ApplyUnit(JsonObject definition, EpmaEditorColumn column, bool storedBefore)
        => ApplyText(definition, "unit", column.Unit, column.UnitBaseline, storedBefore);
    /// <summary>Same as <see cref="ApplyUnit"/> for the optional group heading of a column.</summary>
    /// <summary>
    /// The wording of a column's group heading in the languages of the template, default language first; empty wording is left out.
    /// A group that was not changed since loading keeps its stored form; otherwise it is written as plain text when only the
    /// default language has wording, and as a language dictionary when there are more.
    /// </summary>
    private void ApplyGroup(JsonObject definition, EpmaEditorColumn column, bool storedBefore)
    {
        var after = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in LanguageParts)
        {
            string text = (part.Text == DefaultLanguage ? column.Group : column.GroupTranslations.TryGetValue(part.Text, out var known) ? known : "").Trim();
            if (text.Length > 0) after[part.Text] = text;
        }
        var before = column.GroupBaseline;
        if (storedBefore && before.Count == after.Count && after.All(pair => before.TryGetValue(pair.Key, out var text) && text == pair.Value)) return;
        if (after.Count == 0) definition.Remove("group");
        else if (after.Count == 1 && after.TryGetValue(DefaultLanguage, out var only)) definition["group"] = only;
        else
        {
            var localized = new JsonObject();
            foreach (var pair in after) localized[pair.Key] = pair.Value;
            definition["group"] = localized;
        }
    }
    private void ApplyInputFields(JsonObject definition, EpmaEditorColumn column, bool storedBefore)
    {
        ApplyUnit(definition, column, storedBefore);
        ApplyGroup(definition, column, storedBefore);
    }
    private void ApplyOutputFields(JsonObject definition, EpmaEditorColumn column, bool storedBefore)
    {
        ApplyGroup(definition, column, storedBefore);
    }
    internal static void ApplyText(JsonObject definition, string property, string? text, string? baseline, bool storedBefore)
    {
        text = text?.Trim() ?? "";
        if (storedBefore)
        {
            if (text == (baseline ?? "").Trim()) return;
            if (text.Length == 0) { definition.Remove(property); return; }
            if (definition[property] is JsonObject localized)
            {
                string[] candidates = [CultureInfo.CurrentUICulture.Name, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "en-US", "en"];
                string key = candidates.FirstOrDefault(localized.ContainsKey) ?? localized.Select(pair => pair.Key).FirstOrDefault() ?? "en-US";
                localized[key] = text;
                return;
            }
        }
        if (text.Length > 0) definition[property] = text;
        else definition.Remove(property);
    }
    private void SyncExamples(string[] inputIds)
    {
        if (_exampleTokenCount is int count && count != inputIds.Length) throw new InvalidDataException(EpmaLocalizer.Format("epma_editor_example_count"));
        var file = Files.Single(item => item.Path == "examples.json");
        var examples = JsonNode.Parse(file.CaptureText())!.AsObject();
        var rows = new JsonArray();
        for (int rowIndex = 0; rowIndex < ExampleRows.Count; rowIndex++)
        {
            var row = new JsonObject();
            for (int column = 0; column < inputIds.Length; column++)
                row[inputIds[column]] = JsonCell(InputColumns[column].Examples[rowIndex].Value.Trim());
            rows.Add(row);
        }
        examples["inputs"] = rows;
        _syncingExamples = true;
        try { file.Text = examples.ToJsonString(new() { WriteIndented = true }); }
        finally { _syncingExamples = false; }
    }
    private static JsonNode JsonCell(string token)
    {
        if (token.Length == 0) return JsonNode.Parse("null")!;
        return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number)
            ? JsonValue.Create(number) : JsonValue.Create(token)!;
    }
    private static string FormatCell(JsonNode? node)
    {
        if (node is null || node.GetValueKind() == JsonValueKind.Null) return "";
        if (node is JsonValue value && value.TryGetValue<double>(out double number)) return number.ToString("R", CultureInfo.InvariantCulture);
        if (node is JsonValue text && text.TryGetValue<string>(out string? content)) return content ?? "";
        return node.ToJsonString();
    }

    partial void OnStatusChanged(string value)
    {
        if (_isScriptRun || System.Windows.Application.Current is null || string.IsNullOrWhiteSpace(value)
            || value == EpmaLocalizer.Format("epma_editor_testing") || value == EpmaLocalizer.Format("epma_editor_saving")) return;
        if (value.StartsWith(EpmaLocalizer.Format("epma_editor_failed"), StringComparison.Ordinal)
            || value.StartsWith(EpmaLocalizer.Format("epma_editor_script_failed"), StringComparison.Ordinal)) Notify(NotificationType.Error, value);
        else Notify(NotificationType.Info, value);
    }

    /// <summary>Raised so the editor window can show notifications inside itself instead of the main window.</summary>
    public event Action<NotificationType, string>? NotificationRequested;

    private void Notify(NotificationType type, string message)
    {
        if (NotificationRequested is not null) { NotificationRequested(type, message); return; }
        switch (type)
        {
            case NotificationType.Error: MessageHelper.Error(message); break;
            case NotificationType.Warning: MessageHelper.Warning(message); break;
            case NotificationType.Success: MessageHelper.Success(message); break;
            default: MessageHelper.Info(message); break;
        }
    }

    private bool _isScriptRun;

    private async Task RunScriptAsync()
    {
        if (IsBusy) return;
        _isScriptRun = true;
        _draftNeedsValidation = true;
        ScriptHasError = false;
        IsBusy = true; TrialValues.Clear();
        Step = 1;
        ScriptStatus = EpmaLocalizer.Format("epma_editor_testing");
        using var operation = new CancellationTokenSource();
        _operation = operation;
        try
        {
            // Only capture the inputs needed to execute the draft. Package metadata and
            // declaration consistency are checked by BuildPackage/ValidateAsync on save.
            var columns = _sourceManifest;
            EpmaExampleDataset examples;
            if (IsReadOnly) examples = _original!.Package.Examples;
            else
            {
                string[] inputs = Names(InputColumns);
                        SyncExamples(inputs);
                var definitions = JsonNode.Parse(ComposeDefinitions(inputs, Names(OutputColumns)))!;
                columns = columns with
                {
                    Inputs = definitions["inputs"]!.Deserialize<EpmaScriptInput[]>(EpmaScriptPackage.JsonOptions)!,
                    Outputs = definitions["outputs"]!.Deserialize<EpmaScriptOutput[]>(EpmaScriptPackage.JsonOptions)!
                };
                examples = JsonSerializer.Deserialize<EpmaExampleDataset>(Files.Single(file => file.Path == "examples.json").CaptureText(), EpmaScriptPackage.JsonOptions)!;
            }
            var rows = await EpmaScriptRuntime.TestScriptAsync(ScriptFile.Path, ScriptFile.Text, columns, examples, operation.Token);
            operation.Token.ThrowIfCancellationRequested();
            foreach (var row in rows)
                foreach (var output in columns.Outputs.Where(output => row.Result.Values.ContainsKey(output.Id)))
                    TrialValues.Add(new(rows.Count > 1 ? row.InputRow + " · " + output.DisplayLabel : output.DisplayLabel, EpmaScriptText.Scalar(row.Result.Values[output.Id]), output.DisplayUnit));
            var errors = rows.SelectMany(row => row.Flags.Where(flag => flag.Severity == "Error").Select(flag => flag.Text))
                .Concat(rows.Where(row => row.Result.Status != "Computed").Select(row => row.Result.Status))
                .Where(text => !string.IsNullOrWhiteSpace(text)).Distinct().ToArray();
            if (errors.Length > 0)
            {
                _draftNeedsValidation = false;
                ScriptHasError = true;
                ScriptStatus = EpmaLocalizer.Format("epma_editor_script_failed") + "\n" + string.Join("\n", errors);
            }
            else
            {
                _draftNeedsValidation = false;
                ScriptStatus = EpmaLocalizer.Format("epma_editor_test_passed") + "\n" + string.Join("\n", TrialValues.Select(value => string.IsNullOrEmpty(value.Unit) ? value.Name + ": " + value.Value : value.Name + ": " + value.Value + " " + value.Unit));
            }
        }
        catch (OperationCanceledException) { ScriptStatus = EpmaLocalizer.Format("epma_cancelled"); }
        catch (Exception ex) { _draftNeedsValidation = false; ScriptHasError = true; ScriptStatus = EpmaLocalizer.Format("epma_editor_script_failed") + "\n" + ex.Message; }
        finally
        {
            // Preserve the status API for package tooling without emitting a trial notification.
            Status = ScriptStatus;
            _isScriptRun = false; _operation = null; IsBusy = false;
        }
    }

    private async Task RunAsync()
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        Status = EpmaLocalizer.Format("epma_editor_saving");
        using var operation = new CancellationTokenSource();
        _operation = operation;
        bool saved = false;
        try
        {
            var package = BuildPackage();
            await Task.Run(() => EpmaScriptRuntime.ValidateAsync(package, operation.Token), operation.Token);
            operation.Token.ThrowIfCancellationRequested();
            if (IsReadOnly) throw new InvalidOperationException(EpmaLocalizer.Format("epma_editor_readonly_hint"));
            CreatedTemplate = _original is null ? await _catalog.CreateAsync(package, operation.Token)
                : await _catalog.UpdateAsync(_original, package, IsDeveloperMode, operation.Token);
            _original = CreatedTemplate;
            _draftNeedsValidation = false; ScriptHasError = false;
            OnPropertyChanged(nameof(IsEditing)); OnPropertyChanged(nameof(EditorTitle));
            saved = true;
        }
        catch (OperationCanceledException) { Status = EpmaLocalizer.Format("epma_cancelled"); }
        catch (Exception ex) { Status = EpmaLocalizer.Format("epma_editor_failed") + "\n" + ex.Message; }
        finally { _operation = null; IsBusy = false; }
        if (saved) Saved?.Invoke();
    }
}
