using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls;

internal enum EpmaScriptTokenKind { Keyword, String, Number, Comment, Function, BuiltIn }
internal readonly record struct EpmaScriptToken(int Start, int Length, EpmaScriptTokenKind Kind);

/// <summary>Lexical colouring only: incomplete scripts remain editable without parsing or execution.</summary>
internal static class EpmaScriptHighlighting
{
    // Match comments/strings first so their contents cannot become keywords or numbers.
    // Template literals deliberately use one colour, including any ${...} expressions.
    private static readonly Regex Tokens = new(
        "(?<comment>//[^\\r\\n]*|/\\*[\\s\\S]*?(?:\\*/|$))"
        + "|(?<text>\"(?:\\\\[\\s\\S]|[^\"\\\\\\r\\n])*(?:\"|(?=\\r|\\n|$))"
        + "|'(?:\\\\[\\s\\S]|[^'\\\\\\r\\n])*(?:'|(?=\\r|\\n|$))"
        + "|`(?:\\\\[\\s\\S]|[^`\\\\])*(?:`|$))"
        + "|(?<number>(?<![\\w$])(?:0[xX][0-9a-fA-F_]+|0[bB][01_]+|0[oO][0-7_]+|(?:[0-9][0-9_]*(?:\\.[0-9_]*)?|\\.[0-9][0-9_]*)(?:[eE][+-]?[0-9_]+)?)n?)(?![\\w$])"
        + "|(?<identifier>[\\p{L}_$][\\p{L}\\p{N}_$]*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "async", "await", "break", "case", "catch", "class", "const", "continue", "debugger", "default",
        "delete", "do", "else", "export", "extends", "false", "finally", "for", "from", "function", "get",
        "if", "import", "in", "instanceof", "let", "new", "null", "of", "return", "set", "static", "super",
        "switch", "this", "throw", "true", "try", "typeof", "var", "void", "while", "with", "yield"
    };
    private static readonly HashSet<string> BuiltIns = new(StringComparer.Ordinal)
    {
        "Array", "Boolean", "Date", "Error", "Infinity", "JSON", "Map", "Math", "NaN", "Number", "Object",
        "RegExp", "Set", "String", "console", "undefined"
    };

    internal static IReadOnlyList<EpmaScriptToken> Tokenize(string source)
    {
        var tokens = new List<EpmaScriptToken>();
        foreach (Match match in Tokens.Matches(source))
        {
            EpmaScriptTokenKind kind;
            if (match.Groups["comment"].Success) kind = EpmaScriptTokenKind.Comment;
            else if (match.Groups["text"].Success) kind = EpmaScriptTokenKind.String;
            else if (match.Groups["number"].Success) kind = EpmaScriptTokenKind.Number;
            else if (Keywords.Contains(match.Value)) kind = EpmaScriptTokenKind.Keyword;
            else if (BuiltIns.Contains(match.Value)) kind = EpmaScriptTokenKind.BuiltIn;
            else
            {
                int next = match.Index + match.Length;
                while (next < source.Length && char.IsWhiteSpace(source[next])) next++;
                if (next == source.Length || source[next] != '(') continue;
                kind = EpmaScriptTokenKind.Function;
            }
            tokens.Add(new(match.Index, match.Length, kind));
        }
        return tokens;
    }

    internal static readonly Brush Plain = MakeBrush(0x24, 0x32, 0x47);
    private static readonly Brush Keyword = MakeBrush(0x00, 0x57, 0xB8);
    private static readonly Brush String = MakeBrush(0xA3, 0x3A, 0x16);
    private static readonly Brush Number = MakeBrush(0x09, 0x78, 0x62);
    private static readonly Brush Comment = MakeBrush(0x54, 0x7D, 0x45);
    private static readonly Brush Function = MakeBrush(0x79, 0x50, 0x9A);
    private static readonly Brush BuiltIn = MakeBrush(0x00, 0x78, 0x86);

    internal static Brush BrushFor(EpmaScriptTokenKind kind) => kind switch
    {
        EpmaScriptTokenKind.Keyword => Keyword,
        EpmaScriptTokenKind.String => String,
        EpmaScriptTokenKind.Number => Number,
        EpmaScriptTokenKind.Comment => Comment,
        EpmaScriptTokenKind.Function => Function,
        _ => BuiltIn
    };

    private static Brush MakeBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
