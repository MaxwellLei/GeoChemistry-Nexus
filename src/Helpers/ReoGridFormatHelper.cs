using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using unvell.ReoGrid;
using unvell.ReoGrid.Graphics;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// ReoGrid 格式化操作封装。
    /// </summary>
    public static class ReoGridFormatHelper
    {
        public static readonly float[] CommonFontSizes =
        {
            8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 36, 48, 72
        };

        public static RangePosition GetSelectionRange(Worksheet worksheet)
        {
            if (worksheet == null)
                return RangePosition.Empty;

            var range = worksheet.SelectionRange;
            if (range.IsEmpty)
                return new RangePosition(0, 0, 1, 1);

            return range;
        }

        public static bool TryGetPrimaryCell(Worksheet worksheet, out int row, out int col)
        {
            row = 0;
            col = 0;

            if (worksheet == null)
                return false;

            var range = GetSelectionRange(worksheet);
            row = range.Row;
            col = range.Col;

            if (!worksheet.IsValidCell(row, col))
            {
                for (int r = range.Row; r <= range.EndRow; r++)
                {
                    for (int c = range.Col; c <= range.EndCol; c++)
                    {
                        if (worksheet.IsValidCell(r, c))
                        {
                            row = r;
                            col = c;
                            return true;
                        }
                    }
                }
            }

            return row >= 0 && col >= 0;
        }

        public static CellStyleSnapshot ReadCellStyle(Worksheet worksheet, int row, int col)
        {
            var snapshot = new CellStyleSnapshot();
            if (worksheet == null)
                return snapshot;

            var cell = worksheet.CreateAndGetCell(row, col);
            var style = cell.Style;

            snapshot.Bold = style.Bold;
            snapshot.Italic = style.Italic;
            snapshot.Underline = style.Underline;
            snapshot.FontName = string.IsNullOrWhiteSpace(style.FontName) ? "Segoe UI" : style.FontName;
            snapshot.FontSize = style.FontSize <= 0 ? 10f : style.FontSize;
            snapshot.HAlign = style.HAlign;
            snapshot.VAlign = style.VAlign;
            snapshot.TextColor = ToMediaColor(style.TextColor);
            snapshot.BackColor = ToMediaColor(style.BackColor);
            return snapshot;
        }

        public static void ApplyFontName(Worksheet worksheet, RangePosition range, string fontName)
        {
            if (worksheet == null || string.IsNullOrWhiteSpace(fontName))
                return;

            worksheet.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.FontName,
                FontName = fontName
            });
        }

        public static void ApplyFontSize(Worksheet worksheet, RangePosition range, float fontSize)
        {
            if (worksheet == null || fontSize <= 0)
                return;

            worksheet.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.FontSize,
                FontSize = fontSize
            });
        }

        public static void ApplyBold(Worksheet worksheet, RangePosition range, bool bold)
        {
            worksheet?.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.FontStyleBold,
                Bold = bold
            });
        }

        public static void ApplyItalic(Worksheet worksheet, RangePosition range, bool italic)
        {
            worksheet?.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.FontStyleItalic,
                Italic = italic
            });
        }

        public static void ApplyUnderline(Worksheet worksheet, RangePosition range, bool underline)
        {
            worksheet?.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.FontStyleUnderline,
                Underline = underline
            });
        }

        public static void ApplyTextColor(Worksheet worksheet, RangePosition range, Color color)
        {
            worksheet?.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.TextColor,
                TextColor = ToWorksheetColor(color)
            });
        }

        public static void ApplyBackColor(Worksheet worksheet, RangePosition range, Color color)
        {
            worksheet?.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.BackColor,
                BackColor = ToWorksheetColor(color)
            });
        }

        public static void ApplyHorizontalAlign(Worksheet worksheet, RangePosition range, ReoGridHorAlign align)
        {
            worksheet?.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.HorizontalAlign,
                HAlign = align
            });
        }

        public static void ApplyVerticalAlign(Worksheet worksheet, RangePosition range, ReoGridVerAlign align)
        {
            worksheet?.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.VerticalAlign,
                VAlign = align
            });
        }

        public static void MergeOrUnmerge(Worksheet worksheet, RangePosition range)
        {
            if (worksheet == null || range.IsEmpty)
                return;

            if (worksheet.IsMergedCell(range.Row, range.Col))
                worksheet.UnmergeRange(range);
            else
                worksheet.MergeRange(range);
        }

        public static string ColumnIndexToName(int columnIndex)
        {
            if (columnIndex < 0)
                return "?";

            var columnName = string.Empty;
            int currentIndex = columnIndex;

            do
            {
                columnName = (char)('A' + (currentIndex % 26)) + columnName;
                currentIndex = (currentIndex / 26) - 1;
            }
            while (currentIndex >= 0);

            return columnName;
        }

        public static string FormatRangeAddress(RangePosition range)
        {
            if (range.Rows <= 1 && range.Cols <= 1)
                return $"{ColumnIndexToName(range.Col)}{range.Row + 1}";

            return $"{ColumnIndexToName(range.Col)}{range.Row + 1}:{ColumnIndexToName(range.EndCol)}{range.EndRow + 1}";
        }

        public static string GetCellExportText(Worksheet worksheet, int row, int col, FreeSheetCsvExportMode mode)
        {
            if (worksheet == null)
                return string.Empty;

            if (mode == FreeSheetCsvExportMode.Formulas)
            {
                var cell = worksheet.GetCell(row, col);
                if (cell != null && cell.HasFormula && !string.IsNullOrEmpty(cell.Formula))
                    return EnsureFormulaPrefix(cell.Formula);

                if (cell?.Data is string dataText && dataText.StartsWith("=", StringComparison.Ordinal))
                    return dataText;

                return cell?.Data?.ToString() ?? string.Empty;
            }

            return worksheet.GetCellText(row, col) ?? string.Empty;
        }

        private static string EnsureFormulaPrefix(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return string.Empty;

            return formula.StartsWith("=", StringComparison.Ordinal) ? formula : $"={formula}";
        }

        public static string EscapeCsvCell(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }

        public static string BuildCsv(Worksheet worksheet, FreeSheetCsvExportMode mode)
        {
            if (worksheet == null)
                return string.Empty;

            var range = worksheet.UsedRange;
            var csvBuilder = new StringBuilder();

            for (int r = range.Row; r <= range.EndRow; r++)
            {
                var rowValues = new List<string>();
                for (int c = range.Col; c <= range.EndCol; c++)
                    rowValues.Add(EscapeCsvCell(GetCellExportText(worksheet, r, c, mode)));

                csvBuilder.AppendLine(string.Join(",", rowValues));
            }

            return csvBuilder.ToString();
        }

        private static Color ToMediaColor(SolidColor color)
        {
            return Color.FromArgb(255, color.R, color.G, color.B);
        }

        private static Color ToWorksheetColor(Color color)
        {
            return Color.FromArgb(255, color.R, color.G, color.B);
        }

        public static void ApplyCellStyle(Worksheet worksheet, RangePosition range, CellStyleSnapshot snapshot)
        {
            if (worksheet == null || snapshot == null)
                return;

            worksheet.SetRangeStyles(range, new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.FontName | PlainStyleFlag.FontSize |
                       PlainStyleFlag.FontStyleBold | PlainStyleFlag.FontStyleItalic |
                       PlainStyleFlag.FontStyleUnderline | PlainStyleFlag.TextColor |
                       PlainStyleFlag.BackColor | PlainStyleFlag.HorizontalAlign |
                       PlainStyleFlag.VerticalAlign,
                FontName = snapshot.FontName,
                FontSize = snapshot.FontSize,
                Bold = snapshot.Bold,
                Italic = snapshot.Italic,
                Underline = snapshot.Underline,
                TextColor = ToWorksheetColor(snapshot.TextColor),
                BackColor = ToWorksheetColor(snapshot.BackColor),
                HAlign = snapshot.HAlign,
                VAlign = snapshot.VAlign
            });
        }
    }

    public sealed class CellStyleSnapshot
    {
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public string FontName { get; set; } = "Segoe UI";
        public float FontSize { get; set; } = 10f;
        public ReoGridHorAlign HAlign { get; set; } = ReoGridHorAlign.General;
        public ReoGridVerAlign VAlign { get; set; } = ReoGridVerAlign.Middle;
        public Color TextColor { get; set; } = Colors.Black;
        public Color BackColor { get; set; } = Colors.White;
    }
}
