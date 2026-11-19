using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 只提供“单位”的列头附加信息：不需要分组行的表格（图解数据表、地质温压计、EPMA）直接用它，
    /// 模板里某一列的单位非空时，该列的列名下方就会显示单位，为空则只显示列名。
    /// 用法：<c>ReoGridTableAppearance.SetColumnHeaderProvider(grid, provider)</c> 注册一次，之后随模板变化调用 <see cref="SetUnits"/>。
    /// </summary>
    public sealed class WorksheetColumnUnitProvider : IWorksheetColumnHeaderProvider
    {
        private readonly int _firstColumn;
        private IReadOnlyList<string?> _units = Array.Empty<string?>();

        /// <param name="firstColumn">单位列表中第一项对应的工作表列号（图解数据表第 0 列是 Category，之后才是数据列，传 1）。</param>
        public WorksheetColumnUnitProvider(int firstColumn = 0)
        {
            _firstColumn = firstColumn;
        }

        public event EventHandler? ColumnHeaderInfoChanged;

        /// <summary>设置单位列表（按列顺序，没有单位用 null 或空串）；传 null 表示全部没有单位。会通知表格重新计算列头。</summary>
        public void SetUnits(IEnumerable<string?>? units)
        {
            _units = units?.Select(u => string.IsNullOrWhiteSpace(u) ? null : u.Trim()).ToList() ?? new List<string?>();
            ColumnHeaderInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        public string? GetGroup(int column) => null;

        public string? GetUnit(int column)
        {
            int index = column - _firstColumn;
            return index >= 0 && index < _units.Count ? _units[index] : null;
        }
    }
}
