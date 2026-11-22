using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>粘贴的目标表格：输入列表（列名/单位/分组/示例…）或输出列表（列名/分组/示例公式）。</summary>
    public enum EditorGridTable
    {
        /// <summary>行 0 = 列名，行 1 = 单位，行 2 = 分组，行 3 起 = 示例值。</summary>
        Inputs,
        /// <summary>行 0 = 列名，行 1 = 分组，行 2 = 示例（公式）。</summary>
        Outputs
    }

    /// <summary>
    /// 地质温压计 / EPMA 新建模板窗口“数据”表格的粘贴支持：把从 Excel 等复制的
    /// 制表符/换行分隔内容，按行列铺开到表格中（而不是全部塞进一个单元格）。
    /// 解析与写入逻辑与界面无关，界面部分只负责定位被粘贴的单元格。
    /// </summary>
    public static class EditorColumnGridPasteHelper
    {
        // ==================== 解析 ====================

        /// <summary>
        /// 解析剪贴板文本（Excel 复制格式：列以制表符分隔、行以换行分隔，
        /// 含制表符/换行/引号的单元格用双引号包裹）。单元格内的换行会被替换成空格，首尾空白被去除。
        /// </summary>
        public static string[][] ParseGrid(string? text)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(text)) return Array.Empty<string[]>();

            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;

            void EndCell()
            {
                row.Add(cell.ToString().Trim());
                cell.Clear();
            }

            void EndRow()
            {
                EndCell();
                rows.Add(row.ToArray());
                row.Clear();
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                        cell.Append(' ');
                    }
                    else cell.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"' when cell.Length == 0:
                        inQuotes = true;
                        break;
                    case '\t':
                        EndCell();
                        break;
                    case '\r':
                    case '\n':
                        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                        EndRow();
                        break;
                    default:
                        cell.Append(c);
                        break;
                }
            }

            // 末尾没有换行时补上最后一行；以换行结尾（Excel 的习惯）则不产生多余的空行
            if (cell.Length > 0 || row.Count > 0) EndRow();

            return rows.ToArray();
        }

        // ==================== 写入 ====================

        /// <summary>
        /// 把 <paramref name="cells"/> 以 (<paramref name="row"/>, <paramref name="column"/>) 为左上角写入表格；
        /// 列数 / 示例行数不够时通过回调自动补足（示例行数受上限限制，超出部分忽略）。
        /// </summary>
        public static void Apply(
            EditorGridTable table, int row, int column, string[][] cells,
            ObservableCollection<EpmaEditorColumn> inputs,
            ObservableCollection<EpmaEditorColumn> outputs,
            ObservableCollection<int> exampleRows,
            int maximumExampleRows,
            Action addInput, Action addOutput, Action addExample)
        {
            if (cells.Length == 0 || row < 0 || column < 0) return;

            var columns = table == EditorGridTable.Inputs ? inputs : outputs;
            var addColumn = table == EditorGridTable.Inputs ? addInput : addOutput;

            int width = cells.Max(line => line.Length);
            while (columns.Count < column + width)
            {
                int before = columns.Count;
                addColumn();
                if (columns.Count == before) break;
            }

            if (table == EditorGridTable.Inputs)
            {
                int requiredExamples = Math.Min(row + cells.Length - 3, maximumExampleRows);
                while (exampleRows.Count < requiredExamples)
                {
                    int before = exampleRows.Count;
                    addExample();
                    if (exampleRows.Count == before) break;
                }
            }

            for (int r = 0; r < cells.Length; r++)
            {
                for (int c = 0; c < cells[r].Length; c++)
                {
                    int targetColumn = column + c;
                    if (targetColumn >= columns.Count) continue;
                    var target = columns[targetColumn];
                    string value = cells[r][c];
                    int targetRow = row + r;

                    if (table == EditorGridTable.Inputs)
                    {
                        switch (targetRow)
                        {
                            case 0: SetId(target, value); break;
                            case 1: target.Unit = value; break;
                            case 2: target.Group = value; break;
                            default:
                                int example = targetRow - 3;
                                if (example < target.Examples.Count) target.Examples[example].Value = value;
                                break;
                        }
                    }
                    else
                    {
                        switch (targetRow)
                        {
                            case 0: SetId(target, value); break;
                            case 1: target.Group = value; break;
                            case 2: target.ExampleValue = value; break;
                        }
                    }
                }
            }
        }

        // 必填的固定列（Sample）不能改名；列名不允许被粘贴成空
        private static void SetId(EpmaEditorColumn column, string value)
        {
            if (column.IsFixed || value.Length == 0) return;
            column.Id = value;
        }

        // ==================== 界面：处理单元格上的粘贴 ====================

        /// <summary>
        /// 处理表格单元格上的粘贴事件：单个值按普通粘贴（去掉换行）；多行/多列内容取消默认粘贴，
        /// 按被粘贴单元格所在的行列铺开并通过 <paramref name="apply"/> 写入视图模型。
        /// </summary>
        public static void HandlePasting(
            TextBox? cell, ItemsControl inputsTable, ItemsControl outputsTable,
            DataObjectPastingEventArgs e,
            Action<EditorGridTable, int, int, string[][]> apply)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText)
                || e.DataObject.GetData(DataFormats.UnicodeText) is not string text)
                return;

            var grid = ParseGrid(text);
            if (grid.Length == 0) return;

            if (grid.Length == 1 && grid[0].Length == 1)
            {
                // 单个值：与原有行为一致，不带换行地粘贴到当前单元格
                if (grid[0][0] != text)
                    e.DataObject = new DataObject(DataFormats.UnicodeText, grid[0][0]);
                return;
            }

            if (cell == null || !TryLocate(cell, inputsTable, outputsTable, out var table, out int row, out int column))
            {
                e.DataObject = new DataObject(DataFormats.UnicodeText, string.Join(" ", grid.SelectMany(line => line)));
                return;
            }

            e.CancelCommand();
            apply(table, row, column, grid);
        }

        private static bool TryLocate(
            TextBox cell, ItemsControl inputsTable, ItemsControl outputsTable,
            out EditorGridTable table, out int row, out int column)
        {
            table = EditorGridTable.Inputs; row = column = -1;

            ItemsControl owner;
            if (inputsTable.IsAncestorOf(cell)) owner = inputsTable;
            else if (outputsTable.IsAncestorOf(cell)) { owner = outputsTable; table = EditorGridTable.Outputs; }
            else return false;

            if (ItemsControl.ContainerFromElement(owner, cell) is not UIElement container) return false;
            column = owner.ItemContainerGenerator.IndexFromContainer(container);
            if (column < 0) return false;

            // 同一列内的单元格自上而下排列：该单元格是第几个即是第几行
            var cellsOfColumn = EnumerateTextBoxes(container)
                .Where(candidate => candidate.Visibility == Visibility.Visible)
                .OrderBy(candidate => candidate.TranslatePoint(new Point(), container).Y)
                .ToList();
            row = cellsOfColumn.IndexOf(cell);
            return row >= 0;
        }

        private static IEnumerable<TextBox> EnumerateTextBoxes(DependencyObject root)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is TextBox textBox) yield return textBox;
                else foreach (var nested in EnumerateTextBoxes(child)) yield return nested;
            }
        }
    }
}
