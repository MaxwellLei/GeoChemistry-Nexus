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
