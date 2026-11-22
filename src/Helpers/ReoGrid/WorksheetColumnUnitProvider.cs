using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 提供“单位”（及可选分组）的列头附加信息：图解数据表、地质温压计、EPMA 直接用它，
    /// 模板里某一列的单位非空时，该列的列名下方就会显示单位，为空则只显示列名。
    /// 另外可以让最前面的若干列（样品列 / 分组列）按界面语言显示翻译后的列名，见 <see cref="BuiltInColumnHeaders"/>。
    /// 用法：<c>ReoGridTableAppearance.SetColumnHeaderProvider(grid, provider)</c> 注册一次，之后随模板变化调用 <see cref="SetUnits"/>。
    /// </summary>
    public sealed class WorksheetColumnUnitProvider : IWorksheetColumnHeaderProvider, IWorksheetColumnHeaderLabelProvider
    {
        private readonly int _firstColumn;
        private readonly int _localizedLeadingColumns;
        private IReadOnlyList<string?> _units = Array.Empty<string?>();
        private IReadOnlyList<string?> _groups = Array.Empty<string?>();

        /// <param name="firstColumn">单位列表中第一项对应的工作表列号（图解数据表第 0 列是 Category、第 1 列是 Sample，之后才是数据列，传 2）。</param>
        /// <param name="localizedLeadingColumns">
        /// 最前面有几列的列名需要按界面语言显示翻译（0 表示都不翻译）。只有这些列里名为 Sample / Category 的列头会被翻译，
        /// 其后的列（如结果列恰好叫 Category）不受影响；列头 <c>Text</c> 不变。
        /// </param>
        public WorksheetColumnUnitProvider(int firstColumn = 0, int localizedLeadingColumns = 0)
        {
            _firstColumn = firstColumn;
            _localizedLeadingColumns = Math.Max(0, localizedLeadingColumns);

            // 弱订阅界面语言变化：切换语言后重新绘制列头；不会让单例的语言服务持有本对象。
            if (_localizedLeadingColumns > 0)
                PropertyChangedEventManager.AddHandler(LanguageService.Instance, OnAppLanguageChanged, string.Empty);
        }

        public event EventHandler? ColumnHeaderInfoChanged;

        private void OnAppLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
                ColumnHeaderInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>设置单位列表（按列顺序，没有单位用 null 或空串）；传 null 表示全部没有单位。会通知表格重新计算列头。</summary>
        public void SetUnits(IEnumerable<string?>? units)
        {
            _units = units?.Select(u => string.IsNullOrWhiteSpace(u) ? null : u.Trim()).ToList() ?? new List<string?>();
            _groups = Array.Empty<string?>();
            ColumnHeaderInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>同时设置单位和分组标题（按列顺序，空串或 null 表示没有）；相邻且标题相同的列由表格合并成一个分组条。只通知一次。</summary>
        public void SetHeaders(IEnumerable<string?>? units, IEnumerable<string?>? groups)
        {
            _units = units?.Select(u => string.IsNullOrWhiteSpace(u) ? null : u.Trim()).ToList() ?? new List<string?>();
            _groups = groups?.Select(g => string.IsNullOrWhiteSpace(g) ? null : g.Trim()).ToList() ?? new List<string?>();
            ColumnHeaderInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        public string? GetGroup(int column)
        {
            int index = column - _firstColumn;
            return index >= 0 && index < _groups.Count ? _groups[index] : null;
        }

        public string? GetUnit(int column)
        {
            int index = column - _firstColumn;
            return index >= 0 && index < _units.Count ? _units[index] : null;
        }

        public string? GetLabel(int column, string? headerText)
        {
            if (column < 0 || column >= _localizedLeadingColumns) return null;

            string display = BuiltInColumnHeaders.GetDisplayText(headerText);
            return string.Equals(display, headerText, StringComparison.Ordinal) ? null : display;
        }
    }
}
