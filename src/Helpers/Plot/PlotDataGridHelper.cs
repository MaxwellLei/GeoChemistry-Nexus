using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解模块数据表格辅助：区分分组/metadata 列与参与脚本计算的数值列。
    /// </summary>
    public static class PlotDataGridHelper
    {
        private static readonly HashSet<string> MetadataColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "Category",
            "Sample",
            "SampleID",
            "ID",
            "No",
            "Name",
            "Label",
            "Group",
            "RockType",
            "Comment",
            "Comments",
            "Note",
            "Notes"
        };

        public static bool IsMetadataColumn(string? columnName) =>
            !string.IsNullOrWhiteSpace(columnName) && MetadataColumns.Contains(columnName.Trim());

        public static List<string> ParseScriptDataColumns(string? requiredDataSeries)
        {
            if (string.IsNullOrWhiteSpace(requiredDataSeries))
            {
                return new List<string>();
            }

            return requiredDataSeries
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0 && !IsMetadataColumn(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 解析各数据列的单位，结果与 <see cref="ParseScriptDataColumns"/> 返回的列一一对应（同样的过滤与去重）。
        /// <paramref name="dataSeriesUnits"/> 按位置对应 <paramref name="requiredDataSeries"/> 的原始条目；没有单位的列为 null。
        /// </summary>
        public static List<string?> ParseScriptDataUnits(string? requiredDataSeries, string? dataSeriesUnits)
        {
            var result = new List<string?>();
            if (string.IsNullOrWhiteSpace(requiredDataSeries)) return result;

            var units = (dataSeriesUnits ?? string.Empty).Split(',');
            var tokens = requiredDataSeries.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tokens.Length; i++)
            {
                string name = tokens[i].Trim();
                if (name.Length == 0 || IsMetadataColumn(name) || !seen.Add(name)) continue;
                string unit = i < units.Length ? units[i].Trim() : string.Empty;
                result.Add(unit.Length == 0 ? null : unit);
            }
            return result;
        }

        public static string GetRowColumnText(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
            {
                return string.Empty;
            }

            object? value = row[columnName];
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            return value.ToString()?.Trim() ?? string.Empty;
        }
    }
}
