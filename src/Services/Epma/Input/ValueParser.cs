using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GeoChemistryNexus.Services.Epma.Input;

/// <summary>Strict numeric text and explicit detection-limit tokens. No unit stripping or internal-space repair.</summary>
public static class ValueParser
{
    private static readonly Regex DecimalPointPattern = new(
        @"^[+-]?([0-9]+(\.[0-9]*)?|\.[0-9]+)([eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant);
    private static readonly Regex DecimalCommaPattern = new(
        @"^[+-]?([0-9]+(,[0-9]*)?|,[0-9]+)([eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant);

    /// <summary>Named limits have no number; numeric limits require a finite, nonnegative number after &lt; or &lt;=.</summary>
    public static bool TryParseDetectionLimit(string text, NumericFormatChoice format, out double? limit)
    {
        ArgumentNullException.ThrowIfNull(text);
        limit = null;
        string token = text.Trim();
        if (token.ToLowerInvariant() is "bdl" or "b.d.l" or "b.d.l." or "below detection limit" or "n.d." or "nd")
            return true;
        if (token.Length == 0 || token[0] is not ('<' or '＜')) return false;
        string candidate = token[1..];
        if (candidate.StartsWith('=')) candidate = candidate[1..];
        if (!TryParseStrict(NumericCandidate(candidate), format, out double number) || number < 0) return false;
        limit = number;
        return true;
    }

    /// <summary>Normalize the full-width comma and outer whitespace only. Units are declared by the package.</summary>
    public static string NumericCandidate(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Replace('，', ',').Trim();
    }

    /// <summary>Explicit decimal syntax, without thousands separators; independent of UI culture.</summary>
    public static bool TryParseStrict(string candidate, NumericFormatChoice format, out double value)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        value = 0;
        var pattern = format switch
        {
            NumericFormatChoice.DecimalPoint => DecimalPointPattern,
            NumericFormatChoice.DecimalComma => DecimalCommaPattern,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
        if (!pattern.IsMatch(candidate)) return false;
        string invariant = format == NumericFormatChoice.DecimalComma ? candidate.Replace(',', '.') : candidate;
        if (!double.TryParse(invariant, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            || !double.IsFinite(parsed)) return false;
        value = parsed;
        return true;
    }

    public static string RawText(RawCell raw) => raw switch
    {
        RawCell.Text t => t.Value,
        RawCell.NativeNumber n => n.Value.ToString("R", CultureInfo.InvariantCulture),
        _ => string.Empty,
    };
}
