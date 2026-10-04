using System;
using System.Globalization;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 样品与导出数字只认小数点。逗号不是小数点（不变文化下逗号仅作千分位）。
    /// </summary>
    public static class InvariantNumberParser
    {
        private static readonly NumberStyles FloatStyle = NumberStyles.Float;

        public static bool TryParse(string? text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return double.TryParse(text.Trim(), FloatStyle, CultureInfo.InvariantCulture, out value)
                && double.IsFinite(value);
        }

        public static double Parse(string text)
        {
            if (!TryParse(text, out double value))
                throw new FormatException();

            return value;
        }

        public static bool TryParseCell(object? cell, out double value)
        {
            switch (cell)
            {
                case null:
                    value = 0;
                    return false;
                case double number:
                    value = number;
                    return double.IsFinite(number);
                case float number:
                    value = number;
                    return float.IsFinite(number);
                case decimal number:
                    value = (double)number;
                    return true;
                case int number:
                    value = number;
                    return true;
                case long number:
                    value = number;
                    return true;
                case short number:
                    value = number;
                    return true;
                case byte number:
                    value = number;
                    return true;
                case string text:
                    return TryParse(text, out value);
                default:
                    return TryParse(cell.ToString(), out value);
            }
        }

        public static string ToText(object? cell)
        {
            switch (cell)
            {
                case null:
                    return string.Empty;
                case double number:
                    return number.ToString("G17", CultureInfo.InvariantCulture);
                case float number:
                    return number.ToString("G9", CultureInfo.InvariantCulture);
                case decimal number:
                    return number.ToString(CultureInfo.InvariantCulture);
                case int or long or short or byte or uint or ulong:
                    return Convert.ToString(cell, CultureInfo.InvariantCulture) ?? string.Empty;
                default:
                    return cell.ToString() ?? string.Empty;
            }
        }

        public static string Format(double value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
