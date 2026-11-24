using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 图解模板库的文件夹操作：新建 / 重命名（多语言）/ 移动 / 删除文件夹，移动模板，在文件夹中新建图解。
    /// 个人图解对所有用户开放；官方图解仅开发者模式可用。
    /// </summary>
    public partial class MainPlotViewModel
    {
        private const string OfficialScope = "Official";

        private static void MarkCustomScope(GraphMapTemplateNode? node)
        {
            if (node == null)
                return;

            node.IsCustomScope = true;
            foreach (var child in node.Children)
                MarkCustomScope(child);
        }

        private bool CanManageFolders(bool isCustom) => isCustom || IsDeveloperMode;

        /// <summary>
        /// 树节点 → (个人/官方, 当前界面语言下的文件夹路径)。模板节点返回其所在文件夹。
        /// </summary>
        private bool TryGetFolderLocation(GraphMapTemplateNode? node, out bool isCustom, out List<string> folderPath)
        {
            isCustom = false;
            folderPath = new List<string>();
            if (node == null)
                return false;

            var current = node.IsFolder ? node : node.Parent;
            while (current != null)
            {
                if (ReferenceEquals(current, PersonalTemplatesNode))
                {
                    isCustom = true;
                    return true;
                }

                // 树根（BuildTreeFromEntities 的 Root）与官方根节点不计入路径
                if (current.Parent == null || ReferenceEquals(current, OfficialTemplatesNode))
                    break;

                folderPath.Insert(0, current.Name);
                current = current.Parent;
            }

            isCustom = node.IsCustomScope;
            return true;
        }

        private static Window? DialogOwner => Application.Current?.MainWindow is { IsVisible: true } window ? window : null;

        private async Task RunFolderOperationAsync(Func<TemplateFolderResult> operation, string successKey, string successFallback)
        {
            try
            {
                var result = await Task.Run(operation);
                if (!result.Success)
                {
                    MessageHelper.Warning(result.Message);
                    return;
                }

                await RefreshTemplateLibraryAfterDataChangeAsync();
                MessageHelper.Success(LanguageService.GetString(successKey, successFallback));
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.Message);
            }
        }

        [RelayCommand]
        private void NewTemplateInFolder(GraphMapTemplateNode? node)
        {
            if (!TryGetFolderLocation(node, out bool isCustom, out var path) || !isCustom)
                return;

            var folder = path.Count == 0 ? null : TemplateFolderService.FindFolder(true, path);
            OpenNewTemplateEditor(folder == null ? null : TemplateFolderService.BuildCategoryParts(folder));
        }

        [RelayCommand]
        private Task NewRootFolder(string? scope) => CreateFolderAsync(scope != OfficialScope, Array.Empty<string>());

        [RelayCommand]
        private Task NewSubFolder(GraphMapTemplateNode? node)
        {
            if (!TryGetFolderLocation(node, out bool isCustom, out var path))
                return Task.CompletedTask;
            return CreateFolderAsync(isCustom, path);
        }

        private async Task CreateFolderAsync(bool isCustom, IReadOnlyList<string> parentPath)
        {
            if (!CanManageFolders(isCustom))
                return;

            var siblingNames = await Task.Run(() => GetChildFolderNames(isCustom, parentPath));
            var nameViewModel = FolderNameDialogViewModel.ForCreate(BuildFolderLocationText(isCustom, parentPath), siblingNames);
            var dialog = new FolderNameWindow(nameViewModel, DialogOwner);
            if (dialog.ShowDialog() != true)
                return;

            var names = new Dictionary<string, string>(dialog.Names, StringComparer.OrdinalIgnoreCase);
            await RunFolderOperationAsync(
                () => TemplateFolderService.CreateFolder(isCustom, parentPath, names),
                "folder_create_success", "Folder created.");
        }

        /// <summary>“个人图解 / A / B”形式的路径文字（根名称 + 当前界面语言路径）。</summary>
        private static string BuildFolderLocationText(bool isCustom, IEnumerable<string> folderPath)
            => string.Join(" / ", new[] { TemplateFolderService.GetRootName(isCustom) }.Concat(folderPath));

        /// <summary>某个文件夹下已有的子文件夹名称（当前界面语言），用于输入时的同名检查。</summary>
        private static IReadOnlyList<string> GetChildFolderNames(bool isCustom, IReadOnlyList<string> parentPath)
        {
            var parent = TemplateFolderService.FindFolder(isCustom, parentPath);
            return parent == null
                ? Array.Empty<string>()
                : parent.Children.Select(c => c.Name).ToList();
        }

        [RelayCommand]
        private async Task RenameFolder(GraphMapTemplateNode? node)
        {
            if (node is not { IsFolder: true } || !TryGetFolderLocation(node, out bool isCustom, out var path) ||
                path.Count == 0 || !CanManageFolders(isCustom))
                return;

            var folder = await Task.Run(() => TemplateFolderService.FindFolder(isCustom, path));
            if (folder == null)
            {
                MessageHelper.Warning(LanguageService.GetString("folder_not_found", "The folder no longer exists. Please refresh and try again."));
                return;
            }

            var initialNames = folder.LocalizedNames.Count > 0
                ? new Dictionary<string, string>(folder.LocalizedNames, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage)] = folder.Name
                };

            var siblingNames = await Task.Run(() => GetChildFolderNames(isCustom, path.Take(path.Count - 1).ToList()));
            var nameViewModel = FolderNameDialogViewModel.ForRename(
                BuildFolderLocationText(isCustom, path),
                initialNames,
                siblingNames.Where(n => !string.Equals(n, folder.Name, StringComparison.Ordinal)));
            var dialog = new FolderNameWindow(nameViewModel, DialogOwner);
            if (dialog.ShowDialog() != true)
                return;

            var names = new Dictionary<string, string>(dialog.Names, StringComparer.OrdinalIgnoreCase);
            await RunFolderOperationAsync(
                () => TemplateFolderService.RenameFolder(isCustom, path, names),
                "folder_rename_success", "Folder renamed.");
        }

        [RelayCommand]
        private async Task MoveFolder(GraphMapTemplateNode? node)
        {
            if (node is not { IsFolder: true } || !TryGetFolderLocation(node, out bool isCustom, out var path) ||
                path.Count == 0 || !CanManageFolders(isCustom))
                return;

            var folder = await Task.Run(() => TemplateFolderService.FindFolder(isCustom, path));
            if (folder == null)
            {
                MessageHelper.Warning(LanguageService.GetString("folder_not_found", "The folder no longer exists. Please refresh and try again."));
                return;
            }

            var parentPath = path.Take(path.Count - 1).ToList();
            var pickerViewModel = new FolderPickerDialogViewModel(new FolderPickerOptions
            {
                IsCustom = isCustom,
                Title = LanguageService.GetString("folder_move_title", "Move To"),
                InitialPath = parentPath,
                AllowRoot = true,
                DisallowedPath = path,
                CurrentPath = parentPath,
                Moving = new FolderPickerMovingInfo(
                    IsFolder: true,
                    Name: folder.Name,
                    Meta: string.Format(
                        LanguageService.GetString("folder_picker_template_count", "{0} diagram(s)"),
                        folder.AllTemplates().Count())),
                ConfirmText = LanguageService.GetString("folder_picker_move", "Move")
            });
            var dialog = new FolderPickerWindow(pickerViewModel, DialogOwner);
            if (dialog.ShowDialog() != true || dialog.SelectedPath == null)
                return;

            var target = dialog.SelectedPath;
            await RunFolderOperationAsync(
                () => TemplateFolderService.MoveFolder(isCustom, path, target),
                "folder_move_success", "Moved successfully.");
        }

        [RelayCommand]
        private async Task DeleteFolder(GraphMapTemplateNode? node)
        {
            if (node is not { IsFolder: true } || !TryGetFolderLocation(node, out bool isCustom, out var path) ||
                path.Count == 0 || !CanManageFolders(isCustom))
                return;

            var folder = await Task.Run(() => TemplateFolderService.FindFolder(isCustom, path));
            if (folder == null)
            {
                MessageHelper.Warning(LanguageService.GetString("folder_not_found", "The folder no longer exists. Please refresh and try again."));
                return;
            }

            int templateCount = folder.AllTemplates().Count();
            if (!isCustom && templateCount > 0)
            {
                MessageHelper.Warning(LanguageService.GetString("official_folder_not_empty", "Only empty official folders can be deleted."));
                return;
            }

            string message = templateCount > 0
                ? string.Format(LanguageService.GetString("folder_delete_confirm_with_templates",
                    "Delete the folder \"{0}\" and the {1} template(s) in it? This cannot be undone."), folder.Name, templateCount)
                : string.Format(LanguageService.GetString("folder_delete_confirm",
                    "Delete the folder \"{0}\"?"), folder.Name);

            if (!await MessageHelper.ShowAsyncDialog(message, LanguageService.Instance["Cancel"], LanguageService.Instance["Confirm"]))
                return;

            await RunFolderOperationAsync(
                () => TemplateFolderService.DeleteFolder(isCustom, path),
                "folder_delete_success", "Folder deleted.");
        }

        /// <summary>模板卡片右键「移动到…」</summary>
        [RelayCommand]
        private Task MoveTemplateToFolder(TemplateCardViewModel? card)
        {
            if (card?.TemplateId is not Guid id)
                return Task.CompletedTask;
            return MoveTemplateAsync(id);
        }

        /// <summary>
        /// 左侧树模板节点的右键菜单与卡片共用：优先复用右侧已显示的卡片，否则按节点临时创建一张。
        /// </summary>
        public TemplateCardViewModel? GetTemplateCardForNode(GraphMapTemplateNode? node)
        {
            if (node is not { IsFolder: false, TemplateId: Guid id })
                return null;

            return TemplateCards.FirstOrDefault(c => c.TemplateId == id)
                   ?? CreateTemplateCardViewModel(node, BuildTemplateSummaryLookup());
        }

        private async Task MoveTemplateAsync(Guid templateId)
        {
            var entity = await Task.Run(() => GraphMapDatabaseService.Instance.GetSummaries()
                .FirstOrDefault(s => s.Id == templateId));
            if (entity == null)
            {
                MessageHelper.Warning(LanguageService.GetString("diagram_template_source_missing", "Template not found."));
                return;
            }

            if (!CanManageFolders(entity.IsCustom))
                return;

            var segments = TemplateFolderService.GetDisplaySegments(entity.NodeList);
            var currentFolder = segments.Length > 1 ? segments.Take(segments.Length - 1).ToList() : null;

            var currentPath = currentFolder ?? new List<string>();
            string templateName = segments.Length > 0 ? segments[^1] : entity.Id.ToString();
            var pickerViewModel = new FolderPickerDialogViewModel(new FolderPickerOptions
            {
                IsCustom = entity.IsCustom,
                Title = LanguageService.GetString("folder_move_title", "Move To"),
                InitialPath = currentFolder,
                AllowRoot = false,
                CurrentPath = currentPath,
                Moving = new FolderPickerMovingInfo(
                    IsFolder: false,
                    Name: templateName,
                    Meta: BuildFolderLocationText(entity.IsCustom, currentPath)),
                ConfirmText = LanguageService.GetString("folder_picker_move", "Move")
            });
            var dialog = new FolderPickerWindow(pickerViewModel, DialogOwner);
            if (dialog.ShowDialog() != true || dialog.SelectedPath == null)
                return;

            var target = dialog.SelectedPath;
            await RunFolderOperationAsync(
                () => TemplateFolderService.MoveTemplate(templateId, target),
                "folder_move_success", "Moved successfully.");
        }
    }
}
