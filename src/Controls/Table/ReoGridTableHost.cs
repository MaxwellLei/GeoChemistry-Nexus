using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Services;
using unvell.ReoGrid;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 完整的数据工作表：标题、刷新、单元格编辑栏、图例、表格与细滚动条。
    /// 用法：<c>&lt;ctrl:ReoGridTableHost Style="{StaticResource ReoGridTableHostStyle}"&gt;&lt;rg:ReoGridControl x:Name="..."/&gt;&lt;/ctrl:ReoGridTableHost&gt;</c>。
    /// 只负责外观，不含业务逻辑；表格内容、列头文字、数据区配色由各自的 ViewModel 通过 <see cref="ReoGridTableAppearance"/> 设置。
    /// <para>以下能力都是可选的，模块不用就不会出现：</para>
    /// <list type="bullet">
    /// <item><see cref="Inspector"/>：右侧检查器，内容由模块自己的 XAML 提供（计算详情、数据检查……）。</item>
    /// <item>行头状态点：实现 <see cref="IWorksheetRowStatusProvider"/> 并通过 <see cref="ReoGridTableAppearance.SetRowStatusProvider"/> 注册。</item>
    /// <item>列头分组行 / 单位：实现 <see cref="IWorksheetColumnHeaderProvider"/> 并通过 <see cref="ReoGridTableAppearance.SetColumnHeaderProvider"/> 注册。</item>
    /// </list>
    /// 底部状态栏（选区统计）、撤销 / 重做按钮对所有使用本控件的表格生效。
    /// </summary>
    public class ReoGridTableHost : ContentControl
    {
        private const string RootPart = "PART_Root";
        private const string HorizontalScrollPart = "PART_HorizontalScroll";
        private const string VerticalScrollPart = "PART_VerticalScroll";

        /// <summary>
        /// 为 true 时，在表格初始化以及每次 <c>Worksheet.Reset()</c> 之后自动套用数据区默认字体、颜色与行高。
        /// 适合内容由模板动态重建、没有自己表头样式的表格；已自行设置表头样式的表格保持默认 false，改由 ViewModel 显式调用。
        /// </summary>
        public static readonly DependencyProperty AutoStyleCellsProperty =
            DependencyProperty.Register(nameof(AutoStyleCells), typeof(bool), typeof(ReoGridTableHost), new PropertyMetadata(false));

        public static readonly DependencyProperty ShowWorksheetBarProperty =
            DependencyProperty.Register(nameof(ShowWorksheetBar), typeof(bool), typeof(ReoGridTableHost), new PropertyMetadata(true));
        public static readonly DependencyProperty ToolbarActionsProperty =
            DependencyProperty.Register(nameof(ToolbarActions), typeof(object), typeof(ReoGridTableHost));
        public static readonly DependencyProperty ResetLayoutCommandProperty =
            DependencyProperty.Register(nameof(ResetLayoutCommand), typeof(ICommand), typeof(ReoGridTableHost));
        public static readonly DependencyProperty ResetLayoutCommandParameterProperty =
            DependencyProperty.Register(nameof(ResetLayoutCommandParameter), typeof(object), typeof(ReoGridTableHost));
        public static readonly DependencyProperty UseBuiltInCellEditorProperty =
            DependencyProperty.Register(nameof(UseBuiltInCellEditor), typeof(bool), typeof(ReoGridTableHost), new PropertyMetadata(true));
        public static readonly DependencyProperty SelectedCellAddressProperty =
            DependencyProperty.Register(nameof(SelectedCellAddress), typeof(string), typeof(ReoGridTableHost), new PropertyMetadata("--"));
        public static readonly DependencyProperty SelectedCellContentProperty =
            DependencyProperty.Register(nameof(SelectedCellContent), typeof(string), typeof(ReoGridTableHost),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (d, e) => ((ReoGridTableHost)d).WriteSelectedCell((string)e.NewValue)));
        public static readonly DependencyProperty IsSelectedCellReadOnlyProperty =
            DependencyProperty.Register(nameof(IsSelectedCellReadOnly), typeof(bool), typeof(ReoGridTableHost), new PropertyMetadata(true));

        /// <summary>右侧检查器的内容（任意 UI）。为 null 时不显示检查器及其开关按钮。</summary>
        public static readonly DependencyProperty InspectorProperty =
            DependencyProperty.Register(nameof(Inspector), typeof(object), typeof(ReoGridTableHost),
                new PropertyMetadata(null, (d, e) =>
                {
                    d.SetValue(HasInspectorProperty, e.NewValue != null);
                    ((ReoGridTableHost)d).UpdateLegend();
                    ((ReoGridTableHost)d).ApplyInspectorState(animate: false);
                }));
        /// <summary>是否设置了检查器内容（由控件维护，供模板触发器使用）。</summary>
        public static readonly DependencyProperty HasInspectorProperty =
            DependencyProperty.Register(nameof(HasInspector), typeof(bool), typeof(ReoGridTableHost), new PropertyMetadata(false));
        public static readonly DependencyProperty InspectorTitleProperty =
            DependencyProperty.Register(nameof(InspectorTitle), typeof(string), typeof(ReoGridTableHost), new PropertyMetadata(string.Empty));
        /// <summary>检查器是否展开；可由 ViewModel 双向绑定。</summary>
        public static readonly DependencyProperty IsInspectorOpenProperty =
            DependencyProperty.Register(nameof(IsInspectorOpen), typeof(bool), typeof(ReoGridTableHost),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (d, _) => ((ReoGridTableHost)d).ApplyInspectorState(animate: true)));
        public static readonly DependencyProperty InspectorWidthProperty =
            DependencyProperty.Register(nameof(InspectorWidth), typeof(double), typeof(ReoGridTableHost),
                new PropertyMetadata(304d, (d, _) => ((ReoGridTableHost)d).ApplyInspectorState(animate: false)));

        public static readonly DependencyProperty ShowStatusBarProperty =
            DependencyProperty.Register(nameof(ShowStatusBar), typeof(bool), typeof(ReoGridTableHost),
                new PropertyMetadata(true, (d, _) => ((ReoGridTableHost)d).QueueStatusRefresh()));
        /// <summary>状态栏中的选区统计文字（由控件维护）。</summary>
        public static readonly DependencyProperty SelectionSummaryProperty =
            DependencyProperty.Register(nameof(SelectionSummary), typeof(string), typeof(ReoGridTableHost), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty CanUndoProperty =
            DependencyProperty.Register(nameof(CanUndo), typeof(bool), typeof(ReoGridTableHost), new PropertyMetadata(false));
        public static readonly DependencyProperty CanRedoProperty =
            DependencyProperty.Register(nameof(CanRedo), typeof(bool), typeof(ReoGridTableHost), new PropertyMetadata(false));

        public object? Inspector { get => GetValue(InspectorProperty); set => SetValue(InspectorProperty, value); }
        public bool HasInspector { get => (bool)GetValue(HasInspectorProperty); private set => SetValue(HasInspectorProperty, value); }
        public string InspectorTitle { get => (string)GetValue(InspectorTitleProperty); set => SetValue(InspectorTitleProperty, value); }
        public bool IsInspectorOpen { get => (bool)GetValue(IsInspectorOpenProperty); set => SetValue(IsInspectorOpenProperty, value); }
        public double InspectorWidth { get => (double)GetValue(InspectorWidthProperty); set => SetValue(InspectorWidthProperty, value); }
        public bool ShowStatusBar { get => (bool)GetValue(ShowStatusBarProperty); set => SetValue(ShowStatusBarProperty, value); }
        public string SelectionSummary { get => (string)GetValue(SelectionSummaryProperty); private set => SetValue(SelectionSummaryProperty, value); }
        public bool CanUndo { get => (bool)GetValue(CanUndoProperty); private set => SetValue(CanUndoProperty, value); }
        public bool CanRedo { get => (bool)GetValue(CanRedoProperty); private set => SetValue(CanRedoProperty, value); }

        public bool ShowWorksheetBar { get => (bool)GetValue(ShowWorksheetBarProperty); set => SetValue(ShowWorksheetBarProperty, value); }
        public object? ToolbarActions { get => GetValue(ToolbarActionsProperty); set => SetValue(ToolbarActionsProperty, value); }
        public ICommand? ResetLayoutCommand { get => (ICommand?)GetValue(ResetLayoutCommandProperty); set => SetValue(ResetLayoutCommandProperty, value); }
        public object? ResetLayoutCommandParameter { get => GetValue(ResetLayoutCommandParameterProperty); set => SetValue(ResetLayoutCommandParameterProperty, value); }
        /// <summary>有独立编辑策略的模块可关闭内置编辑器，并绑定下面三个属性。</summary>
        public bool UseBuiltInCellEditor { get => (bool)GetValue(UseBuiltInCellEditorProperty); set => SetValue(UseBuiltInCellEditorProperty, value); }
        public string SelectedCellAddress { get => (string)GetValue(SelectedCellAddressProperty); set => SetValue(SelectedCellAddressProperty, value); }
        public string SelectedCellContent { get => (string)GetValue(SelectedCellContentProperty); set => SetValue(SelectedCellContentProperty, value); }
        public bool IsSelectedCellReadOnly { get => (bool)GetValue(IsSelectedCellReadOnlyProperty); set => SetValue(IsSelectedCellReadOnlyProperty, value); }

        private Grid? _root;
        private ScrollBar? _horizontalScroll, _verticalScroll;
        private ReoGridScrollPresenter? _presenter;
        private ReoGridControl? _grid;
        private Worksheet? _sheet;
        private bool _attached;
        private bool _updatingEditor;
        private Button? _resetButton, _inspectorCloseButton, _undoButton, _redoButton;
        private System.Windows.Controls.Border? _inspectorPanel;
        private Thumb? _inspectorResizer;

        /// <summary>检查器可拖到的最小宽度。</summary>
        private const double MinInspectorWidth = 240;
        /// <summary>检查器最大宽度占软件窗口宽度的比例。</summary>
        private const double MaxInspectorWidthRatio = 2d / 3d;
        private int _inspectorAnimationVersion;
        private FrameworkElement? _legend;
        private bool _statusRefreshPending;

        /// <summary>选区统计的最大单元格数，超过后不统计，避免整表选中时拖慢界面。</summary>
        private const int MaxSummaryCells = 60000;

        public bool AutoStyleCells
        {
            get => (bool)GetValue(AutoStyleCellsProperty);
            set => SetValue(AutoStyleCellsProperty, value);
        }

        /// <summary>被承载的 ReoGrid 控件。</summary>
        public ReoGridControl? WorksheetGrid => _grid ?? Content as ReoGridControl;

        public override void OnApplyTemplate()
        {
            if (_resetButton != null) _resetButton.Click -= ResetLayout;
            if (_inspectorCloseButton != null) _inspectorCloseButton.Click -= CloseInspector;
            if (_undoButton != null) _undoButton.Click -= UndoClick;
            if (_redoButton != null) _redoButton.Click -= RedoClick;
            if (_inspectorResizer != null) _inspectorResizer.DragDelta -= InspectorResizerDragDelta;
            base.OnApplyTemplate();
            _inspectorResizer = GetTemplateChild("PART_InspectorResizer") as Thumb;
            if (_inspectorResizer != null) _inspectorResizer.DragDelta += InspectorResizerDragDelta;
            _root = GetTemplateChild(RootPart) as Grid;
            _horizontalScroll = GetTemplateChild(HorizontalScrollPart) as ScrollBar;
            _verticalScroll = GetTemplateChild(VerticalScrollPart) as ScrollBar;
            _resetButton = GetTemplateChild("PART_ResetLayout") as Button;
            if (_resetButton != null) _resetButton.Click += ResetLayout;
            _inspectorCloseButton = GetTemplateChild("PART_InspectorClose") as Button;
            if (_inspectorCloseButton != null) _inspectorCloseButton.Click += CloseInspector;
            _undoButton = GetTemplateChild("PART_Undo") as Button;
            if (_undoButton != null) _undoButton.Click += UndoClick;
            _redoButton = GetTemplateChild("PART_Redo") as Button;
            if (_redoButton != null) _redoButton.Click += RedoClick;
            _legend = GetTemplateChild("PART_Legend") as FrameworkElement;
            _inspectorPanel = GetTemplateChild("PART_Inspector") as System.Windows.Controls.Border;
            ApplyInspectorState(animate: false);
            UpdateLegend();
            TryAttach();
        }

        /// <summary>
        /// 检查器展开 / 收起：外框宽度在 0 与 <see cref="InspectorWidth"/> 之间做动画，
        /// 左侧的表格与状态栏随之被挤压（Auto 列跟随外框宽度变化）。
        /// </summary>
        private void ApplyInspectorState(bool animate)
        {
            var panel = _inspectorPanel;
            if (panel == null) return;

            bool open = HasInspector && IsInspectorOpen;
            double target = open ? InspectorWidth : 0;
            int version = ++_inspectorAnimationVersion;
            double from = panel.ActualWidth;
            panel.BeginAnimation(WidthProperty, null);   // 终止正在进行的动画，从当前宽度继续

            if (!animate || !IsLoaded)
            {
                panel.Width = target;
                panel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            if (open) panel.Visibility = Visibility.Visible;
            panel.Width = target;   // 动画结束（FillBehavior.Stop）后自然回落到目标宽度，不会闪动
            var animation = new DoubleAnimation
            {
                From = from,
                To = target,
                Duration = TimeSpan.FromMilliseconds(open ? 240 : 200),
                EasingFunction = new CubicEase { EasingMode = open ? EasingMode.EaseOut : EasingMode.EaseIn },
                FillBehavior = FillBehavior.Stop,
            };
            animation.Completed += (_, _) =>
            {
                // 期间又触发了新的展开 / 收起时，旧动画的收尾不再生效。
                if (version == _inspectorAnimationVersion && !open) panel.Visibility = Visibility.Collapsed;
            };
            panel.BeginAnimation(WidthProperty, animation);
        }

        private void CloseInspector(object sender, RoutedEventArgs e) => IsInspectorOpen = false;

        /// <summary>检查器允许的最大宽度：软件窗口宽度的 2/3（取不到窗口时退回本控件宽度）。</summary>
        private double GetMaxInspectorWidth()
        {
            double reference = Window.GetWindow(this)?.ActualWidth ?? 0;
            if (reference <= 0) reference = ActualWidth;
            return reference > 0 ? Math.Max(MinInspectorWidth, reference * MaxInspectorWidthRatio) : double.PositiveInfinity;
        }

        private void InspectorResizerDragDelta(object sender, DragDeltaEventArgs e)
        {
            // 拖动条位于检查器左边缘：向左拖（HorizontalChange 为负）变宽。
            double width = InspectorWidth - e.HorizontalChange;
            InspectorWidth = Math.Clamp(width, MinInspectorWidth, GetMaxInspectorWidth());
            e.Handled = true;
        }

        /// <summary>窗口缩小后，已经超过 2/3 的检查器自动收回到上限以内。</summary>
        private void ClampInspectorWidth()
        {
            if (!HasInspector) return;
            double max = GetMaxInspectorWidth();
            if (InspectorWidth > max) InspectorWidth = max;
        }

        private void UndoClick(object sender, RoutedEventArgs e)
        {
            if (_grid is { } grid && grid.CanUndo()) grid.Undo();
            UpdateUndoState();
        }

        private void RedoClick(object sender, RoutedEventArgs e)
        {
            if (_grid is { } grid && grid.CanRedo()) grid.Redo();
            UpdateUndoState();
        }

        /// <summary>Ctrl+I：展开 / 收起检查器（仅在模块设置了检查器时生效）。</summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Handled || !HasInspector || e.Key != Key.I || Keyboard.Modifiers != ModifierKeys.Control) return;
            IsInspectorOpen = !IsInspectorOpen;
            e.Handled = true;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateLegend();
            ClampInspectorWidth();
        }

        private void UpdateLegend()
        {
            // 工具栏右侧每多一个元素（模块按钮 / 检查器开关 / 撤销重做），就需要更宽才放得下图例。
            double needed = (ToolbarActions == null ? 850 : 1040) + (HasInspector ? 120 : 0);
            if (_legend != null) _legend.Visibility = ActualWidth < needed ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ResetLayout(object sender, RoutedEventArgs e)
        {
            if (ResetLayoutCommand is { } command)
            {
                var parameter = ResetLayoutCommandParameter ?? WorksheetGrid;
                if (command.CanExecute(parameter)) command.Execute(parameter);
                return;
            }
            var sheet = WorksheetGrid?.CurrentWorksheet;
            if (sheet == null) return;
            sheet.SetRowsHeight(0, sheet.RowCount, ReoGridTableAppearance.BodyRowHeight);
            for (int c = 0; c < sheet.ColumnCount; c++)
            {
                if (ReoGridTableAppearance.GetColumnKind(sheet, c) == ReoGridColumnKind.Separator) continue;
                sheet.AutoFitColumnWidth(c);
                var width = ReoGridTableAppearance.GetHeaderColumnWidth(sheet.ColumnHeaders[c].Text, 70, ushort.MaxValue);
                sheet.SetColumnsWidth(c, 1, Math.Max(sheet.GetColumnWidth(c), width));
            }
            sheet.RequestInvalidate();
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            TryAttach();
        }

        private void TryAttach()
        {
            if (_attached || Content is not ReoGridControl grid
                || _root is null || _horizontalScroll is null || _verticalScroll is null) return;
            _attached = true;
            _grid = grid;
            ReoGridTableAppearance.ApplyControlStyle(grid);
            try
            {
                _presenter = new ReoGridScrollPresenter(grid, _root, _horizontalScroll, _verticalScroll);
            }
            catch (InvalidOperationException ex)
            {
                // 找不到 ReoGrid 的内置滚动条时退回原生滚动条，表格仍可使用。
                Trace.TraceWarning("ReoGridTableHost: " + ex.Message);
            }
            grid.CurrentWorksheetChanged += (_, _) => BindWorksheet();
            // 表头、行头、选区与结果区底色都取自主题色：切换主题后重新着色（弱引用订阅，无需手动注销）。
            WeakReferenceMessenger.Default.Register<ReoGridTableHost, AppThemeChangedMessage>(this,
                static (host, _) => host.OnThemeChanged());
            BindWorksheet();
        }

        private void OnThemeChanged()
        {
            if (_grid != null) ReoGridTableAppearance.ReapplyTheme(_grid);
        }

        private void BindWorksheet()
        {
            if (_sheet != null)
            {
                _sheet.Resetted -= WorksheetStructureChanged;
                _sheet.ColumnsInserted -= WorksheetColumnsChanged;
                _sheet.ColumnsDeleted -= WorksheetColumnsDeleted;
                _sheet.RowsInserted -= WorksheetRowsInserted;
                _sheet.SelectionRangeChanged -= SelectedCellChanged;
                _sheet.FocusPosChanged -= SelectedCellChanged;
                _sheet.CellDataChanged -= SelectedCellChanged;
                _sheet.SelectionRangeChanged -= StatusSourceChanged;
                _sheet.CellDataChanged -= StatusSourceChanged;
                _sheet.RangeDataChanged -= StatusSourceChanged;
            }
            _sheet = _grid?.CurrentWorksheet;
            if (_sheet != null)
            {
                _sheet.Resetted += WorksheetStructureChanged;
                _sheet.ColumnsInserted += WorksheetColumnsChanged;
                _sheet.ColumnsDeleted += WorksheetColumnsDeleted;
                _sheet.RowsInserted += WorksheetRowsInserted;
                _sheet.SelectionRangeChanged += SelectedCellChanged;
                _sheet.FocusPosChanged += SelectedCellChanged;
                _sheet.CellDataChanged += SelectedCellChanged;
                _sheet.SelectionRangeChanged += StatusSourceChanged;
                _sheet.CellDataChanged += StatusSourceChanged;
                _sheet.RangeDataChanged += StatusSourceChanged;
            }
            RefreshStyle(applyCellDefaults: AutoStyleCells);
            RefreshSelectedCell();
            QueueStatusRefresh();
        }

        private void SelectedCellChanged(object? sender, EventArgs e) => RefreshSelectedCell();

        // ---------- 状态栏（选区统计）与撤销 / 重做 ----------

        private void StatusSourceChanged(object? sender, EventArgs e) => QueueStatusRefresh();

        /// <summary>
        /// 计算写入大量单元格时会连续触发数据变化事件，这里合并成一次延后刷新，避免逐格重算统计。
        /// </summary>
        private void QueueStatusRefresh()
        {
            if (_statusRefreshPending) return;
            _statusRefreshPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _statusRefreshPending = false;
                UpdateSelectionSummary();
                UpdateUndoState();
            }));
        }

        private void UpdateUndoState()
        {
            CanUndo = _grid?.CanUndo() ?? false;
            CanRedo = _grid?.CanRedo() ?? false;
        }

        private void UpdateSelectionSummary()
        {
            var sheet = _sheet;
            string text = string.Empty;
            if (ShowStatusBar && sheet != null)
            {
                var range = sheet.SelectionRange;
                long cells = (long)range.Rows * range.Cols;
                if (cells > 1 && cells <= MaxSummaryCells) text = BuildSelectionSummary(sheet, range);
            }
            if (SelectionSummary != text) SelectionSummary = text;
        }

        private static string BuildSelectionSummary(Worksheet sheet, RangePosition range)
        {
            int count = 0, numeric = 0;
            double sum = 0, min = double.MaxValue, max = double.MinValue;
            int lastRow = Math.Min(range.EndRow, sheet.RowCount - 1), lastCol = Math.Min(range.EndCol, sheet.ColumnCount - 1);
            for (int row = Math.Max(0, range.Row); row <= lastRow; row++)
            {
                for (int col = Math.Max(0, range.Col); col <= lastCol; col++)
                {
                    var value = sheet.GetCellData(row, col);
                    if (value == null || (value is string text && text.Length == 0)) continue;
                    count++;
                    if (!InvariantNumberParser.TryParseCell(value, out double number)) continue;
                    numeric++;
                    sum += number;
                    if (number < min) min = number;
                    if (number > max) max = number;
                }
            }
            if (count == 0) return string.Empty;

            static string Label(string key, string fallback) => LanguageService.GetString(key, fallback);
            static string Number(double value) => value.ToString("0.00###", CultureInfo.InvariantCulture);
            var parts = new System.Collections.Generic.List<string> { $"{Label("sheet_stat_count", "Count")} {count}" };
            if (numeric > 0)
            {
                parts.Add($"{Label("sheet_stat_sum", "Sum")} {Number(sum)}");
                parts.Add($"{Label("sheet_stat_avg", "Average")} {Number(sum / numeric)}");
                parts.Add($"{Label("sheet_stat_min", "Min")} {Number(min)}");
                parts.Add($"{Label("sheet_stat_max", "Max")} {Number(max)}");
            }
            return string.Join("    ", parts);
        }

        private void WorksheetRowsInserted(object? sender, unvell.ReoGrid.Events.RowsInsertedEventArgs e)
        {
            // 新增的行需要同样套用行头状态点绘制。
            if (_grid != null) ReoGridTableAppearance.StyleRowHeaders(_grid);
        }

        private void RefreshSelectedCell()
        {
            if (!UseBuiltInCellEditor || _updatingEditor) return;
            _updatingEditor = true;
            try
            {
                var sheet = _sheet;
                var pos = sheet?.FocusPos ?? new CellPosition(-1, -1);
                bool valid = sheet != null && pos.Row >= 0 && pos.Col >= 0 && pos.Row < sheet.RowCount && pos.Col < sheet.ColumnCount;
                SetCurrentValue(SelectedCellAddressProperty, valid ? pos.ToAddress() : "--");
                SetCurrentValue(IsSelectedCellReadOnlyProperty, !valid || sheet!.Cells[pos.Row, pos.Col].IsReadOnly
                    || ReoGridTableAppearance.GetColumnKind(sheet!, pos.Col) != ReoGridColumnKind.Input);
                var cell = valid ? sheet!.Cells[pos.Row, pos.Col] : null;
                SetCurrentValue(SelectedCellContentProperty, cell == null ? string.Empty : cell.HasFormula
                    ? "=" + cell.Formula.TrimStart('=')
                    : Convert.ToString(sheet!.GetCellData(pos.Row, pos.Col), CultureInfo.InvariantCulture) ?? string.Empty);
            }
            finally { _updatingEditor = false; }
        }

        private void WriteSelectedCell(string value)
        {
            if (!UseBuiltInCellEditor || _updatingEditor || IsSelectedCellReadOnly || _sheet == null) return;
            var pos = _sheet.FocusPos;
            if (pos.Row < 0 || pos.Col < 0 || pos.Row >= _sheet.RowCount || pos.Col >= _sheet.ColumnCount
                || _sheet.Cells[pos.Row, pos.Col].IsReadOnly) return;
            _updatingEditor = true;
            try { _sheet[pos.Row, pos.Col] = value; }
            finally { _updatingEditor = false; }
        }

        private void WorksheetStructureChanged(object? sender, EventArgs e)
        {
            RefreshStyle(AutoStyleCells);
            RefreshSelectedCell();
        }
        private void WorksheetColumnsChanged(object? sender, unvell.ReoGrid.Events.ColumnsInsertedEventArgs e) => RefreshStyle(false);
        private void WorksheetColumnsDeleted(object? sender, unvell.ReoGrid.Events.ColumnsDeletedEventArgs e) => RefreshStyle(false);

        private void RefreshStyle(bool applyCellDefaults)
        {
            var grid = _grid;
            var sheet = grid?.CurrentWorksheet;
            if (grid is null || sheet is null) return;
            if (applyCellDefaults) ReoGridTableAppearance.ApplyCellDefaults(sheet);
            ReoGridTableAppearance.StyleColumnHeaders(grid);
            ReoGridTableAppearance.StyleRowHeaders(grid);
        }
    }
}
