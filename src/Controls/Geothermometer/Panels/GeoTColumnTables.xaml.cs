using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GeoChemistryNexus.Controls.GeothermometerEditorPanels
{
    /// <summary>
    /// 输入列 / 输出列表格。这里只处理纯界面行为（Tab 切换单元格、粘贴多行多列内容、
    /// 滚轮转发、新增列后聚焦），数据与命令全部在 GeothermometerEditorViewModel 中。
    /// </summary>
    public partial class GeoTColumnTables : UserControl
    {
        private GeothermometerEditorViewModel? _viewModel;

        public GeoTColumnTables()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            AddHandler(PreviewKeyDownEvent, new KeyEventHandler(OnCellPreviewKeyDown));
            DataObject.AddPastingHandler(this, OnCellPasting);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ColumnAdded -= OnColumnAdded;
                _viewModel.ExampleAdded -= OnExampleAdded;
            }

            _viewModel = e.NewValue as GeothermometerEditorViewModel;
            if (_viewModel != null)
            {
                _viewModel.ColumnAdded += OnColumnAdded;
                _viewModel.ExampleAdded += OnExampleAdded;
            }
        }

        // ==================== 新增示例后聚焦 ====================

        /// <summary>新增一条示例后，滚动到最左并聚焦第一个输入列的新示例单元格。</summary>
        private void OnExampleAdded(int row)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (_viewModel == null || row < 0 || row >= _viewModel.ExampleRows.Count || _viewModel.InputColumns.Count == 0)
                    return;

                var examples = _viewModel.InputColumns[0].Examples;
                if (row >= examples.Count) return;

                InputsEditor.UpdateLayout();
                InputColumnsScroll.ScrollToLeftEnd();
                if (FindExampleCell(InputsEditor, examples[row]) is { } cell)
                {
                    cell.Focus();
                    cell.SelectAll();
                    cell.BringIntoView();
                }
            }));
        }

        private static TextBox? FindExampleCell(DependencyObject root, EpmaEditorExampleCell example)
        {
            if (root is TextBox textBox && ReferenceEquals(textBox.DataContext, example))
                return textBox;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                if (FindExampleCell(VisualTreeHelper.GetChild(root, i), example) is { } found)
                    return found;
            }

            return null;
        }

        /// <summary>表格内滚轮优先滚动外层页面；按住 Shift 时交给表格做横向滚动。</summary>
        private void ColumnTablesMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) return;

            var pageScroll = FindAncestorScrollViewer(this);
            if (pageScroll == null) return;

            e.Handled = true;
            pageScroll.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = Mouse.MouseWheelEvent,
                Source = pageScroll
            });
        }

        private static ScrollViewer? FindAncestorScrollViewer(DependencyObject start)
        {
            for (DependencyObject? parent = VisualTreeHelper.GetParent(start);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is ScrollViewer scrollViewer)
                    return scrollViewer;
            }

            return null;
        }

        // ==================== 新增列后聚焦 ====================

        private void OnColumnAdded(EpmaEditorColumn column, bool isInput)
        {
            var table = isInput ? InputsEditor : OutputsEditor;
            var scroll = isInput ? InputColumnsScroll : OutputColumnsScroll;

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (!table.Items.Contains(column)) return;

                table.UpdateLayout();
                scroll.ScrollToRightEnd();
                if (table.ItemContainerGenerator.ContainerFromItem(column) is DependencyObject container
                    && FindFirstTextBox(container) is { } name)
                {
                    name.Focus();
                    name.SelectAll();
                    name.BringIntoView();
                }
            }));
        }

        private static TextBox? FindFirstTextBox(DependencyObject root)
        {
            if (root is TextBox textBox) return textBox;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                if (FindFirstTextBox(VisualTreeHelper.GetChild(root, i)) is { } found)
                    return found;
            }

            return null;
        }

        // ==================== Tab 在单元格间按行切换 ====================

        private void OnCellPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab
                || (Keyboard.Modifiers & ~ModifierKeys.Shift) != 0
                || e.OriginalSource is not TextBox cell)
                return;

            e.Handled = MoveColumnCellFocus(cell, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
        }

        private bool MoveColumnCellFocus(TextBox cell, bool backward)
        {
            ItemsControl? table = null;
            for (DependencyObject? parent = cell; parent != null; parent = VisualTreeHelper.GetParent(parent))
            {
                if (ReferenceEquals(parent, InputsEditor)) { table = InputsEditor; break; }
                if (ReferenceEquals(parent, OutputsEditor)) { table = OutputsEditor; break; }
            }

            if (table == null) return false;

            // 每个条目是一整列（含删除按钮与示例），这里改按“先行后列”的显示顺序遍历单元格。
            table.UpdateLayout();
            var cells = EnumerateTextBoxes(table)
                .Where(candidate => candidate.IsEnabled && candidate.Focusable && candidate.Visibility == Visibility.Visible)
                .OrderBy(candidate => candidate.TranslatePoint(new Point(), table).Y)
                .ThenBy(candidate => candidate.TranslatePoint(new Point(), table).X)
                .ToList();

            int current = cells.IndexOf(cell);
            if (current < 0) return false;

            int next = current + (backward ? -1 : 1);
            if (next < 0)
                return table.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));

            // 最后一个单元格交还给默认的焦点导航，便于离开表格
            if (next >= cells.Count) return false;

            var target = cells[next];
            target.Focus();
            target.SelectAll();
            target.BringIntoView();
            return true;
        }

        private static IEnumerable<TextBox> EnumerateTextBoxes(DependencyObject root)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is TextBox textBox)
                {
                    yield return textBox;
                }
                else
                {
                    foreach (var nested in EnumerateTextBoxes(child))
                        yield return nested;
                }
            }
        }

        // ==================== 粘贴：多行多列内容按行列铺开 ====================

        private void OnCellPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (_viewModel == null) return;

            // 从 Excel 等复制的多行多列内容按行列铺开到表格，而不是塞进同一个单元格
            EditorColumnGridPasteHelper.HandlePasting(
                e.OriginalSource as TextBox, InputsEditor, OutputsEditor, e, _viewModel.PasteGrid);
        }
    }
}
