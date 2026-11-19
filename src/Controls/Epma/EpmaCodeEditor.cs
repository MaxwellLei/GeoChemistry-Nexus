using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls;

/// <summary>A native TextBox with a scrolling gutter, basic indentation and a syntax drawing layer.</summary>
public class EpmaCodeEditor : TextBox
{
    private EpmaCodeLineNumbers? _lineNumbers;
    private EpmaCodeCurrentLine? _currentLine;
    private EpmaCodeSyntaxLayer? _syntaxLayer;
    private bool _syntaxNeedsLayout;
    internal IReadOnlyList<EpmaScriptToken> SyntaxTokens { get; private set; } = [];
    private readonly List<int> _lineStarts = [0];
    // TextBox's line-index helpers assume its default font metrics even when a
    // custom LineHeight is set. This unwrapped editor maps physical source lines
    // explicitly so caret status, gutter and scrolling use the same 22 px rows.
    public new int LineCount => _lineStarts.Count;
    public new int GetLineIndexFromCharacterIndex(int index)
    {
        int line = _lineStarts.BinarySearch(index);
        return line >= 0 ? line : ~line - 1;
    }
    public new int GetCharacterIndexFromLineIndex(int line) => line >= 0 && line < LineCount ? _lineStarts[line] : -1;
    private double CodeLineHeight => double.IsNaN(TextBlock.GetLineHeight(this)) ? FontSize * 1.2 : TextBlock.GetLineHeight(this);
    public new int GetFirstVisibleLineIndex() => Math.Clamp((int)Math.Floor(VerticalOffset / CodeLineHeight), 0, LineCount - 1);
    public new int GetLastVisibleLineIndex() => Math.Clamp((int)Math.Ceiling((VerticalOffset + ViewportHeight) / CodeLineHeight), 0, LineCount - 1);
    public new void ScrollToLine(int line) => ScrollToVerticalOffset(Math.Max(0, line) * CodeLineHeight);
    public Rect CurrentLineBounds
    {
        get
        {
            if (LineCount <= 0) return Rect.Empty;
            return GetCodeLineBounds(GetLineIndexFromCharacterIndex(CaretIndex));
        }
    }
    internal Rect GetCodeLineBounds(int line)
    {
        if (line < 0 || line >= LineCount) return Rect.Empty;
        var text = GetRectFromCharacterIndex(GetCharacterIndexFromLineIndex(line), true);
        if (text.IsEmpty) return Rect.Empty;
        double height = TextBlock.GetLineHeight(this);
        if (double.IsNaN(height)) height = Math.Max(text.Height, FontSize * 1.2);
        // WPF returns glyph bounds rather than the full line box. Center the
        // highlight and gutter around those glyphs, using the configured spacing.
        var row = new Rect(0, text.Top - (height - text.Height) / 2, ActualWidth, height);
        return row.Bottom <= 0 || row.Top >= ActualHeight ? Rect.Empty : row;
    }
    private static readonly DependencyPropertyKey CaretLocationPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CaretLocation), typeof(string), typeof(EpmaCodeEditor), new PropertyMetadata("Ln 1, Col 1"));
    public static readonly DependencyProperty CaretLocationProperty = CaretLocationPropertyKey.DependencyProperty;
    public string CaretLocation => (string)GetValue(CaretLocationProperty);

    public EpmaCodeEditor()
    {
        TextChanged += (_, _) =>
        {
            string source = Text;
            _lineStarts.Clear(); _lineStarts.Add(0);
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == '\r')
                {
                    if (i + 1 < source.Length && source[i + 1] == '\n') i++;
                    _lineStarts.Add(i + 1);
                }
                else if (source[i] == '\n') _lineStarts.Add(i + 1);
            }
            SyntaxTokens = EpmaScriptHighlighting.Tokenize(source);
            _syntaxNeedsLayout = true;
            UpdateEditorState();
        };
        // TextChanged can arrive before the native text view has new glyph
        // geometry. Repaint once after layout, when those positions are valid.
        LayoutUpdated += (_, _) =>
        {
            if (!_syntaxNeedsLayout) return;
            _syntaxNeedsLayout = false;
            _syntaxLayer?.InvalidateVisual();
        };
        SelectionChanged += (_, _) => UpdateEditorState();
        SizeChanged += (_, _) => InvalidateDecorations();
        AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, _) => InvalidateDecorations()));
        SpellCheck.SetIsEnabled(this, false);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _currentLine = GetTemplateChild("PART_CurrentLine") as EpmaCodeCurrentLine;
        if (_currentLine is not null) _currentLine.Editor = this;
        _lineNumbers = GetTemplateChild("PART_LineNumbers") as EpmaCodeLineNumbers;
        if (_lineNumbers is not null) _lineNumbers.Editor = this;
        _syntaxLayer = GetTemplateChild("PART_Syntax") as EpmaCodeSyntaxLayer;
        if (_syntaxLayer is not null) _syntaxLayer.Editor = this;
        UpdateEditorState();
    }

    private void UpdateEditorState()
    {
        int line = GetLineIndexFromCharacterIndex(CaretIndex);
        int start = line < 0 ? 0 : GetCharacterIndexFromLineIndex(line);
        SetValue(CaretLocationPropertyKey, $"Ln {Math.Max(0, line) + 1}, Col {CaretIndex - Math.Max(0, start) + 1}");
        if (_lineNumbers is not null)
        {
            _lineNumbers.Width = Math.Max(48, (Math.Max(1, LineCount).ToString(CultureInfo.InvariantCulture).Length + 2) * FontSize * 0.65);
            _lineNumbers.InvalidateVisual();
        }
        _currentLine?.InvalidateVisual();
        _syntaxLayer?.InvalidateVisual();
    }

    private void InvalidateDecorations()
    {
        _syntaxNeedsLayout = true;
        _lineNumbers?.InvalidateVisual(); _currentLine?.InvalidateVisual(); _syntaxLayer?.InvalidateVisual();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!IsReadOnly && Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Tab)
        {
            ReplaceSelection(new string(' ', 4));
            e.Handled = true;
        }
        else if (!IsReadOnly && Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter)
        {
            int line = GetLineIndexFromCharacterIndex(SelectionStart);
            int start = line < 0 ? 0 : GetCharacterIndexFromLineIndex(line);
            string prefix = Text.Substring(Math.Max(0, start), SelectionStart - Math.Max(0, start));
            string indent = new(prefix.TakeWhile(c => c is ' ' or '\t').ToArray());
            if (prefix.TrimEnd().EndsWith('{')) indent += "    ";
            ReplaceSelection(Environment.NewLine + indent);
            e.Handled = true;
        }
        base.OnPreviewKeyDown(e);
    }

    private void ReplaceSelection(string text)
    {
        BeginChange();
        try
        {
            int start = SelectionStart;
            SelectedText = text;
            Select(start + text.Length, 0);
        }
        finally { EndChange(); }
    }
}

