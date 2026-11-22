using System;
using System.Collections.Generic;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>投图时某一行没有被画到图上的原因（对应行头的红点）。</summary>
    public enum DiagramRowSkipReason
    {
        /// <summary>脚本结果不是合法坐标（除以 0、取对数失败、返回值不是有限数等）。</summary>
        InvalidCoordinate,
        /// <summary>Category 为空，无法分组，整行被跳过。</summary>
        MissingCategory,
        /// <summary>蛛网图 / Harker 多联图：该行没有可用的数值，整行被跳过。</summary>
        NoValidData,
    }

    /// <summary>
    /// 图解数据表的行头状态点提供者（标准图解、蛛网图、Harker 多联图共用同一张表）。
    /// <list type="bullet">
    /// <item>Ok：该行已投图。</item>
    /// <item>Error：该行有内容但没能投图（原因见 <see cref="GetSkipReason"/>）。</item>
    /// <item>Stale：该行投图之后又被修改，或插入 / 删除 / 排序行后行号已经不可靠。</item>
    /// <item>NotCalculated：该行有内容，但还没有投过图。</item>
    /// </list>
    /// 用法：<c>ReoGridTableAppearance.SetRowStatusProvider(grid, provider)</c> 注册一次；投图时先 <see cref="Reset"/>，
    /// 再逐行 <see cref="MarkPlotted"/> / <see cref="MarkSkipped"/>，最后 <see cref="NotifyChanged"/>。
    /// </summary>
    public sealed class DiagramRowStatusProvider : IWorksheetRowStatusProvider
    {
        private readonly HashSet<int> _plotted = new();
        private readonly Dictionary<int, DiagramRowSkipReason> _skipped = new();
        private readonly HashSet<int> _edited = new();
        private Worksheet? _sheet;
        private int _maxTrackedRow = -1;
        private bool _allStale;

        public event EventHandler? RowStatusChanged;

        /// <summary>绑定数据表：监听插入 / 删除 / 排序行，行号偏移后旧的逐行状态不再可信。</summary>
        public void Attach(Worksheet sheet)
        {
            if (_sheet != null)
            {
                _sheet.RowsInserted -= OnRowsInserted;
                _sheet.RowsDeleted -= OnRowsDeleted;
                _sheet.RowsSorted -= OnRowsSorted;
            }
            _sheet = sheet;
            sheet.RowsInserted += OnRowsInserted;
            sheet.RowsDeleted += OnRowsDeleted;
            sheet.RowsSorted += OnRowsSorted;
        }

        /// <summary>清空所有逐行状态（表格重建、清空数据或开始新一次投图时调用）。</summary>
        public void Reset()
        {
            _plotted.Clear();
            _skipped.Clear();
            _edited.Clear();
            _maxTrackedRow = -1;
            _allStale = false;
            NotifyChanged();
        }

        /// <summary>登记一行已投图（不触发重绘，批量登记完请调用 <see cref="NotifyChanged"/>）。</summary>
        public void MarkPlotted(int row)
        {
            if (row < 0) return;
            _plotted.Add(row);
            _maxTrackedRow = Math.Max(_maxTrackedRow, row);
        }

        /// <summary>登记一批已投图的行。</summary>
        public void MarkPlotted(IEnumerable<int> rows)
        {
            foreach (var row in rows) MarkPlotted(row);
        }

        /// <summary>登记一行被跳过（不触发重绘）。</summary>
        public void MarkSkipped(int row, DiagramRowSkipReason reason)
        {
            if (row < 0) return;
            _skipped[row] = reason;
            _maxTrackedRow = Math.Max(_maxTrackedRow, row);
        }

        /// <summary>该行有内容时登记为被跳过（用于蛛网图 / Harker 里“没有可用数值”的行）。</summary>
        public void MarkSkippedIfHasContent(int row, DiagramRowSkipReason reason)
        {
            if (HasContent(row)) MarkSkipped(row, reason);
        }

        /// <summary>批量登记完成后通知表格重绘行头。</summary>
        public void NotifyChanged() => RowStatusChanged?.Invoke(this, EventArgs.Empty);

        /// <summary>用户修改了单元格：已投图 / 已跳过的行变为“已过期”，新录入数据的行也需要刷新。</summary>
        public void MarkEdited(int firstRow, int rowCount = 1)
        {
            if (rowCount > 0 && (_plotted.Count > 0 || _skipped.Count > 0))
            {
                int lastRow = firstRow + rowCount - 1;
                if (rowCount == 1)
                {
                    if (_plotted.Contains(firstRow) || _skipped.ContainsKey(firstRow)) _edited.Add(firstRow);
                }
                else
                {
                    // 只遍历已登记的行，避免整列 / 整表选区时逐行循环。
                    foreach (var row in _plotted)
                        if (row >= firstRow && row <= lastRow) _edited.Add(row);
                    foreach (var row in _skipped.Keys)
                        if (row >= firstRow && row <= lastRow) _edited.Add(row);
                }
            }
            NotifyChanged();
        }

        /// <summary>返回该行被跳过的原因；该行不是“已跳过”、或跳过之后又被修改过（结果已过期）则返回 null。</summary>
        public DiagramRowSkipReason? GetSkipReason(int row)
            => _skipped.TryGetValue(row, out var reason) && !_edited.Contains(row) ? reason : null;

        /// <summary>该行是否有任何非空内容。</summary>
        public bool HasContent(int row)
        {
            var sheet = _sheet;
            if (sheet == null || row < 0 || row >= sheet.RowCount) return false;
            for (int col = 0; col < sheet.ColumnCount; col++)
            {
                var value = sheet.GetCellData(row, col);
                if (value == null) continue;
                if (value is string text)
                {
                    if (!string.IsNullOrWhiteSpace(text)) return true;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public WorksheetRowStatus GetRowStatus(int row)
        {
            var sheet = _sheet;
            if (sheet == null || row < 0 || row >= sheet.RowCount) return WorksheetRowStatus.None;

            if (_plotted.Contains(row))
                return _edited.Contains(row) ? WorksheetRowStatus.Stale : WorksheetRowStatus.Ok;

            if (!HasContent(row)) return WorksheetRowStatus.None;

            if (_skipped.ContainsKey(row))
                return _edited.Contains(row) ? WorksheetRowStatus.Stale : WorksheetRowStatus.Error;

            return _allStale ? WorksheetRowStatus.Stale : WorksheetRowStatus.NotCalculated;
        }

        // ---------- 行号偏移 ----------

        private void OnRowsInserted(object? sender, RowsInsertedEventArgs e)
        {
            // 在最后一个已登记行之后追加（例如粘贴时自动扩行），不会让已登记的行号偏移。
            if (e.Row > _maxTrackedRow) return;
            InvalidateAll();
        }

        private void OnRowsDeleted(object? sender, RowsDeletedEventArgs e)
        {
            if (e.Row > _maxTrackedRow) return;
            InvalidateAll();
        }

        private void OnRowsSorted(object? sender, RangeEventArgs e) => InvalidateAll();

        /// <summary>行号偏移后逐行状态已不可靠：丢弃它们，让有内容的行统一显示为“已过期”，等待下一次投图。</summary>
        private void InvalidateAll()
        {
            if (_plotted.Count == 0 && _skipped.Count == 0) return;
            _plotted.Clear();
            _skipped.Clear();
            _edited.Clear();
            _maxTrackedRow = -1;
            _allStale = true;
            NotifyChanged();
        }
    }
}
