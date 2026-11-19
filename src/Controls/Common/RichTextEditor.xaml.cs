using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 通用富文本编辑器：工具栏 + RichTextBox。只负责富文本本身，不包含任何业务逻辑。
    /// 帮助文档（图解 / 地质温压计 / EPMA）统一使用此控件。
    /// <para>
    /// 两种使用方式：
    /// 1) 宿主通过 <see cref="WriteRtf"/> / <see cref="ReadRtf"/> / <see cref="ContentEdited"/> 驱动，并提供 historyKey（如语言代码），
    ///    控件按 key 维护独立的撤销/重做历史；
    /// 2) 宿主直接操作 <see cref="Editor"/>（不提供 historyKey），撤销/重做使用 RichTextBox 自带的功能。
    /// </para>
    /// </summary>
    public partial class RichTextEditor : UserControl
    {
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(RichTextEditor), new PropertyMetadata(false));

        public static readonly DependencyProperty IsToolbarVisibleProperty = DependencyProperty.Register(
            nameof(IsToolbarVisible), typeof(bool), typeof(RichTextEditor), new PropertyMetadata(true));

        public static readonly DependencyProperty IsToolbarEnabledProperty = DependencyProperty.Register(
            nameof(IsToolbarEnabled), typeof(bool), typeof(RichTextEditor), new PropertyMetadata(true));

        public static readonly DependencyProperty IsHistoryVisibleProperty = DependencyProperty.Register(
            nameof(IsHistoryVisible), typeof(bool), typeof(RichTextEditor), new PropertyMetadata(true));

        public static readonly DependencyProperty ToolbarTrailingContentProperty = DependencyProperty.Register(
            nameof(ToolbarTrailingContent), typeof(object), typeof(RichTextEditor), new PropertyMetadata(null));

        public static readonly DependencyProperty ToolbarPaddingProperty = DependencyProperty.Register(
            nameof(ToolbarPadding), typeof(Thickness), typeof(RichTextEditor), new PropertyMetadata(new Thickness(10, 7, 10, 7)));

        public static readonly DependencyProperty SelectedTextColorProperty = DependencyProperty.Register(
            nameof(SelectedTextColor), typeof(Color), typeof(RichTextEditor),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TextColorChanged));

        /// <summary>格式工具栏的内边距（调整工具栏高度 / 两侧留白）。</summary>
        public Thickness ToolbarPadding { get => (Thickness)GetValue(ToolbarPaddingProperty); set => SetValue(ToolbarPaddingProperty, value); }

        /// <summary>文档是否只读。</summary>
        public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

        /// <summary>是否显示格式工具栏。</summary>
        public bool IsToolbarVisible { get => (bool)GetValue(IsToolbarVisibleProperty); set => SetValue(IsToolbarVisibleProperty, value); }

        /// <summary>格式工具栏是否可用。</summary>
        public bool IsToolbarEnabled { get => (bool)GetValue(IsToolbarEnabledProperty); set => SetValue(IsToolbarEnabledProperty, value); }

        /// <summary>是否显示撤销 / 重做按钮。</summary>
        public bool IsHistoryVisible { get => (bool)GetValue(IsHistoryVisibleProperty); set => SetValue(IsHistoryVisibleProperty, value); }

        /// <summary>工具栏右侧的附加内容（始终显示，即使格式工具栏被隐藏）。</summary>
        public object? ToolbarTrailingContent { get => GetValue(ToolbarTrailingContentProperty); set => SetValue(ToolbarTrailingContentProperty, value); }

        public Color SelectedTextColor { get => (Color)GetValue(SelectedTextColorProperty); set => SetValue(SelectedTextColorProperty, value); }

        /// <summary>内部的 RichTextBox，供需要直接读写文档的宿主使用。</summary>
        public RichTextBox Editor => HelpEditor;

        /// <summary>用户编辑了内容（仅在提供 historyKey 的模式下触发），参数为当前 RTF。</summary>
        public event EventHandler<string>? ContentEdited;

        private static void TextColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var view = (RichTextEditor)sender;
            if (view._loading || view._updatingToolbar || view.HelpEditor is null || view.HelpEditor.IsReadOnly) return;
            var color = (Color)e.NewValue;
            var brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
            brush.Freeze();
            view.HelpEditor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
        }

        private sealed class History { public List<string> Items { get; } = []; public int Index { get; set; } }
        private readonly Dictionary<string, History> _histories = new(StringComparer.OrdinalIgnoreCase);
        private bool _loading;
        private bool _updatingToolbar;
        private string _fontSizeAtFocus = "14";
        private string? _historyKey;

        public RichTextEditor()
        {
            InitializeComponent();
            UpdateHistoryActions();
        }

        /// <summary>读取 RTF；没有任何文字时返回空串（表示该文档不存在）。</summary>
        public string? ReadRtf()
        {
            if (new TextRange(HelpEditor.Document.ContentStart, HelpEditor.Document.ContentEnd).Text.Trim().Length == 0) return "";
            return RtfHelper.GetRtfString(HelpEditor);
        }

        /// <summary>
        /// 写入 RTF 并切换到 <paramref name="historyKey"/> 对应的撤销历史。
        /// </summary>
        public void WriteRtf(string? rtf, string? historyKey)
        {
            if (FontColorButton.FindName("ColorPopup") is Popup popup) popup.IsOpen = false;
            _historyKey = historyKey;
            LoadRtf(rtf);
            if (_historyKey is not null && !_histories.ContainsKey(_historyKey))
                _histories[_historyKey] = new History { Items = { ReadRtf() ?? "" } };
            UpdateHistoryActions();
        }

        private void LoadRtf(string? rtf)
        {
            _loading = true;
            try
            {
                HelpEditor.IsUndoEnabled = false;
                HelpEditor.Document.Blocks.Clear();
                if (!string.IsNullOrEmpty(rtf))
                    try { RtfHelper.LoadRtfString(HelpEditor, rtf); }
                    catch { HelpEditor.Document.Blocks.Clear(); }
            }
            finally { HelpEditor.IsUndoEnabled = true; _loading = false; SynchronizeToolbar(); }
        }

        private void DocumentTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading || HelpEditor is null) return;
            if (_historyKey is null)
            {
                // 宿主直接操作 RichTextBox 的模式：使用其自带的撤销栈
                UpdateHistoryActions();
                SynchronizeToolbar();
                return;
            }
            if (HelpEditor.IsReadOnly) return;
            string rtf = ReadRtf() ?? "";
            ContentEdited?.Invoke(this, rtf);
            if (!_histories.TryGetValue(_historyKey, out var history)) _histories[_historyKey] = history = new History();
            if (history.Items.Count > 0 && history.Items[history.Index] == rtf) return;
            if (history.Index + 1 < history.Items.Count) history.Items.RemoveRange(history.Index + 1, history.Items.Count - history.Index - 1);
            history.Items.Add(rtf);
            if (history.Items.Count > 50) history.Items.RemoveAt(0);
            history.Index = history.Items.Count - 1;
            UpdateHistoryActions();
            SynchronizeToolbar();
        }

        private void UpdateHistoryActions()
        {
            if (UndoButton is null || RedoButton is null || HelpEditor is null) return;
            if (_historyKey is null)
            {
                UndoButton.IsEnabled = HelpEditor.CanUndo;
                RedoButton.IsEnabled = HelpEditor.CanRedo;
                return;
            }
            _histories.TryGetValue(_historyKey, out var history);
            UndoButton.IsEnabled = history is { Index: > 0 };
            RedoButton.IsEnabled = history is not null && history.Index + 1 < history.Items.Count;
        }

        private void MoveHistory(int delta)
        {
            if (HelpEditor.IsReadOnly) return;
            if (_historyKey is null)
            {
                if (delta < 0) HelpEditor.Undo(); else HelpEditor.Redo();
                HelpEditor.Focus();
                return;
            }
            if (!_histories.TryGetValue(_historyKey, out var history)) return;
            int index = history.Index + delta;
            if (index < 0 || index >= history.Items.Count) return;
            history.Index = index;
            LoadRtf(history.Items[index]);
            ContentEdited?.Invoke(this, ReadRtf() ?? "");
            UpdateHistoryActions();
            HelpEditor.Focus();
        }

        private void UndoClick(object sender, RoutedEventArgs e) => MoveHistory(-1);
        private void RedoClick(object sender, RoutedEventArgs e) => MoveHistory(1);

        private void DocumentKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                HelpEditor.MoveFocus(new TraversalRequest((Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next));
                e.Handled = true;
                return;
            }
            // 自定义历史模式下接管 Ctrl+Z / Ctrl+Y；否则交给 RichTextBox 自带的撤销
            if (_historyKey is null || (Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
            if (e.Key == Key.Z) { MoveHistory((Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 1 : -1); e.Handled = true; }
            if (e.Key == Key.Y) { MoveHistory(1); e.Handled = true; }
        }

        private void BoldClick(object sender, RoutedEventArgs e) => Apply(EditingCommands.ToggleBold);
        private void ItalicClick(object sender, RoutedEventArgs e) => Apply(EditingCommands.ToggleItalic);
        private void UnderlineClick(object sender, RoutedEventArgs e) => Apply(EditingCommands.ToggleUnderline);
        private void BulletsClick(object sender, RoutedEventArgs e) => Apply(EditingCommands.ToggleBullets);
        private void NumberingClick(object sender, RoutedEventArgs e) => Apply(EditingCommands.ToggleNumbering);
        private void IndentationCanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = false; e.Handled = true; }

        // 上下标由 RtfHelper 处理：系统自带的 ToggleSuperscript 只设置 Typography.Variants，多数字体下没有可见效果。
        private void SuperscriptClick(object sender, RoutedEventArgs e) => ToggleScript(FontVariants.Superscript);
        private void SubscriptClick(object sender, RoutedEventArgs e) => ToggleScript(FontVariants.Subscript);
        private void ToggleScript(FontVariants variant)
        {
            if (HelpEditor.IsReadOnly) return;
            RtfHelper.ToggleScript(HelpEditor, variant);
            HelpEditor.Focus();
        }

        private void StrikeClick(object sender, RoutedEventArgs e)
        {
            if (HelpEditor.IsReadOnly) return;
            var current = HelpEditor.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            var decorations = current is TextDecorationCollection collection ? collection.Clone() : new TextDecorationCollection();
            var strikes = decorations.Where(decoration => decoration.Location == TextDecorationLocation.Strikethrough).ToArray();
            if (strikes.Length > 0) foreach (var strike in strikes) decorations.Remove(strike);
            else decorations.Add(TextDecorations.Strikethrough[0]);
            HelpEditor.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, decorations);
            HelpEditor.Focus();
        }

        private void AlignClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string alignment }) Apply(alignment switch
            {
                "Center" => EditingCommands.AlignCenter, "Right" => EditingCommands.AlignRight, _ => EditingCommands.AlignLeft,
            });
        }

        private void ParagraphStyleChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || _updatingToolbar || HelpEditor is null || HelpEditor.IsReadOnly || ParagraphStyleBox.SelectedItem is not ComboBoxItem item) return;
            string style = item.Tag as string ?? "Body";
            HelpEditor.BeginChange();
            try
            {
                var range = new TextRange(HelpEditor.Selection.Start.Paragraph?.ContentStart ?? HelpEditor.Selection.Start,
                    HelpEditor.Selection.End.Paragraph?.ContentEnd ?? HelpEditor.Selection.End);
                range.ApplyPropertyValue(TextElement.FontSizeProperty, style switch { "H1" => 24d, "H2" => 18d, "H3" => 16d, _ => 14d });
                range.ApplyPropertyValue(TextElement.FontWeightProperty, style == "Body" ? FontWeights.Normal : FontWeights.SemiBold);
            }
            finally { HelpEditor.EndChange(); }
            HelpEditor.Focus();
        }

        private void Apply(RoutedUICommand command)
        {
            if (HelpEditor.IsReadOnly) return;
            command.Execute(null, HelpEditor);
            HelpEditor.Focus();
        }

        private void FontSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || _updatingToolbar || HelpEditor is null || HelpEditor.IsReadOnly) return;
            string? text = FontSizeBox.SelectedItem is ComboBoxItem item ? item.Content?.ToString() : FontSizeBox.SelectedItem as string;
            ApplyFontSize(text);
        }

        private void ApplyFontSize(string? text)
        {
            if (HelpEditor is null || HelpEditor.IsReadOnly || !double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out double size) || size < 8 || size > 96) return;
            HelpEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
        }

        private void FontSizeGotFocus(object sender, KeyboardFocusChangedEventArgs e) => _fontSizeAtFocus = FontSizeBox.Text;

        private void FontSizeLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (FontSizeBox.Text != _fontSizeAtFocus) ApplyFontSize(FontSizeBox.Text);
        }

        private void FontSizeKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            ApplyFontSize(FontSizeBox.Text);
            HelpEditor.Focus();
            e.Handled = true;
        }

        private void DocumentSelectionChanged(object sender, RoutedEventArgs e) => SynchronizeToolbar();

        private void SynchronizeToolbar()
        {
            if (_loading || _updatingToolbar || HelpEditor is null || FontSizeBox is null || ParagraphStyleBox is null) return;
            _updatingToolbar = true;
            try
            {
                object value = HelpEditor.Selection.GetPropertyValue(TextElement.FontSizeProperty);
                if (value is double size && !FontSizeBox.IsKeyboardFocusWithin)
                {
                    FontSizeBox.Text = size.ToString("0.#", CultureInfo.CurrentCulture);
                    bool heading = HelpEditor.Selection.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight weight && weight >= FontWeights.SemiBold;
                    ParagraphStyleBox.SelectedIndex = heading && size == 24 ? 1 : heading && size == 18 ? 2 : heading && size == 16 ? 3 : 0;
                }
                if (HelpEditor.Selection.GetPropertyValue(TextElement.ForegroundProperty) is SolidColorBrush brush)
                    SetCurrentValue(SelectedTextColorProperty, Color.FromRgb(brush.Color.R, brush.Color.G, brush.Color.B));
            }
            finally { _updatingToolbar = false; }
        }
    }
}
