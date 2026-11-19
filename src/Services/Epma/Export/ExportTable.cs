using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Services.Epma.Export
{
    /// <summary>
    /// 类型化导出单元格，按类型写为 CSV / XLSX。
    /// </summary>
    public abstract record ExportCell
    {
        private ExportCell()
        {
        }

        /// <summary>数值单元格；CSV 写出时使用 "R" + InvariantCulture。</summary>
        public sealed record Number(double Value) : ExportCell;

        public sealed record Text(string Value) : ExportCell
        {
            public string Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));
        }

        /// <summary>空单元格（未定义值、非有限值）。</summary>
        public sealed record Empty : ExportCell;
    }

    /// <summary>
    /// 与语言无关的导出表：单表头行 + 数据行，每行宽度等于表头数。
    /// </summary>
    public sealed record ExportTable
    {
        public ExportTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<ExportCell>> rows)
        {
            ArgumentNullException.ThrowIfNull(headers);
            ArgumentNullException.ThrowIfNull(rows);

            var headerCopy = headers.ToArray();
            if (headerCopy.Any(h => h is null))
            {
                throw new ArgumentException("Headers must not contain null.", nameof(headers));
            }

            var rowCopy = new IReadOnlyList<ExportCell>[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i] ?? throw new ArgumentException($"Row {i} is null.", nameof(rows));
                var cells = row.ToArray();
                if (cells.Length != headerCopy.Length || cells.Any(c => c is null))
                {
                    throw new ArgumentException($"Row {i} must have exactly {headerCopy.Length} non-null cells.", nameof(rows));
                }

                rowCopy[i] = Array.AsReadOnly(cells);
            }

            Headers = Array.AsReadOnly(headerCopy);
            Rows = Array.AsReadOnly(rowCopy);
        }

        public IReadOnlyList<string> Headers { get; }

        public IReadOnlyList<IReadOnlyList<ExportCell>> Rows { get; }
    }
}
