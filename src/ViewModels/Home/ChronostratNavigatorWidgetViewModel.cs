using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Models.Chronostrat;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace GeoChemistryNexus.ViewModels.Home
{
    /// <summary>
    /// 国际年代地层表速查卡（ICS Chronostratigraphic Navigator）的 ViewModel。
    /// </summary>
    public partial class ChronostratNavigatorWidgetViewModel : ObservableObject
    {
        public const string SourceUrl = "https://github.com/i-c-stratigraphy/chart";

        [RelayCommand]
        private void OpenSource()
        {
            try
            {
                Process.Start(new ProcessStartInfo(SourceUrl) { UseShellExecute = true });
            }
            catch
            {
                // 忽略外部浏览器无法打开的情况
            }
        }
        // ── 搜索 / 年龄反查 ───────────────────────────────────────────────────

        /// <summary>快速关键词搜索框内容（中/英/双轨制均支持）。</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSearchResults))]
        [NotifyPropertyChangedFor(nameof(IsInSearchMode))]
        private string searchText = string.Empty;

        /// <summary>年龄反查输入（Ma）。</summary>
        [ObservableProperty]
        private double ageLookupMa = 251.9;

        /// <summary>年龄反查结果摘要文本。</summary>
        [ObservableProperty]
        private string ageLookupResultText = string.Empty;

        /// <summary>年龄反查是否找到结果。</summary>
        [ObservableProperty]
        private bool hasAgeLookupResult;

        // ── 当前选中单元与上下文 ─────────────────────────────────────────────

        /// <summary>当前在右侧面板展示的地质年代单元。</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedUnit))]
        [NotifyPropertyChangedFor(nameof(SelectedColorHex))]
        [NotifyPropertyChangedFor(nameof(SelectedColorRgbText))]
        [NotifyPropertyChangedFor(nameof(BoundaryBadgeText))]
        [NotifyPropertyChangedFor(nameof(SelectedUnitDisplayName))]
        [NotifyPropertyChangedFor(nameof(SelectedUnitSubDisplayName))]
        [NotifyPropertyChangedFor(nameof(SelectedUnitStratDisplayName))]
        [NotifyPropertyChangedFor(nameof(SelectedUnitRankDisplayName))]
        private ChronostratUnitRecord? selectedUnit;

        /// <summary>当前选中单元的祖先链（从父到根，用于面包屑）。</summary>
        public ObservableCollection<ChronostratUnitRecord> Ancestors { get; } = new();

        /// <summary>当前选中单元的直接子单元列表。</summary>
        public ObservableCollection<ChronostratUnitRecord> Children { get; } = new();

        /// <summary>年龄反查后高亮的路径（从最小单元到根）。</summary>
        public ObservableCollection<ChronostratUnitRecord> AgeLookupPath { get; } = new();

        // ── 层级树：树形节点 ─────────────────────────────────────────────────

        /// <summary>顶层树节点集合（宙/超宙级别），驱动主导航折叠树。</summary>
        public ObservableCollection<ChronostratTreeNodeViewModel> RootNodes { get; } = new();

        /// <summary>顶层原始数据节点（备用）。</summary>
        public ObservableCollection<ChronostratUnitRecord> RootUnits { get; } = new();

        private readonly Dictionary<string, ChronostratTreeNodeViewModel> _nodesById = new(StringComparer.OrdinalIgnoreCase);

        // ── 搜索结果 ──────────────────────────────────────────────────────────

        public ObservableCollection<ChronostratUnitRecord> SearchResults { get; } = new();

        // ── 派生属性 ──────────────────────────────────────────────────────────

        public bool HasSelectedUnit   => SelectedUnit != null;
        public bool HasSearchResults  => SearchResults.Count > 0;
        public bool IsInSearchMode    => !string.IsNullOrWhiteSpace(SearchText);

        public string SelectedColorHex => SelectedUnit?.ColorHex ?? "#CCCCCC";

        public string SelectedColorRgbText
        {
            get
            {
                if (SelectedUnit?.ColorHex is null) return string.Empty;
                try
                {
                    var hex = SelectedUnit.ColorHex.TrimStart('#');
                    if (hex.Length == 6)
                    {
                        int r = Convert.ToInt32(hex[..2], 16);
                        int g = Convert.ToInt32(hex[2..4], 16);
                        int b = Convert.ToInt32(hex[4..6], 16);
                        return $"RGB({r}, {g}, {b})";
                    }
                }
                catch { /* ignore */ }
                return string.Empty;
            }
        }

        public string BoundaryBadgeText
        {
            get
            {
                if (SelectedUnit == null) return string.Empty;
                if (SelectedUnit.HasGssp) return "GSSP";
                if (SelectedUnit.HasGssa) return "GSSA";
                return string.Empty;
            }
        }

        public string SelectedUnitDisplayName      => SelectedUnit?.GetDisplayName() ?? string.Empty;
        public string SelectedUnitSubDisplayName   => SelectedUnit?.GetSubDisplayName() ?? string.Empty;
        public string SelectedUnitStratDisplayName => SelectedUnit?.GetStratigraphicName() ?? string.Empty;
        public string SelectedUnitRankDisplayName  => SelectedUnit?.GetLocalizedRank() ?? string.Empty;

        // ── 数据来源信息 ──────────────────────────────────────────────────────

        public string SourceCitation  => ChronostratDataService.Catalog.Source.Citation;
        public string SourceVersion   => ChronostratDataService.Catalog.Source.Version;

        // ── 构造 ──────────────────────────────────────────────────────────────

        public ChronostratNavigatorWidgetViewModel()
        {
            LoadRoots();
            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                // 刷新整棵树的多语言显示
                foreach (var root in RootNodes)
                {
                    root.RefreshLanguage();
                }
                // 刷新当前选中项的派生多语言属性
                OnPropertyChanged(nameof(SelectedUnitDisplayName));
                OnPropertyChanged(nameof(SelectedUnitSubDisplayName));
                OnPropertyChanged(nameof(SelectedUnitStratDisplayName));
                OnPropertyChanged(nameof(SelectedUnitRankDisplayName));
                UpdateAgeLookupResultText();
            }
        }

        private void UpdateAgeLookupResultText()
        {
            if (AgeLookupPath.Count == 0) return;
            var smallest = AgeLookupPath[0];
            var periodOrHigher = AgeLookupPath.FirstOrDefault(u => u.Rank <= ChronostratRank.Period);

            string periodSub = periodOrHigher?.GetSubDisplayName() ?? string.Empty;
            string periodName = periodOrHigher != null
                ? (string.IsNullOrEmpty(periodSub)
                    ? periodOrHigher.GetDisplayName()
                    : $"{periodOrHigher.GetDisplayName()} ({periodSub})")
                : string.Empty;

            string smallestSub = smallest.GetSubDisplayName();
            string smallestName = string.IsNullOrEmpty(smallestSub)
                ? smallest.GetDisplayName()
                : $"{smallest.GetDisplayName()} ({smallestSub})";

            AgeLookupResultText = string.IsNullOrEmpty(periodName)
                ? $"{AgeLookupMa} Ma → {smallestName}"
                : $"{AgeLookupMa} Ma → {smallestName}  ·  {periodName}";
        }

        // ── Commands ─────────────────────────────────────────────────────────

        /// <summary>执行年龄反查地层。</summary>
        [RelayCommand]
        private void LookupAge()
        {
            AgeLookupPath.Clear();
            HasAgeLookupResult = false;
            AgeLookupResultText = string.Empty;

            if (AgeLookupMa < 0)
            {
                AgeLookupResultText = "请输入有效的正值年龄（Ma）。";
                return;
            }

            var path = ChronostratDataService.FindAncestorPath(AgeLookupMa);
            if (path.Count == 0)
            {
                AgeLookupResultText = $"{AgeLookupMa} Ma 超出已知年代地层范围。";
                return;
            }

            foreach (var u in path)
                AgeLookupPath.Add(u);

            // 自动选中最小单元（路径第一个）
            SelectUnit(path[0]);
            UpdateAgeLookupResultText();
            HasAgeLookupResult = true;
        }

        /// <summary>选中某一地质年代单元，更新右侧面板。</summary>
        [RelayCommand]
        public void SelectUnit(ChronostratUnitRecord? unit)
        {
            if (unit == null) return;

            SelectedUnit = unit;

            // 联动折叠树：确保树节点展开并高亮选中
            if (_nodesById.TryGetValue(unit.Id, out var node))
            {
                node.EnsurePathExpanded();
                node.IsSelected = true;
            }

            // 更新面包屑祖先链
            Ancestors.Clear();
            foreach (var a in ChronostratDataService.GetAncestors(unit))
                Ancestors.Add(a);

            // 更新子节点列表
            Children.Clear();
            foreach (var c in unit.Children)
                Children.Add(c);
        }

        /// <summary>展开至“纪 (Period)”级别（推荐视图，一览无余地质全纪）。</summary>
        [RelayCommand]
        public void ExpandToPeriod()
        {
            foreach (var root in RootNodes)
                root.ExpandToRank(ChronostratRank.Period);
        }

        /// <summary>展开树形视图中的所有层级。</summary>
        [RelayCommand]
        public void ExpandAll()
        {
            foreach (var root in RootNodes)
                root.SetExpandedRecursive(true);
        }

        /// <summary>折叠树形视图中的所有节点。</summary>
        [RelayCommand]
        public void CollapseAll()
        {
            foreach (var root in RootNodes)
                root.SetExpandedRecursive(false);
        }

        /// <summary>实时搜索：SearchText 变化时自动触发。</summary>
        partial void OnSearchTextChanged(string value)
        {
            SearchResults.Clear();
            if (string.IsNullOrWhiteSpace(value)) return;

            var results = ChronostratDataService.Search(value);
            foreach (var r in results)
                SearchResults.Add(r);
        }

        /// <summary>清除搜索框。</summary>
        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        /// <summary>复制当前选中单元的 IUGS 官方颜色 HEX 到剪贴板。</summary>
        [RelayCommand]
        private void CopyHex()
        {
            if (SelectedUnit == null) return;
            try { Clipboard.SetText(SelectedUnit.ColorHex); } catch { /* ignore */ }
        }

        /// <summary>复制当前选中单元的 RGB 到剪贴板。</summary>
        [RelayCommand]
        private void CopyRgb()
        {
            if (string.IsNullOrEmpty(SelectedColorRgbText)) return;
            try { Clipboard.SetText(SelectedColorRgbText); } catch { /* ignore */ }
        }

        /// <summary>复制当前选中单元的标准学术引用文本到剪贴板。</summary>
        [RelayCommand]
        private void CopyCitation()
        {
            if (SelectedUnit == null) return;
            string startText = SelectedUnit.StartMa.HasValue
                ? (SelectedUnit.StartError.HasValue
                    ? $"{SelectedUnit.StartMa.Value} ± {SelectedUnit.StartError.Value} Ma"
                    : $"{SelectedUnit.StartMa.Value} Ma")
                : "unknown";
            string endText = SelectedUnit.EndMa.HasValue
                ? (SelectedUnit.EndError.HasValue
                    ? $"{SelectedUnit.EndMa.Value} ± {SelectedUnit.EndError.Value} Ma"
                    : $"{SelectedUnit.EndMa.Value} Ma")
                : "present";

            string text = $"{SelectedUnit.NameEn} ({SelectedUnit.RankRaw}): {startText} – {endText}";
            try { Clipboard.SetText(text); } catch { /* ignore */ }
        }

        /// <summary>重置：清除选中、搜索框、年龄反查结果，回到总览状态。</summary>
        [RelayCommand]
        private void Reset()
        {
            SearchText = string.Empty;
            AgeLookupMa = 251.9;
            AgeLookupResultText = string.Empty;
            HasAgeLookupResult = false;
            SelectedUnit = null;
            Ancestors.Clear();
            Children.Clear();
            AgeLookupPath.Clear();
            ExpandToPeriod();
        }

        // ── 私有方法 ─────────────────────────────────────────────────────────

        private void LoadRoots()
        {
            RootUnits.Clear();
            RootNodes.Clear();
            _nodesById.Clear();

            foreach (var root in ChronostratDataService.GetRoots())
            {
                RootUnits.Add(root);
                var rootNode = CreateTreeNode(root, null);
                RootNodes.Add(rootNode);
            }

            // 默认自动展开至“纪 (Period)”级别，一览无余
            ExpandToPeriod();
        }

        private ChronostratTreeNodeViewModel CreateTreeNode(
            ChronostratUnitRecord unit, ChronostratTreeNodeViewModel? parent)
        {
            var node = new ChronostratTreeNodeViewModel(unit, parent);
            _nodesById[unit.Id] = node;

            foreach (var child in unit.Children)
            {
                var childNode = CreateTreeNode(child, node);
                node.Children.Add(childNode);
            }

            return node;
        }
    }
}
