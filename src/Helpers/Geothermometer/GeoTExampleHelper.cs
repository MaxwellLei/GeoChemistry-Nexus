using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 温压计多行示例数据的整理与展开。
    /// <para>
    /// 模板里第 1 条示例仍保存在 <c>ExampleRow</c>（输入值 + 输出列公式），旧版软件只认这一行；
    /// 第 2 条起保存在可选的 <c>ExtraExampleRows</c>：每行与表头等长，只填输入列的值，输出列留空，
    /// 输出列的公式由第 1 条示例的公式按行号推算。没有更多示例时不写入该字段，旧模板内容与哈希保持不变。
    /// </para>
    /// </summary>
    public static class GeoTExampleHelper
    {
        /// <summary>示例总行数上限（含 ExampleRow 的第 1 条）。</summary>
        public const int MaxExampleRows = 30;

        /// <summary>
        /// 模板里示例公式书写时假定的数据行号：表头占第 1 行，第 1 条示例位于第 2 行（如 =MyFormula(A2,B2)）。
        /// </summary>
        private const int AuthoredFormulaRow = 2;

        // 单元格引用中的行号 2：前面不能是字母数字（排除 LOG2 这类标识符），后面不能是数字或字母，也不能紧跟 (（排除函数名）。
        private static readonly Regex AuthoredRowReference = new(
            @"(?<![A-Za-z0-9_.])(\$?[A-Z]{1,3}\$?)2(?![0-9A-Za-z_(])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>是否至少有一条附加示例行。</summary>
        public static bool HasAny(IReadOnlyList<List<string>>? rows) => rows != null && rows.Count > 0;

        /// <summary>
        /// 把附加示例行整理为与表头等长（不足补空串、多余截断），去掉末尾全空的行，并限制总行数。
        /// 没有有效内容时返回空列表，使保存、导出和哈希与没有附加示例的模板完全一致。
        /// </summary>
        public static List<List<string>> NormalizeExtraRows(
            IReadOnlyList<string>? headers,
            IEnumerable<IEnumerable<string>?>? rows)
        {
            var result = new List<List<string>>();
            if (headers == null || headers.Count == 0 || rows == null)
                return result;

            foreach (var row in rows.Take(MaxExampleRows - 1))
            {
                var cells = (row ?? Enumerable.Empty<string>()).ToList();
                var aligned = new List<string>(headers.Count);
                for (int i = 0; i < headers.Count; i++)
                    aligned.Add(i < cells.Count ? cells[i]?.Trim() ?? string.Empty : string.Empty);
                result.Add(aligned);
            }

            while (result.Count > 0 && result[^1].All(string.IsNullOrEmpty))
                result.RemoveAt(result.Count - 1);

            return result;
        }

        /// <summary>
        /// 把按“第 2 行”书写的示例公式改写到指定行号（Excel 行号，从 1 开始）。非公式内容原样返回。
        /// </summary>
        public static string ShiftFormulaRow(string? cell, int targetRow)
        {
            if (string.IsNullOrEmpty(cell))
                return cell ?? string.Empty;

            if (!cell.TrimStart().StartsWith('=') || targetRow == AuthoredFormulaRow)
                return cell;

            return AuthoredRowReference.Replace(cell, match => match.Groups[1].Value + targetRow);
        }

        /// <summary>
        /// 展开为工作表数据行（第 1 项对应工作表第 1 个数据行）：
        /// 第 1 行取 ExampleRow；其余行的输入值取自附加示例行，输出列（第 1 行为公式的列）按行号改写公式。
        /// </summary>
        public static List<List<string>> BuildWorksheetRows(
            IReadOnlyList<string> headers,
            IReadOnlyList<string> exampleRow,
            IReadOnlyList<List<string>>? extraRows)
        {
            var firstRow = new List<string>(headers.Count);
            for (int i = 0; i < headers.Count; i++)
                firstRow.Add(i < exampleRow.Count ? exampleRow[i] ?? string.Empty : string.Empty);

            var rows = new List<List<string>> { firstRow };
            if (extraRows == null)
                return rows;

            // 数据行位于工作表第 1 行起（表头是原生列头），因此第 n 行数据对应 Excel 行号 n。
            for (int rowIndex = 0; rowIndex < extraRows.Count; rowIndex++)
            {
                var source = extraRows[rowIndex] ?? new List<string>();
                var row = new List<string>(headers.Count);
                for (int i = 0; i < headers.Count; i++)
                {
                    bool isFormulaColumn = firstRow[i].TrimStart().StartsWith('=');
                    row.Add(isFormulaColumn
                        ? firstRow[i]
                        : i < source.Count ? source[i] ?? string.Empty : string.Empty);
                }
                rows.Add(row);
            }

            // 公式统一改写到各自所在行；输入列的值不是公式，不受影响。
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                for (int i = 0; i < headers.Count; i++)
                    rows[rowIndex][i] = ShiftFormulaRow(rows[rowIndex][i], rowIndex + 1);
            }

            return rows;
        }
    }
}
