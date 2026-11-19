using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls;

/// <summary>
/// Paints only visible lines over the transparent native text. The TextBox still
/// owns selection, caret, IME, clipboard, accessibility and undo; colouring never edits text.
/// </summary>
public class EpmaCodeSyntaxLayer : FrameworkElement
{
    internal EpmaCodeEditor? Editor { get; set; }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Editor is not { } editor || FindViewport(editor) is not { } viewport) return;
        var clip = new Rect(viewport.TranslatePoint(new Point(), this), viewport.RenderSize);
        clip.Intersect(new Rect(RenderSize));
        if (clip.IsEmpty) return;
        drawingContext.PushClip(new RectangleGeometry(clip));
        string source = editor.Text;
        var typeface = new Typeface(editor.FontFamily, editor.FontStyle, editor.FontWeight, editor.FontStretch);
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        int first = editor.GetFirstVisibleLineIndex(), last = editor.GetLastVisibleLineIndex();
        int tokenIndex = 0;
        var tokens = editor.SyntaxTokens;
        int firstStart = editor.GetCharacterIndexFromLineIndex(first);
        while (tokenIndex < tokens.Count && tokens[tokenIndex].Start + tokens[tokenIndex].Length <= firstStart) tokenIndex++;
        for (int line = first; line <= last; line++)
        {
            int start = editor.GetCharacterIndexFromLineIndex(line);
            int end = line + 1 < editor.LineCount ? editor.GetCharacterIndexFromLineIndex(line + 1) : source.Length;
            while (end > start && source[end - 1] is '\r' or '\n') end--;
            if (end == start) continue;
            while (tokenIndex < tokens.Count && tokens[tokenIndex].Start + tokens[tokenIndex].Length <= start) tokenIndex++;
            // TextBox and FormattedText use different default tab stops. Anchor
            // each tab-separated segment to native glyph geometry so pasted tabs
            // keep the drawing, selection and caret at exactly the same positions.
            for (int segment = start; segment < end;)
            {
                if (source[segment] == '\t') { segment++; continue; }
                int tab = source.IndexOf('\t', segment, end - segment);
                int segmentEnd = tab < 0 ? end : tab;
                var rect = editor.GetRectFromCharacterIndex(segment);
                if (!rect.IsEmpty)
                {
                    var text = new FormattedText(source.Substring(segment, segmentEnd - segment), CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, typeface, editor.FontSize, EpmaScriptHighlighting.Plain,
                        null, TextOptions.GetTextFormattingMode(editor), dpi);
                    for (int index = tokenIndex; index < tokens.Count && tokens[index].Start < segmentEnd; index++)
                    {
                        var token = tokens[index];
                        int from = Math.Max(segment, token.Start), to = Math.Min(segmentEnd, token.Start + token.Length);
                        if (to > from) text.SetForegroundBrush(EpmaScriptHighlighting.BrushFor(token.Kind), from - segment, to - from);
                    }
                    var origin = editor.TranslatePoint(new Point(rect.Left, rect.Top + (rect.Height - text.Height) / 2), this);
                    drawingContext.DrawText(text, origin);
                }
                segment = segmentEnd;
            }
        }
        drawingContext.Pop();
    }

    private static ScrollContentPresenter? FindViewport(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ScrollContentPresenter viewport) return viewport;
            if (FindViewport(child) is { } nested) return nested;
        }
        return null;
    }
}
