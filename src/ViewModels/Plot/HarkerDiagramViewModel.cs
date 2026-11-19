using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Models.HarkerDiagram;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>图例位置（图例属于整张图，而不是某个面板）。</summary>
    public enum HarkerLegendMode
    {
        Below = 0,
        Above = 1,
        Right = 2,
        Panel = 3,
        Cell = 4,
        Left = 5
    }

    /// <summary>设置页里的一个变量 chip（可勾选，选作 X 变量时置灰）。</summary>
    public partial class HarkerVariableItemViewModel : ObservableObject
    {
        public HarkerVariable Variable { get; }

        public string Key => Variable.Key;

        public string Label => Variable.Label;

        [ObservableProperty]
        private bool _isSelected;

        /// <summary>被选作 X 变量时不可再作为 Y 变量。</summary>
        [ObservableProperty]
        private bool _isEnabled = true;

        public HarkerVariableItemViewModel(HarkerVariable variable, bool isSelected)
        {
            Variable = variable;
            _isSelected = isSelected;
        }
    }

    /// <summary>行 × 列 预设按钮。</summary>
    public sealed class HarkerLayoutPreset
    {
        public required int Rows { get; init; }
        public required int Columns { get; init; }
        public required string Text { get; init; }
        public bool IsActive { get; init; }
    }

    /// <summary>布局预览里的一个格子。</summary>
    public partial class HarkerPreviewCell : ObservableObject
    {
        public required int Row { get; init; }
        public required int Column { get; init; }

        /// <summary>面板序号（从 1 开始），空格为 0。</summary>
        public int Number { get; init; }

        public string Label { get; init; } = string.Empty;

        public HarkerVariableItemViewModel? Item { get; init; }

        public bool IsEmpty => Item == null;

        /// <summary>图例占用该空格。</summary>
        public bool IsLegendCell { get; init; }

        /// <summary>图例嵌入该面板。</summary>
        public bool IsLegendEmbedded { get; init; }

        [ObservableProperty]
        private bool _isPicked;
    }

    /// <summary>图例位置选项。</summary>
    public partial class HarkerLegendOption : ObservableObject
    {
        public required HarkerLegendMode Mode { get; init; }

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private bool _isEnabled = true;
    }

    /// <summary>进入绘图后的一个面板描述。</summary>
    public sealed record HarkerPanelSpec(HarkerVariable Variable, int Row, int Column, int Index);

    /// <summary>
    /// Harker 多联图设置页的 ViewModel（与 <see cref="SpiderDiagramViewModel"/> 同级）。
    /// 决定数据表列、面板数量与排版；进入绘图模式后，由 MainPlotViewModel 按这里的设置构建多联图。
    /// </summary>
    public partial class HarkerDiagramViewModel : ObservableObject
    {
        /// <summary>行数 / 列数的下限。</summary>
        public const int MinRowsOrColumns = 1;

        /// <summary>行数 / 列数的上限。</summary>
        public const int MaxRowsOrColumns = 8;

        /// <summary>行 / 列数量范围的提示文字。</summary>
        public string RowsColumnsRangeText =>
            Format("harker_rows_columns_range", "范围 {0}–{1}", MinRowsOrColumns, MaxRowsOrColumns);

        /// <summary>主列表，顺序即“当前绘图顺序”（只有选中且未被 X 占用的才会出面板）。</summary>
        private readonly List<HarkerVariableItemViewModel> _allItems = new();

        private bool _suspend;
        private string? _pickedKey;
        private string? _fitMessage;

        public HarkerDiagramViewModel()
        {
            foreach (var variable in HarkerVariableCatalog.All)
            {
                var item = new HarkerVariableItemViewModel(variable, HarkerVariableCatalog.DefaultYKeys.Contains(variable.Key));
                item.PropertyChanged += OnItemPropertyChanged;
                _allItems.Add(item);
            }

            MajorItems = new(_allItems.Where(i => i.Variable.Group == HarkerVariableGroup.Major));
            DerivedItems = new(_allItems.Where(i => i.Variable.Group == HarkerVariableGroup.Derived));
            TraceItems = new(_allItems.Where(i => i.Variable.Group == HarkerVariableGroup.Trace));

            LegendOptions =
            [
                new HarkerLegendOption { Mode = HarkerLegendMode.Below },
                new HarkerLegendOption { Mode = HarkerLegendMode.Above },
                new HarkerLegendOption { Mode = HarkerLegendMode.Left },
                new HarkerLegendOption { Mode = HarkerLegendMode.Right },
                new HarkerLegendOption { Mode = HarkerLegendMode.Panel },
                new HarkerLegendOption { Mode = HarkerLegendMode.Cell },
            ];

            LanguageService.Instance.PropertyChanged += OnAppLanguageChanged;
            ApplyLocalizedTexts();
            Recompute();
        }

        private void OnAppLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Item[]")
                return;

            ApplyLocalizedTexts();
            OnPropertyChanged(nameof(RowsColumnsRangeText));
            Recompute();
        }

        private void ApplyLocalizedTexts()
        {
            foreach (var option in LegendOptions)
            {
                option.Title = option.Mode switch
                {
                    HarkerLegendMode.Below => Text("harker_legend_below", "下方"),
                    HarkerLegendMode.Above => Text("harker_legend_above", "上方"),
                    HarkerLegendMode.Left => Text("harker_legend_left", "左侧"),
                    HarkerLegendMode.Right => Text("harker_legend_right", "右侧"),
                    HarkerLegendMode.Panel => Text("harker_legend_embed", "嵌入面板"),
                    HarkerLegendMode.Cell => Text("harker_legend_cell", "占用空格"),
                    _ => option.Title
                };

                if (option.Mode == HarkerLegendMode.Panel)
                    option.Description = Text("harker_legend_embed_corner", "第 1 个面板右上角");
            }
        }

        private static string Text(string key, string fallback)
        {
            string value = LanguageService.Instance[key];
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static string Format(string key, string fallback, params object[] args)
        {
            try
            {
                return string.Format(Text(key, fallback), args);
            }
            catch (FormatException)
            {
                return string.Format(fallback, args);
            }
        }

        #region 变量

        public IReadOnlyList<HarkerVariable> XOptions => HarkerVariableCatalog.XOptions;

        public ObservableCollection<HarkerVariableItemViewModel> MajorItems { get; }

        public ObservableCollection<HarkerVariableItemViewModel> DerivedItems { get; }

        public ObservableCollection<HarkerVariableItemViewModel> TraceItems { get; }

        /// <summary>当前绘图顺序（选中的 Y 变量，面板排列顺序 = 数据表列顺序）。</summary>
        public ObservableCollection<HarkerVariableItemViewModel> PlotOrderItems { get; } = new();

        [ObservableProperty]
        private HarkerVariable _selectedXVariable = HarkerVariableCatalog.XOptions[0];

        [ObservableProperty]
        private HarkerVariableItemViewModel? _selectedOrderItem;

        partial void OnSelectedXVariableChanged(HarkerVariable value)
        {
            if (value == null || _suspend)
                return;

            Recompute();
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_suspend || e.PropertyName != nameof(HarkerVariableItemViewModel.IsSelected))
                return;

            Recompute();
        }

        [RelayCommand]
        private void SelectAllVariables()
        {
            SetAllSelected(true);
        }

        [RelayCommand]
        private void DeselectAllVariables()
        {
            SetAllSelected(false);
        }

        private void SetAllSelected(bool selected)
        {
            RunSuspended(() =>
            {
                foreach (var item in _allItems)
                    item.IsSelected = selected;
            });
        }

        [RelayCommand]
        private void MoveVariableUp(HarkerVariableItemViewModel? item) => MoveVariable(item, -1);

        [RelayCommand]
        private void MoveVariableDown(HarkerVariableItemViewModel? item) => MoveVariable(item, 1);

        private void MoveVariable(HarkerVariableItemViewModel? item, int delta)
        {
            if (item == null)
                return;

            int index = PlotOrderItems.IndexOf(item);
            int neighborIndex = index + delta;
            if (index < 0 || neighborIndex < 0 || neighborIndex >= PlotOrderItems.Count)
                return;

            SwapInMasterOrder(item, PlotOrderItems[neighborIndex]);
            Recompute();
            SelectedOrderItem = item;
        }

        private void SwapInMasterOrder(HarkerVariableItemViewModel a, HarkerVariableItemViewModel b)
        {
            int ia = _allItems.IndexOf(a);
            int ib = _allItems.IndexOf(b);
            if (ia < 0 || ib < 0)
                return;

            (_allItems[ia], _allItems[ib]) = (_allItems[ib], _allItems[ia]);
        }

        /// <summary>恢复到默认设置。</summary>
        [RelayCommand]
        private void Reset()
        {
            RunSuspended(() =>
            {
                // 主列表恢复为目录顺序
                var ordered = HarkerVariableCatalog.All
                    .Select(v => _allItems.First(i => i.Key == v.Key))
                    .ToList();
                _allItems.Clear();
                _allItems.AddRange(ordered);

                foreach (var item in _allItems)
                    item.IsSelected = HarkerVariableCatalog.DefaultYKeys.Contains(item.Key);

                SelectedXVariable = HarkerVariableCatalog.XOptions[0];
                Rows = 3;
                Columns = 3;
                FillByColumn = false;
                ShareXAxis = false;
                LegendModeIndex = (int)HarkerLegendMode.Below;
                _pickedKey = null;
                SelectedOrderItem = null;
            });
        }

        #endregion

        #region 布局

        [ObservableProperty]
        private int _rows = 3;

        [ObservableProperty]
        private int _columns = 3;

        [ObservableProperty]
        private bool _fillByColumn;

        [ObservableProperty]
        private bool _shareXAxis;

        [ObservableProperty]
        private int _legendModeIndex = (int)HarkerLegendMode.Below;

        [ObservableProperty]
        private string _layoutMessage = string.Empty;

        [ObservableProperty]
        private bool _layoutMessageIsWarning;

        [ObservableProperty]
        private string _legendNote = string.Empty;

        [ObservableProperty]
        private string _summaryText = string.Empty;

        [ObservableProperty]
        private bool _canEnter;

        public bool FillByRow
        {
            get => !FillByColumn;
            set
            {
                if (value)
                    FillByColumn = false;
            }
        }

        public IReadOnlyList<HarkerLegendOption> LegendOptions { get; }

        public ObservableCollection<HarkerLayoutPreset> LayoutPresets { get; } = new();

        public ObservableCollection<HarkerPreviewCell> PreviewCells { get; } = new();

        public HarkerLegendMode LegendMode => LegendOptions[Math.Clamp(LegendModeIndex, 0, LegendOptions.Count - 1)].Mode;

        /// <summary>“占用空格”但没有空格时，按“图下方”处理。</summary>
        public HarkerLegendMode EffectiveLegendMode =>
            LegendMode == HarkerLegendMode.Cell && EmptyCellCount == 0 ? HarkerLegendMode.Below : LegendMode;

        public int PanelCount => PlotOrderItems.Count;

        public int EmptyCellCount => Math.Max(0, Rows * Columns - PanelCount);

        public bool IsLegendAbove => EffectiveLegendMode == HarkerLegendMode.Above;

        public bool IsLegendBelow => EffectiveLegendMode == HarkerLegendMode.Below;

        public bool IsLegendRight => EffectiveLegendMode == HarkerLegendMode.Right;

        public bool IsLegendLeft => EffectiveLegendMode == HarkerLegendMode.Left;

        partial void OnRowsChanged(int value)
        {
            IncreaseRowsCommand.NotifyCanExecuteChanged();
            DecreaseRowsCommand.NotifyCanExecuteChanged();
            OnLayoutSettingChanged();
        }

        partial void OnColumnsChanged(int value)
        {
            IncreaseColumnsCommand.NotifyCanExecuteChanged();
            DecreaseColumnsCommand.NotifyCanExecuteChanged();
            OnLayoutSettingChanged();
        }

        partial void OnShareXAxisChanged(bool value) => OnLayoutSettingChanged();

        partial void OnLegendModeIndexChanged(int value)
        {
            // ListBox / RadioButton 初始化时可能短暂写入 -1，忽略
            if (value < 0)
                return;

            OnLayoutSettingChanged();
        }

        partial void OnFillByColumnChanged(bool value)
        {
            OnPropertyChanged(nameof(FillByRow));
            OnLayoutSettingChanged();
        }

        private void OnLayoutSettingChanged()
        {
            if (_suspend)
                return;

            Recompute();
        }

        private bool CanIncreaseRows() => Rows < MaxRowsOrColumns;

        private bool CanDecreaseRows() => Rows > MinRowsOrColumns;

        private bool CanIncreaseColumns() => Columns < MaxRowsOrColumns;

        private bool CanDecreaseColumns() => Columns > MinRowsOrColumns;

        [RelayCommand(CanExecute = nameof(CanIncreaseRows))]
        private void IncreaseRows() => Rows = Math.Min(MaxRowsOrColumns, Rows + 1);

        [RelayCommand(CanExecute = nameof(CanDecreaseRows))]
        private void DecreaseRows() => Rows = Math.Max(MinRowsOrColumns, Rows - 1);

        [RelayCommand(CanExecute = nameof(CanIncreaseColumns))]
        private void IncreaseColumns() => Columns = Math.Min(MaxRowsOrColumns, Columns + 1);

        [RelayCommand(CanExecute = nameof(CanDecreaseColumns))]
        private void DecreaseColumns() => Columns = Math.Max(MinRowsOrColumns, Columns - 1);

        [RelayCommand]
        private void ApplyLayoutPreset(HarkerLayoutPreset? preset)
        {
            if (preset == null)
                return;

            RunSuspended(() =>
            {
                Rows = preset.Rows;
                Columns = preset.Columns;
            });
        }

        /// <summary>点击预览格子：第一次点击选中，第二次点击与之互换位置。</summary>
        [RelayCommand]
        private void PickPreviewCell(HarkerPreviewCell? cell)
        {
            if (cell?.Item == null)
                return;

            string key = cell.Item.Key;
            if (_pickedKey == null)
            {
                _pickedKey = key;
            }
            else if (_pickedKey == key)
            {
                _pickedKey = null;
            }
            else
            {
                var first = _allItems.First(i => i.Key == _pickedKey);
                SwapInMasterOrder(first, cell.Item);
                _pickedKey = null;
            }

            Recompute();
        }

        #endregion

        #region 面板规格（供进入绘图时使用）

        /// <summary>X 变量。</summary>
        public HarkerVariable XVariable => SelectedXVariable;

        /// <summary>按当前设置排出每个面板所在的行列。</summary>
        public IReadOnlyList<HarkerPanelSpec> BuildPanelSpecs()
        {
            var specs = new List<HarkerPanelSpec>();
            for (int i = 0; i < PlotOrderItems.Count; i++)
            {
                (int row, int column) = GetCellOfIndex(i);
                if (row >= Rows || column >= Columns)
                    continue;

                specs.Add(new HarkerPanelSpec(PlotOrderItems[i].Variable, row, column, specs.Count));
            }

            return specs;
        }

        /// <summary>“占用空格”图例所在的格子（取最后一个空格）；不适用时返回 null。</summary>
        public (int Row, int Column)? GetLegendCell()
        {
            if (EffectiveLegendMode != HarkerLegendMode.Cell)
                return null;

            var occupied = BuildPanelSpecs().Select(s => (s.Row, s.Column)).ToHashSet();
            (int, int)? last = null;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (!occupied.Contains((r, c)))
                        last = (r, c);
                }
            }

            return last;
        }

        private (int Row, int Column) GetCellOfIndex(int index)
        {
            return FillByColumn
                ? (index % Rows, index / Rows)
                : (index / Columns, index % Columns);
        }

        #endregion

        #region 重新计算

        private void RunSuspended(Action action)
        {
            bool previous = _suspend;
            _suspend = true;
            try
            {
                action();
            }
            finally
            {
                _suspend = previous;
            }

            if (!_suspend)
                Recompute();
        }

        /// <summary>设置变化后统一重算：顺序、行列、预设、预览、提示。</summary>
        private void Recompute()
        {
            bool previous = _suspend;
            _suspend = true;
            try
            {
                // X 变量不可再作为 Y
                foreach (var item in _allItems)
                    item.IsEnabled = !string.Equals(item.Key, SelectedXVariable.Key, StringComparison.OrdinalIgnoreCase);

                SyncPlotOrder();

                Rows = Math.Clamp(Rows, MinRowsOrColumns, MaxRowsOrColumns);
                Columns = Math.Clamp(Columns, MinRowsOrColumns, MaxRowsOrColumns);

                int n = Math.Max(1, PlotOrderItems.Count);
                _fitMessage = null;
                if (Rows * Columns < n)
                {
                    int needRows = (int)Math.Ceiling(n / (double)Columns);
                    if (needRows <= MaxRowsOrColumns)
                    {
                        _fitMessage = Format(
                            "harker_fit_rows",
                            "格子不够（{0}×{1}={2} < {3}），已自动补为 {4} 行",
                            Rows, Columns, Rows * Columns, n, needRows);
                        Rows = needRows;
                    }
                    else
                    {
                        int needColumns = (int)Math.Ceiling(n / (double)Rows);
                        needColumns = Math.Min(MaxRowsOrColumns, needColumns);
                        _fitMessage = Format(
                            "harker_fit_columns",
                            "格子不够（{0}×{1}={2} < {3}），已自动补为 {4} 列",
                            Rows, Columns, Rows * Columns, n, needColumns);
                        Columns = needColumns;
                    }
                }

                if (_pickedKey != null && !PlotOrderItems.Any(i => i.Key == _pickedKey))
                    _pickedKey = null;

                RebuildPresets(n);
                UpdateLegendOptions();
                RebuildPreview();
                UpdateMessages();
            }
            finally
            {
                _suspend = previous;
            }

            OnPropertyChanged(nameof(PanelCount));
            OnPropertyChanged(nameof(EmptyCellCount));
            OnPropertyChanged(nameof(LegendMode));
            OnPropertyChanged(nameof(EffectiveLegendMode));
            OnPropertyChanged(nameof(IsLegendAbove));
            OnPropertyChanged(nameof(IsLegendBelow));
            OnPropertyChanged(nameof(IsLegendRight));
            OnPropertyChanged(nameof(IsLegendLeft));
            OnPropertyChanged(nameof(XVariable));
        }

        /// <summary>让 PlotOrderItems 与目标顺序一致，尽量用 Move/Insert 以保留列表选中项。</summary>
        private void SyncPlotOrder()
        {
            var target = _allItems.Where(i => i.IsSelected && i.IsEnabled).ToList();

            for (int i = PlotOrderItems.Count - 1; i >= 0; i--)
            {
                if (!target.Contains(PlotOrderItems[i]))
                    PlotOrderItems.RemoveAt(i);
            }

            for (int i = 0; i < target.Count; i++)
            {
                if (i < PlotOrderItems.Count && ReferenceEquals(PlotOrderItems[i], target[i]))
                    continue;

                int existing = PlotOrderItems.IndexOf(target[i]);
                if (existing >= 0)
                    PlotOrderItems.Move(existing, i);
                else
                    PlotOrderItems.Insert(i, target[i]);
            }

            if (SelectedOrderItem != null && !PlotOrderItems.Contains(SelectedOrderItem))
                SelectedOrderItem = null;
        }

        private void RebuildPresets(int n)
        {
            var seen = new HashSet<(int, int)>();
            var list = new List<(int Rows, int Columns)>();

            // force：即使当前面板数放不下也列出（点击后会自动补行 / 列并给出提示）
            void Add(int r, int c, bool force = false)
            {
                if (r < 1 || c < 1 || r > MaxRowsOrColumns || c > MaxRowsOrColumns || (!force && r * c < n))
                    return;

                if (seen.Add((r, c)))
                    list.Add((r, c));
            }

            int c0 = (int)Math.Ceiling(Math.Sqrt(n));
            Add((int)Math.Ceiling(n / (double)c0), c0);
            Add((int)Math.Ceiling(n / 3.0), 3);
            Add(2, (int)Math.Ceiling(n / 2.0));
            Add((int)Math.Ceiling(n / 2.0), 2);
            Add(2, 2, force: true);

            LayoutPresets.Clear();
            int index = 0;
            foreach (var (r, c) in list.Take(6))
            {
                LayoutPresets.Add(new HarkerLayoutPreset
                {
                    Rows = r,
                    Columns = c,
                    Text = $"{r}×{c}",
                    IsActive = Rows == r && Columns == c
                });
                index++;
            }
        }

        private void UpdateLegendOptions()
        {
            var cellOption = LegendOptions.First(o => o.Mode == HarkerLegendMode.Cell);
            bool hasEmpty = Rows * Columns - PlotOrderItems.Count > 0;
            cellOption.IsEnabled = hasEmpty;
            cellOption.Description = hasEmpty ? string.Empty : Text("harker_no_empty_cell", "无空格");

            LegendNote = LegendMode == HarkerLegendMode.Cell && !hasEmpty
                ? Text("harker_legend_no_cell", "当前布局没有空格，将按“下方”显示。")
                : string.Empty;
        }

        private void RebuildPreview()
        {
            PreviewCells.Clear();

            var specs = BuildPanelSpecs();
            var byCell = specs.ToDictionary(s => (s.Row, s.Column));
            var legendCell = GetLegendCell();
            bool embedded = EffectiveLegendMode == HarkerLegendMode.Panel;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (byCell.TryGetValue((r, c), out var spec))
                    {
                        var item = PlotOrderItems[spec.Index];
                        PreviewCells.Add(new HarkerPreviewCell
                        {
                            Row = r,
                            Column = c,
                            Number = spec.Index + 1,
                            Label = spec.Variable.Label,
                            Item = item,
                            IsLegendEmbedded = embedded && spec.Index == 0,
                            IsPicked = _pickedKey == item.Key
                        });
                    }
                    else
                    {
                        bool isLegendCell = legendCell.HasValue && legendCell.Value.Row == r && legendCell.Value.Column == c;
                        PreviewCells.Add(new HarkerPreviewCell
                        {
                            Row = r,
                            Column = c,
                            IsLegendCell = isLegendCell,
                            Label = isLegendCell ? Text("legend", "图例") : string.Empty
                        });
                    }
                }
            }
        }

        private void UpdateMessages()
        {
            int n = PlotOrderItems.Count;
            int cells = Rows * Columns;
            int empty = Math.Max(0, cells - n);

            if (n == 0)
            {
                LayoutMessage = Text("harker_select_y", "请至少选择一个 Y 变量");
                LayoutMessageIsWarning = true;
            }
            else if (_fitMessage != null)
            {
                LayoutMessage = _fitMessage;
                LayoutMessageIsWarning = true;
            }
            else
            {
                LayoutMessage = empty > 0
                    ? Format("harker_layout_with_empty", "{0}×{1} = {2} 格，{3} 个面板，空 {4} 格", Rows, Columns, cells, n, empty)
                    : Format("harker_layout_full", "{0}×{1} = {2} 格，刚好放满", Rows, Columns, cells);
                LayoutMessageIsWarning = false;
            }

            CanEnter = n > 0;
            SummaryText = Format("harker_summary", "{0} 个面板 · {1}×{2}", n, Rows, Columns);
        }

        #endregion
    }
}