public class EpmaCodeCurrentLine : FrameworkElement
{
    internal EpmaCodeEditor? Editor { get; set; }
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Editor is not { } editor || editor.CurrentLineBounds.IsEmpty) return;
        var row = editor.CurrentLineBounds;
        drawingContext.PushClip(new RectangleGeometry(new Rect(RenderSize)));
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(234, 243, 255)),
            new Pen(new SolidColorBrush(Color.FromRgb(211, 228, 249)), 1),
            new Rect(0.5, row.Top + 0.5, Math.Max(0, ActualWidth - 1), Math.Max(0, row.Height - 1)));
        drawingContext.Pop();
    }
}

public class EpmaCodeLineNumbers : FrameworkElement
{
    internal EpmaCodeEditor? Editor { get; set; }
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(243, 246, 250)), null, new Rect(RenderSize));
        if (Editor is not { } editor || editor.LineCount == 0) return;
        if (!editor.CurrentLineBounds.IsEmpty)
            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(234, 243, 255)), null,
                new Rect(0, editor.CurrentLineBounds.Top, ActualWidth, editor.CurrentLineBounds.Height));
        int first = editor.GetFirstVisibleLineIndex(), last = editor.GetLastVisibleLineIndex();
        int current = editor.GetLineIndexFromCharacterIndex(editor.CaretIndex);
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(editor.FontFamily, editor.FontStyle, editor.FontWeight, editor.FontStretch);
        for (int line = Math.Max(0, first); line <= last; line++)
        {
            var rect = editor.GetCodeLineBounds(line);
            if (rect.IsEmpty) continue;
            var color = line == current ? Color.FromRgb(3, 105, 161) : Color.FromRgb(148, 163, 184);
            var number = new FormattedText((line + 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, typeface, editor.FontSize, new SolidColorBrush(color), dpi);
            drawingContext.DrawText(number, new Point(ActualWidth - number.Width - 12, rect.Top + (rect.Height - number.Height) / 2));
        }
    }
}
