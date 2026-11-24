using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>文件夹名称窗口中某一种语言的名称输入项。</summary>
    public partial class FolderLanguageNameItem : ObservableObject
    {
        public FolderLanguageNameItem(string code, string displayName, string name, bool isDefault = false)
        {
            Code = code;
            DisplayName = displayName;
            IsDefault = isDefault;
            _name = name;
        }

        public string Code { get; }

        public string DisplayName { get; }

        /// <summary>是否为当前界面语言（表头显示“默认”标记）。</summary>
        public bool IsDefault { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFilled))]
        private string _name;

        [ObservableProperty]
        private bool _hasError;

        public bool IsFilled => !string.IsNullOrWhiteSpace(Name);
    }

    /// <summary>
    /// 新建 / 重命名文件夹窗口（FolderNameWindow）的视图模型。
    /// 主输入框对应当前界面语言，其余语言收在可折叠的“多语言名称”区域；校验在输入时实时进行。
    /// </summary>
    public partial class FolderNameDialogViewModel : ObservableObject
    {
        private const string CreateIconData = "M22,19 A2,2 0 0 1 20,21 L4,21 A2,2 0 0 1 2,19 L2,5 A2,2 0 0 1 4,3 L9,3 L11,6 L20,6 A2,2 0 0 1 22,8 Z M12,11 L12,17 M9,14 L15,14";
        private const string RenameIconData = "M12,20 L21,20 M16.5,3.5 A2.1,2.1 0 0 1 19.5,6.5 L7,19 L3,20 L4,16 Z";

        private readonly IReadOnlyList<string> _siblingNames;
        private readonly string _appLanguage;

        private FolderNameDialogViewModel(
            bool isRename,
            string location,
            IDictionary<string, string>? initialNames,
            IEnumerable<string>? siblingNames)
        {
            _siblingNames = (siblingNames ?? Enumerable.Empty<string>())
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .ToList();
            _appLanguage = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);

            Title = LanguageService.GetString(isRename ? "folder_rename_title" : "folder_new_title",
                isRename ? "Rename Folder" : "New Folder");
            LocationLabel = isRename
                ? LanguageService.GetString("folder_picker_tag_current", "Current location")
                : LanguageService.GetString("template_save_location", "Save location");
            LocationText = location;
            IconData = isRename ? RenameIconData : CreateIconData;
            ConfirmText = LanguageService.GetString(isRename ? "folder_name_save" : "folder_name_create",
                isRename ? "Save" : "Create");

            string Initial(string code)
            {
                if (initialNames == null)
                    return string.Empty;
                var match = initialNames.FirstOrDefault(p => string.Equals(p.Key, code, StringComparison.OrdinalIgnoreCase));
                return match.Value ?? string.Empty;
            }

            PrimaryLanguageTag = AppCultureRegistry.GetDisplayName(_appLanguage, false);
            PrimaryItem = new FolderLanguageNameItem(_appLanguage, PrimaryLanguageTag, Initial(_appLanguage), isDefault: true);
            // 其余语言直接取自软件支持的语言列表：以后新增支持语言时，多语言表格会自动多出一列
            OtherLanguages = new ObservableCollection<FolderLanguageNameItem>(
                AppCultureRegistry.SupportedCodes
                    .Where(code => !string.Equals(code, _appLanguage, StringComparison.OrdinalIgnoreCase))
                    .Select(code => new FolderLanguageNameItem(code, AppCultureRegistry.GetDisplayName(code, false), Initial(code))));
            // 重命名且已有其它语言的名称时，默认展开多语言区
            _isMultiLanguageExpanded = OtherLanguages.Any(i => i.IsFilled);

            PrimaryItem.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FolderLanguageNameItem.Name))
                    Validate();
            };
            foreach (var item in OtherLanguages)
            {
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(FolderLanguageNameItem.Name))
                        Validate();
                };
            }

            Validate();
        }

        /// <summary>新建文件夹。<paramref name="location"/> 为父级路径文字，<paramref name="siblingNames"/> 为同级已有文件夹名。</summary>
        public static FolderNameDialogViewModel ForCreate(string location, IEnumerable<string>? siblingNames)
            => new(false, location, null, siblingNames);

        /// <summary>重命名文件夹。<paramref name="siblingNames"/> 不应包含文件夹自身。</summary>
        public static FolderNameDialogViewModel ForRename(
            string location,
            IDictionary<string, string> initialNames,
            IEnumerable<string>? siblingNames)
            => new(true, location, initialNames, siblingNames);

        public string Title { get; }

        public string IconData { get; }

        public string ConfirmText { get; }

        /// <summary>只读位置框的标签：新建为“保存位置”，重命名为“当前位置”。</summary>
        public string LocationLabel { get; }

        /// <summary>“个人图解 / A / B”形式的位置文字。</summary>
        public string LocationText { get; }

        /// <summary>名称输入行右侧显示的当前界面语言标签。</summary>
        public string PrimaryLanguageTag { get; }

        /// <summary>当前界面语言的名称输入项（主输入框）。</summary>
        public FolderLanguageNameItem PrimaryItem { get; }

        /// <summary>其余语言的名称输入项。</summary>
        public ObservableCollection<FolderLanguageNameItem> OtherLanguages { get; }

        /// <summary>确认后的各语言名称（仅包含已填写的语言）。</summary>
        public Dictionary<string, string> Names { get; } = new(StringComparer.OrdinalIgnoreCase);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ExpandToggleText))]
        private bool _isMultiLanguageExpanded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private bool _canConfirm;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>“多语言名称”标题右侧的展开 / 收起链接文字。</summary>
        public string ExpandToggleText => IsMultiLanguageExpanded
            ? LanguageService.GetString("folder_name_collapse", "Collapse")
            : LanguageService.GetString("folder_name_expand", "Expand");

        /// <summary>窗口关闭请求：true 表示确认，false 表示取消。</summary>
        public event Action<bool>? CloseRequested;

        private IEnumerable<FolderLanguageNameItem> AllItems
            => new[] { PrimaryItem }.Concat(OtherLanguages);

        private void Validate()
        {
            string? error = null;
            int filled = 0;

            foreach (var item in AllItems)
            {
                item.HasError = false;
                string value = item.Name?.Trim() ?? string.Empty;
                if (value.Length == 0)
                    continue;

                filled++;
                if (error == null && !TemplateFolderService.IsValidName(value))
                {
                    error = LanguageService.GetString("invalid_filename_char", "The name contains invalid characters.");
                    item.HasError = true;
                }
            }

            // 同级重名：与服务端一致，按当前界面语言解析出的名称比较
            if (error == null && filled > 0)
            {
                var names = CollectNames();
                string resolved = TemplateFolderService.ResolveName(names).Trim();
                if (resolved.Length > 0 && _siblingNames.Any(n => string.Equals(n, resolved, StringComparison.OrdinalIgnoreCase)))
                {
                    error = LanguageService.GetString("folder_name_exists", "A folder with the same name already exists here.");
                    var target = AllItems.FirstOrDefault(i => string.Equals(i.Name?.Trim(), resolved, StringComparison.OrdinalIgnoreCase));
                    if (target != null)
                        target.HasError = true;
                }
            }

            ErrorMessage = error ?? string.Empty;
            CanConfirm = error == null && filled > 0;
        }

        [RelayCommand]
        private void ToggleMultiLanguage() => IsMultiLanguageExpanded = !IsMultiLanguageExpanded;

        #region 多语言名称导入 / 导出（与图解编辑的多语言页共用 DiagramTemplateTranslationHelper 的 JSON 格式）

        private const string TransferRowKey = "folderName";

        /// <summary>导入 / 导出的结果提示（显示在多语言表格下方）。</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasTransferMessage))]
        private string _transferMessage = string.Empty;

        [ObservableProperty]
        private bool _transferIsError;

        public bool HasTransferMessage => !string.IsNullOrEmpty(TransferMessage);

        private void ShowTransferResult(string message, bool isError)
        {
            TransferIsError = isError;
            TransferMessage = message;
        }

        /// <summary>把各语言的名称组装成一行翻译表（列 = 软件支持的语言），供共用的导入导出逻辑使用。</summary>
        private DataTable BuildTransferTable()
        {
            var table = new DataTable();
            table.Columns.Add("Context", typeof(string));
            table.Columns.Add(DiagramTemplateTranslationHelper.TranslationKeyColumn, typeof(string));
            table.Columns.Add("ObjectRef", typeof(object));
            foreach (var item in AllItems)
                table.Columns.Add(item.Code, typeof(string));

            var row = table.NewRow();
            row["Context"] = LanguageService.GetString("folder_name_label", "Folder name");
            row[DiagramTemplateTranslationHelper.TranslationKeyColumn] = TransferRowKey;
            foreach (var item in AllItems)
                row[item.Code] = item.Name?.Trim() ?? string.Empty;
            table.Rows.Add(row);
            return table;
        }

        [RelayCommand]
        private void ExportTranslations()
        {
            var languages = AllItems.Select(i => i.Code).ToList();
            var dialog = new SaveFileDialog
            {
                Title = LanguageService.GetString("diagram_translation_export", "Export Translations"),
                Filter = FileDialogFilterHelper.JsonOnly,
                DefaultExt = "json",
                FileName = "folder-name-translations.json"
            };
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string json = DiagramTemplateTranslationHelper.ExportToJson(BuildTransferTable(), languages, _appLanguage);
                File.WriteAllText(dialog.FileName, json);
                ShowTransferResult(
                    string.Format(LanguageService.GetString("translation_export_success", "Exported translations to {0}."), dialog.FileName),
                    false);
            }
            catch (Exception ex)
            {
                ShowTransferResult($"{LanguageService.GetString("translation_export_failed", "Export failed.")} {ex.Message}", true);
            }
        }

        [RelayCommand]
        private void ImportTranslations()
        {
            var languages = AllItems.Select(i => i.Code).ToList();
            var dialog = new OpenFileDialog
            {
                Title = LanguageService.GetString("diagram_translation_import", "Import Translations"),
                Filter = FileDialogFilterHelper.JsonOnly,
                DefaultExt = "json"
            };
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var table = BuildTransferTable();
                var result = DiagramTemplateTranslationHelper.ImportFromJson(File.ReadAllText(dialog.FileName), table, languages);
                if (!result.IsSuccess)
                {
                    ShowTransferResult(result.ErrorMessage, true);
                    return;
                }

                foreach (var item in AllItems)
                    item.Name = table.Rows[0][item.Code] as string ?? string.Empty;

                ShowTransferResult(
                    string.Format(LanguageService.GetString("translation_import_success", "Imported {0} items, updated {1} cells."),
                        result.MatchedItems, result.UpdatedCells),
                    false);
            }
            catch (Exception ex)
            {
                ShowTransferResult($"{LanguageService.GetString("translation_import_failed", "Import failed.")} {ex.Message}", true);
            }
        }

        #endregion

        private Dictionary<string, string> CollectNames()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in AllItems)
            {
                string value = item.Name?.Trim() ?? string.Empty;
                if (value.Length > 0)
                    result[item.Code] = value;
            }
            return result;
        }

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void Confirm()
        {
            Names.Clear();
            foreach (var pair in CollectNames())
                Names[pair.Key] = pair.Value;
            CloseRequested?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel() => CloseRequested?.Invoke(false);
    }
}
