using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>文件夹选择树中的一个节点。</summary>
    public partial class FolderPickerNodeViewModel : ObservableObject
    {
        private readonly Action<FolderPickerNodeViewModel> _onSelected;

        public FolderPickerNodeViewModel(
            TemplateFolderInfo folder,
            FolderPickerNodeViewModel? parent,
            bool isDisallowed,
            bool isMoving,
            bool isCurrent,
            Action<FolderPickerNodeViewModel> onSelected)
        {
            Folder = folder;
            Parent = parent;
            IsDisallowed = isDisallowed;
            IsMoving = isMoving;
            IsCurrent = isCurrent;
            _onSelected = onSelected;
            TemplateCount = folder.AllTemplates().Count();
        }

        public TemplateFolderInfo Folder { get; }

        public FolderPickerNodeViewModel? Parent { get; }

        public string Name => Folder.Name;

        public bool IsRoot => Folder.IsRoot;

        public int TemplateCount { get; }

        public bool HasCount => TemplateCount > 0;

        /// <summary>被移动的文件夹自身及其子级：不可作为目标。</summary>
        public bool IsDisallowed { get; }

        public bool IsEnabled => !IsDisallowed;

        /// <summary>正在被移动的文件夹（显示“移动中”标签）。</summary>
        public bool IsMoving { get; }

        /// <summary>被移动项目当前所在的文件夹（显示“当前位置”标签）。</summary>
        public bool IsCurrent { get; }

        public string MovingTagText => LanguageService.GetString("folder_picker_tag_moving", "Moving");

        public string CurrentTagText => LanguageService.GetString("folder_picker_tag_current", "Current");

        public ObservableCollection<FolderPickerNodeViewModel> Children { get; } = new();

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private bool _isSelected;

        /// <summary>搜索过滤后是否显示。</summary>
        [ObservableProperty]
        private bool _isFilterVisible = true;

        /// <summary>开始搜索前的展开状态，清空搜索时恢复。</summary>
        internal bool ExpandedBeforeFilter { get; set; }

        partial void OnIsSelectedChanged(bool value)
        {
            if (value)
                _onSelected(this);
        }
    }

    /// <summary>面包屑中的一段路径。</summary>
    public sealed class FolderPathSegment
    {
        public FolderPathSegment(string text, bool isFirst, bool isLast)
        {
            Text = text;
            HasSeparator = !isFirst;
            IsLast = isLast;
        }

        public string Text { get; }

        public bool HasSeparator { get; }

        public bool IsLast { get; }

        /// <summary>最后一段（即最终选中的位置）以主题色高亮。</summary>
        public bool IsAccent => IsLast;
    }

    /// <summary>“正在移动”的项目说明。</summary>
    public sealed record FolderPickerMovingInfo(bool IsFolder, string Name, string? Meta);

    /// <summary>文件夹选择窗口的参数。</summary>
    public sealed class FolderPickerOptions
    {
        public bool IsCustom { get; init; }

        public string Title { get; init; } = string.Empty;

        /// <summary>初始选中的文件夹（当前界面语言路径，不含根）。</summary>
        public IReadOnlyList<string>? InitialPath { get; init; }

        public bool AllowRoot { get; init; }

        /// <summary>不可选择的文件夹路径（该文件夹及其子级置灰），用于移动文件夹。</summary>
        public IReadOnlyList<string>? DisallowedPath { get; init; }

        /// <summary>被移动项目当前所在的文件夹路径；选中该位置时不允许确认。为 null 表示不是“移动”场景。</summary>
        public IReadOnlyList<string>? CurrentPath { get; init; }

        public FolderPickerMovingInfo? Moving { get; init; }

        /// <summary>确认按钮文字；为空时使用“确认”。</summary>
        public string? ConfirmText { get; init; }
    }

    /// <summary>
    /// 文件夹选择窗口（移动到 / 选择保存位置）的视图模型：可搜索的文件夹树、目标路径面包屑、窗口内新建文件夹。
    /// </summary>
    public partial class FolderPickerDialogViewModel : ObservableObject
    {
        private const string FolderIconData = "M22,19 A2,2 0 0 1 20,21 L4,21 A2,2 0 0 1 2,19 L2,5 A2,2 0 0 1 4,3 L9,3 L11,6 L20,6 A2,2 0 0 1 22,8 Z";
        private const string DocumentIconData = "M14,3 L6,3 A2,2 0 0 0 4,5 L4,19 A2,2 0 0 0 6,21 L18,21 A2,2 0 0 0 20,19 L20,9 Z M14,3 L14,9 L20,9 M8,14 L16,14 M8,17.5 L13,17.5";
        private const string MoveIconData = "M22,19 A2,2 0 0 1 20,21 L4,21 A2,2 0 0 1 2,19 L2,5 A2,2 0 0 1 4,3 L9,3 L11,6 L20,6 A2,2 0 0 1 22,8 Z M8.5,14 L15.5,14 M12.8,11.2 L15.6,14 L12.8,16.8";

        private readonly bool _isCustom;
        private readonly bool _allowRoot;
        private readonly IReadOnlyList<string>? _disallowedPath;
        private readonly IReadOnlyList<string>? _currentPath;
        private bool _isFiltering;

        public FolderPickerDialogViewModel(FolderPickerOptions options)
        {
            _isCustom = options.IsCustom;
            _allowRoot = options.AllowRoot;
            _disallowedPath = options.DisallowedPath;
            _currentPath = options.CurrentPath;

            Title = options.Title;
            ConfirmText = string.IsNullOrWhiteSpace(options.ConfirmText)
                ? LanguageService.GetString("Confirm", "OK")
                : options.ConfirmText!;
            IconData = MoveIconData;

            if (options.Moving is { } moving)
            {
                HasMovingContext = true;
                MovingIconData = moving.IsFolder ? FolderIconData : DocumentIconData;
                MovingLabel = LanguageService.GetString(
                    moving.IsFolder ? "folder_picker_moving_folder" : "folder_picker_moving_template",
                    moving.IsFolder ? "Moving folder" : "Moving diagram");
                MovingName = moving.Name;
                MovingMeta = moving.Meta ?? string.Empty;
            }

            SelectedHeading = HasMovingContext
                ? LanguageService.GetString("folder_picker_target_title", "Move to")
                : LanguageService.GetString("folder_picker_selected_title", "Selected location");

            Reload(options.InitialPath);
        }

        public string Title { get; }

        /// <summary>面包屑区域的小标题：移动场景为“将移动到”，否则为“已选位置”。</summary>
        public string SelectedHeading { get; }

        public string ConfirmText { get; }

        public string IconData { get; }

        public bool HasMovingContext { get; }

        public string MovingIconData { get; } = FolderIconData;

        public string MovingLabel { get; } = string.Empty;

        public string MovingName { get; } = string.Empty;

        public string MovingMeta { get; } = string.Empty;

        /// <summary>树的根节点集合（仅一个根）。</summary>
        public ObservableCollection<FolderPickerNodeViewModel> RootNodes { get; } = new();

        /// <summary>目标位置面包屑。</summary>
        public ObservableCollection<FolderPathSegment> PathSegments { get; } = new();

        /// <summary>弹出子窗口（新建文件夹）时使用的所属窗口。</summary>
        public Window? DialogOwner { get; set; }

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _hasNoMatch;

        [ObservableProperty]
        private string _noMatchText = string.Empty;

        [ObservableProperty]
        private FolderPickerNodeViewModel? _selectedNode;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private bool _canConfirm;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NewFolderCommand))]
        private bool _canCreateFolder;

        public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>确认后选中的文件夹路径（当前界面语言，不含根）。</summary>
        public IReadOnlyList<string>? SelectedPath { get; private set; }

        /// <summary>窗口关闭请求：true 表示确认，false 表示取消。</summary>
        public event Action<bool>? CloseRequested;

        /// <summary>需要把当前选中项滚动到可视区域（重新加载树之后）。</summary>
        public event Action? ScrollToSelectionRequested;

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        #region 树的构建

        private void Reload(IReadOnlyList<string>? selectPath)
        {
            _isFiltering = false;
            SearchText = string.Empty;

            RootNodes.Clear();
            SelectedNode = null;

            var folderRoot = TemplateFolderService.BuildFolderTree(_isCustom);
            var rootNode = CreateNode(folderRoot, null);
            rootNode.IsExpanded = true;
            RootNodes.Add(rootNode);

            var target = selectPath != null ? FindNode(rootNode, selectPath) : null;
            target ??= rootNode;
            for (var parent = target.Parent; parent != null; parent = parent.Parent)
                parent.IsExpanded = true;
            target.IsSelected = true;

            ApplyFilter();
            ScrollToSelectionRequested?.Invoke();
        }

        private FolderPickerNodeViewModel CreateNode(TemplateFolderInfo folder, FolderPickerNodeViewModel? parent)
        {
            var node = new FolderPickerNodeViewModel(
                folder,
                parent,
                isDisallowed: IsDisallowed(folder),
                isMoving: _disallowedPath is { Count: > 0 } && PathEquals(folder.DisplayPath, _disallowedPath),
                isCurrent: _currentPath != null && PathEquals(folder.DisplayPath, _currentPath),
                onSelected: OnNodeSelected);

            foreach (var child in folder.Children.OrderBy(c => c.Name, StringComparer.CurrentCulture))
                node.Children.Add(CreateNode(child, node));

            return node;
        }

        private static FolderPickerNodeViewModel? FindNode(FolderPickerNodeViewModel root, IReadOnlyList<string> path)
        {
            var current = root;
            foreach (var part in path)
            {
                current = current.Children.FirstOrDefault(c => string.Equals(c.Name, part, StringComparison.Ordinal));
                if (current == null)
                    return null;
            }
            return current;
        }

        private static bool PathEquals(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private bool IsDisallowed(TemplateFolderInfo folder)
        {
            if (_disallowedPath == null || _disallowedPath.Count == 0 || folder.DisplayPath.Count < _disallowedPath.Count)
                return false;

            for (int i = 0; i < _disallowedPath.Count; i++)
            {
                if (!string.Equals(folder.DisplayPath[i], _disallowedPath[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private bool IsSelectable(FolderPickerNodeViewModel node)
            => (_allowRoot || !node.IsRoot) && !node.IsDisallowed;

        #endregion

        #region 搜索过滤

        private void ApplyFilter()
        {
            string query = SearchText?.Trim() ?? string.Empty;

            if (query.Length == 0)
            {
                if (_isFiltering)
                {
                    _isFiltering = false;
                    foreach (var root in RootNodes)
                        RestoreFilter(root);
                }
                HasNoMatch = false;
                return;
            }

            if (!_isFiltering)
            {
                _isFiltering = true;
                foreach (var root in RootNodes)
                    SaveExpansion(root);
            }

            bool anyVisible = false;
            foreach (var root in RootNodes)
                anyVisible |= FilterNode(root, query);

            HasNoMatch = !anyVisible;
            NoMatchText = string.Format(
                LanguageService.GetString("folder_picker_no_match", "No folders match \"{0}\""), query);
        }

        private static void SaveExpansion(FolderPickerNodeViewModel node)
        {
            node.ExpandedBeforeFilter = node.IsExpanded;
            foreach (var child in node.Children)
                SaveExpansion(child);
        }

        private static void RestoreFilter(FolderPickerNodeViewModel node)
        {
            node.IsFilterVisible = true;
            node.IsExpanded = node.ExpandedBeforeFilter;
            foreach (var child in node.Children)
                RestoreFilter(child);
        }

        private static bool FilterNode(FolderPickerNodeViewModel node, string query)
        {
            bool childMatch = false;
            foreach (var child in node.Children)
                childMatch |= FilterNode(child, query);

            bool selfMatch = node.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase);
            node.IsFilterVisible = selfMatch || childMatch;
            node.IsExpanded = childMatch;
            return node.IsFilterVisible;
        }

        #endregion

        #region 选中状态

        private void OnNodeSelected(FolderPickerNodeViewModel node)
        {
            SelectedNode = node;
            UpdateSelectionState();
        }

        private void UpdateSelectionState()
        {
            var node = SelectedNode;
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
            PathSegments.Clear();

            if (node == null)
            {
                CanConfirm = false;
                CanCreateFolder = false;
                return;
            }

            CanCreateFolder = node.IsEnabled;

            var names = new[] { node.Folder.IsRoot ? node.Name : TemplateFolderService.GetRootName(_isCustom) }
                .Concat(node.Folder.DisplayPath)
                .ToList();
            for (int i = 0; i < names.Count; i++)
                PathSegments.Add(new FolderPathSegment(names[i], i == 0, i == names.Count - 1));

            if (!IsSelectable(node))
            {
                StatusMessage = node.IsDisallowed
                    ? LanguageService.GetString("folder_move_into_self", "A folder cannot be moved into itself or one of its subfolders.")
                    : LanguageService.GetString("folder_picker_root_not_allowed", "The root folder cannot be selected. Please choose a folder.");
            }
            else if (_currentPath != null && PathEquals(node.Folder.DisplayPath, _currentPath))
            {
                StatusMessage = LanguageService.GetString("folder_already_in_location", "It is already in this location.");
            }

            CanConfirm = string.IsNullOrEmpty(StatusMessage);
        }

        #endregion

        #region 命令

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void Confirm()
        {
            if (SelectedNode is not { } node || !IsSelectable(node))
                return;

            SelectedPath = node.Folder.DisplayPath.ToArray();
            CloseRequested?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel() => CloseRequested?.Invoke(false);

        [RelayCommand(CanExecute = nameof(CanCreateFolder))]
        private void NewFolder()
        {
            var parent = SelectedNode;
            if (parent == null)
                return;

            var parentPath = parent.Folder.DisplayPath;
            string location = string.Join(" / ", new[] { TemplateFolderService.GetRootName(_isCustom) }.Concat(parentPath));

            var nameViewModel = FolderNameDialogViewModel.ForCreate(
                location, parent.Folder.Children.Select(c => c.Name));
            var dialog = new FolderNameWindow(nameViewModel, DialogOwner);
            if (dialog.ShowDialog() != true)
                return;

            var result = TemplateFolderService.CreateFolder(_isCustom, parentPath, nameViewModel.Names);
            if (!result.Success)
            {
                ErrorMessage = result.Message;
                return;
            }

            string newName = TemplateFolderService.ResolveName(nameViewModel.Names);
            Reload(parentPath.Append(newName).ToArray());
        }

        #endregion
    }
}
