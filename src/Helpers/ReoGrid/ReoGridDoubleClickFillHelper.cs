using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using unvell.ReoGrid;
using unvell.ReoGrid.Actions;
using unvell.ReoGrid.Events;
using RGPoint = unvell.ReoGrid.Graphics.Point;
using RGRect = unvell.ReoGrid.Graphics.Rectangle;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 为 ReoGrid 补齐类似 Excel 的「双击选区右下角填充柄 → 自动向下填充」。
    /// ReoGrid 仅内置拖动填充，双击填充柄会进入单元格编辑，需在 Preview 阶段拦截。
    /// </summary>
    public static class ReoGridDoubleClickFillHelper
    {
        private static readonly ConditionalWeakTable<Worksheet, ReoGridControl> WorksheetToGrid = new();

        private static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached(
                "IsAttached",
                typeof(bool),
                typeof(ReoGridDoubleClickFillHelper),
                new PropertyMetadata(false));

        private static readonly DependencyProperty IsOverFillHandleProperty =
            DependencyProperty.RegisterAttached(
                "IsOverFillHandle",
                typeof(bool),
                typeof(ReoGridDoubleClickFillHelper),
                new PropertyMetadata(false));

        /// <summary>
        /// 双击的第一次按下是否落在填充柄上（第二次按下时 ReoGrid 可能已复位光标）。
        /// </summary>
        private static readonly DependencyProperty FillHandleArmedProperty =
            DependencyProperty.RegisterAttached(
                "FillHandleArmed",
                typeof(bool),
                typeof(ReoGridDoubleClickFillHelper),
                new PropertyMetadata(false));

        private static readonly DependencyProperty BoundWorksheetProperty =
            DependencyProperty.RegisterAttached(
                "BoundWorksheet",
                typeof(Worksheet),
                typeof(ReoGridDoubleClickFillHelper),
                new PropertyMetadata(null));

        /// <summary>
        /// 为指定 ReoGrid 启用双击填充柄自动向下填充（幂等）。
        /// </summary>
        public static void Attach(ReoGridControl? grid)
        {
            if (grid == null)
            {
                return;
            }

            if ((bool)grid.GetValue(IsAttachedProperty))
            {
                return;
            }

            grid.SetValue(IsAttachedProperty, true);
            grid.PreviewMouseMove += OnPreviewMouseMove;
            grid.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            grid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
            BindWorksheet(grid, grid.CurrentWorksheet);
        }

        private static void OnCurrentWorksheetChanged(object? sender, EventArgs e)
        {
            if (sender is not ReoGridControl grid)
            {
                return;
            }

            BindWorksheet(grid, grid.CurrentWorksheet);
        }

        private static void BindWorksheet(ReoGridControl grid, Worksheet? worksheet)
        {
            var previous = grid.GetValue(BoundWorksheetProperty) as Worksheet;
            if (ReferenceEquals(previous, worksheet))
            {
                return;
            }

            if (previous != null)
            {
                previous.CellMouseMove -= OnCellMouseMove;
                WorksheetToGrid.Remove(previous);
            }

            grid.SetValue(BoundWorksheetProperty, worksheet);
            grid.SetValue(IsOverFillHandleProperty, false);
            grid.SetValue(FillHandleArmedProperty, false);

            if (worksheet == null)
            {
                return;
            }

            WorksheetToGrid.AddOrUpdate(worksheet, grid);
            worksheet.CellMouseMove += OnCellMouseMove;
        }

        private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ReoGridControl grid)
            {
                return;
            }

            // ReoGrid 在填充柄上会把光标设为 Cross；此时内部不再抛 CellMouseMove
            if (grid.Cursor == Cursors.Cross)
            {
                grid.SetValue(IsOverFillHandleProperty, true);
            }
        }

        private static void OnCellMouseMove(object? sender, CellMouseEventArgs e)
        {
            if (sender is not Worksheet worksheet)
            {
                return;
            }

            if (!WorksheetToGrid.TryGetValue(worksheet, out var grid))
            {
                return;
            }

            bool overHandle = IsFillHandleHit(worksheet, grid, e.AbsolutePosition)
                || IsEndCellCornerHit(worksheet, e);

            grid.SetValue(IsOverFillHandleProperty, overHandle);
            if (!overHandle && grid.Cursor != Cursors.Cross)
            {
                grid.SetValue(FillHandleArmedProperty, false);
            }
        }

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ReoGridControl grid)
            {
                return;
            }

            bool overHandle = (bool)grid.GetValue(IsOverFillHandleProperty)
                || grid.Cursor == Cursors.Cross;

            if (e.ClickCount == 1)
            {
                grid.SetValue(FillHandleArmedProperty, overHandle);
                return;
            }

            if (e.ClickCount != 2)
            {
                return;
            }

            bool armed = (bool)grid.GetValue(FillHandleArmedProperty) || overHandle;
            grid.SetValue(FillHandleArmedProperty, false);

            if (!armed)
            {
                return;
            }

            var worksheet = grid.CurrentWorksheet;
            if (worksheet == null
                || worksheet.IsEditing
                || worksheet.SelectionStyle == WorksheetSelectionStyle.None
                || worksheet.HasSettings(WorksheetSettings.Edit_Readonly)
                || !worksheet.HasSettings(WorksheetSettings.Edit_DragSelectionToFillSerial))
            {
                return;
            }

            if (TryFillDown(grid, worksheet))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 复刻 ReoGrid 内部 SelectDragCornerHitTest：选区右下角填充柄。
        /// </summary>
        private static bool IsFillHandleHit(Worksheet worksheet, ReoGridControl grid, RGPoint location)
        {
            var selection = worksheet.SelectionRange;
            if (selection.IsEmpty)
            {
                return false;
            }

            var selBounds = worksheet.GetRangePhysicsBounds(selection);
            selBounds.Width--;
            selBounds.Height--;

            float borderWidth = grid.ControlStyle.SelectionBorderWidth;
            const float pad = 4f;
            var thumbRect = new RGRect(
                selBounds.Right - borderWidth - pad,
                selBounds.Bottom - borderWidth - pad,
                borderWidth + 2 + pad * 2,
                borderWidth + 2 + pad * 2);

            return thumbRect.Contains(location);
        }

        /// <summary>
        /// 备用命中：选区右下角单元格内靠近右下角的区域。
        /// </summary>
        private static bool IsEndCellCornerHit(Worksheet worksheet, CellMouseEventArgs e)
        {
            var selection = worksheet.SelectionRange;
            if (selection.IsEmpty)
            {
                return false;
            }

            if (e.CellPosition.Row != selection.EndRow || e.CellPosition.Col != selection.EndCol)
            {
                return false;
            }

            float cellWidth = worksheet.GetColumnWidth(selection.EndCol);
            float cellHeight = worksheet.GetRowHeight(selection.EndRow);
            const float cornerSize = 14f;

            return e.RelativePosition.X >= cellWidth - cornerSize
                && e.RelativePosition.Y >= cellHeight - cornerSize;
        }

        private static bool TryFillDown(ReoGridControl grid, Worksheet worksheet)
        {
            var source = worksheet.SelectionRange;
            if (source.IsEmpty || source.Rows <= 0 || source.Cols <= 0)
            {
                return false;
            }

            int endRow = FindExcelFillDownEndRow(worksheet, source);
            if (endRow <= source.EndRow)
            {
                return false;
            }

            var target = new RangePosition(
                source.EndRow + 1,
                source.Col,
                endRow - source.EndRow,
                source.Cols);

            try
            {
                grid.DoAction(worksheet, new AutoFillSerialAction(source, target));
                return true;
            }
            catch
            {
                try
                {
                    worksheet.AutoFillSerial(source, target);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Excel 风格：左右相邻列在选区底行有值时，向下填到该列第一个空行之前。
        /// </summary>
        private static int FindExcelFillDownEndRow(Worksheet worksheet, RangePosition source)
        {
            int endRow = -1;

            if (source.Col > 0)
            {
                endRow = Math.Max(endRow, GetAdjacentColumnFillEnd(worksheet, source, source.Col - 1));
            }

            if (source.EndCol + 1 < worksheet.ColumnCount)
            {
                endRow = Math.Max(endRow, GetAdjacentColumnFillEnd(worksheet, source, source.EndCol + 1));
            }

            return endRow;
        }

        private static int GetAdjacentColumnFillEnd(Worksheet worksheet, RangePosition source, int adjCol)
        {
            if (IsCellEmpty(worksheet, source.EndRow, adjCol))
            {
                return -1;
            }

            int row = source.EndRow + 1;
            while (row < worksheet.RowCount && !IsCellEmpty(worksheet, row, adjCol))
            {
                row++;
            }

            return row - 1;
        }

        private static bool IsCellEmpty(Worksheet worksheet, int row, int col)
        {
            if (row < 0 || col < 0 || row >= worksheet.RowCount || col >= worksheet.ColumnCount)
            {
                return true;
            }

            object? data = worksheet.GetCellData(row, col);
            if (data == null)
            {
                return true;
            }

            if (data is string text)
            {
                return string.IsNullOrWhiteSpace(text);
            }

            string display = worksheet.GetCellText(row, col);
            return string.IsNullOrWhiteSpace(display);
        }
    }
}
