using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Services.Epma.Input
{
    /// <summary>
    /// 输入表的不可变快照：固定列 ID 与各行原始单元格，数组下标 r 对应工作表第 r+1 行。
    /// 每行宽度等于表头数；空白单元格显式保存为 <see cref="RawCell.Empty"/>。
    /// </summary>
    public sealed record InputTableSnapshot
    {
        public InputTableSnapshot(
            IReadOnlyList<string> headers,
            IReadOnlyList<IReadOnlyList<RawCell>> rows)
        {
            ArgumentNullException.ThrowIfNull(headers);
            ArgumentNullException.ThrowIfNull(rows);

            Headers = Array.AsReadOnly(headers.ToArray());
            if (Headers.Any(h => h is null))
                throw new ArgumentException("Headers must not contain null.", nameof(headers));
            Rows = Array.AsReadOnly(rows.Select(row =>
            {
                var cells = (row ?? throw new ArgumentException("Row must not be null.", nameof(rows))).ToArray();
                if (cells.Length != Headers.Count || cells.Any(c => c is null))
                    throw new ArgumentException("Rows must match the header width and contain non-null cells.", nameof(rows));
                return (IReadOnlyList<RawCell>)Array.AsReadOnly(cells);
            }).ToArray());
        }

        public IReadOnlyList<string> Headers { get; }

        public IReadOnlyList<IReadOnlyList<RawCell>> Rows { get; }
    }
}
