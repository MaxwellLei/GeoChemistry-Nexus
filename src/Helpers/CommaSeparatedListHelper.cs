using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 逗号分隔列表的解析与格式化，支持含逗号的 ReoGrid 公式（如 =MyFormula(A2,B2,C2)）。
    /// </summary>
    public static class CommaSeparatedListHelper
    {
        public static string Join(IEnumerable<string>? items)
        {
            if (items == null)
                return string.Empty;

            return string.Join(", ", items
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim()));
        }

        public static List<string> Split(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            text = text.Trim();
            if (text.IndexOf('"') >= 0)
                return SplitQuotedCsv(text);

            return SplitRespectingParentheses(text);
        }

        /// <summary>
        /// 修复因历史逗号拆分而断裂的列表（先合并为文本，再按括号规则重新拆分）。
        /// </summary>
        public static List<string> RepairList(IReadOnlyList<string>? items)
        {
            if (items == null || items.Count == 0)
                return new List<string>();

            return Split(Join(items));
        }

        /// <summary>
        /// 将示例行与表头列数对齐；合并因错误拆分而散落在末尾的公式片段。
        /// </summary>
        public static List<string> AlignToHeaderCount(IReadOnlyList<string>? headers, IReadOnlyList<string>? exampleRow)
        {
            var repaired = RepairList(exampleRow);
            if (headers == null || headers.Count == 0)
                return repaired;

            if (repaired.Count <= headers.Count)
                return repaired;

            var aligned = repaired.Take(headers.Count - 1)
                .Select(s => s.Trim())
                .ToList();
            aligned.Add(string.Join(", ", repaired.Skip(headers.Count - 1).Select(s => s.Trim())));
            return aligned;
        }

        /// <summary>
        /// 按逗号拆分，但不拆分括号内的逗号（用于公式参数列表）。
        /// </summary>
        private static List<string> SplitRespectingParentheses(string text)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            int depth = 0;

            foreach (char c in text)
            {
                if (c == '(')
                {
                    depth++;
                    current.Append(c);
                }
                else if (c == ')')
                {
                    depth = Math.Max(0, depth - 1);
                    current.Append(c);
                }
                else if (c == ',' && depth == 0)
                {
                    AppendIfNotEmpty(result, current);
                }
                else
                {
                    current.Append(c);
                }
            }

            AppendIfNotEmpty(result, current);
            return result;
        }

        /// <summary>
        /// 标准 CSV 引号字段解析（字段含逗号时可使用双引号包裹）。
        /// </summary>
        private static List<string> SplitQuotedCsv(string text)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    AppendIfNotEmpty(result, current);
                }
                else
                {
                    current.Append(c);
                }
            }

            AppendIfNotEmpty(result, current);
            return result;
        }

        private static void AppendIfNotEmpty(List<string> result, StringBuilder current)
        {
            var item = current.ToString().Trim();
            if (item.Length > 0)
                result.Add(item);
            current.Clear();
        }
    }
}
