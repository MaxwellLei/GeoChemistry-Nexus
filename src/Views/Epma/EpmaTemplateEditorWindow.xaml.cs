using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.Views;

public partial class EpmaTemplateEditorWindow : Window, INotificationCenterHost
{
    public EpmaTemplateEditorViewModel Editor { get; }
    private bool _composingIcon;
    private int _previewAnimationVersion;
    public EpmaTemplateEditorWindow() : this(null) { }
    public EpmaTemplateEditorWindow(EpmaInstalledTemplate? template)
    {
        Editor = new(template);
        InitializeComponent();
        DataContext = Editor;
        Editor.PropertyChanged += EditorPropertyChanged;
        UpdatePreview(false);
        TextCompositionManager.AddPreviewTextInputStartHandler(TemplateIconEditor, (_, _) => _composingIcon = true);
        TextCompositionManager.AddPreviewTextInputHandler(TemplateIconEditor, (_, _) =>
        {
            _composingIcon = false;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new System.Action(() => UpdateIconText(TemplateIconEditor)));
        });
        GeoChemistryNexus.Helpers.UiScaleHelper.Attach(this);
        GeoChemistryNexus.Helpers.WindowWorkAreaHelper.Attach(this);
        SourceInitialized += (_, _) =>
        {
            // Let the compositor draw the native shadow outside the window. No bitmap effect wraps the editor.
            int policy = 2; // DWMNCRP_ENABLED
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 2, ref policy, sizeof(int));
            FitToOwner();
        };
        Loaded += (_, _) => { FitToOwner(); UpdatePreview(false); };
        Editor.InputColumns.CollectionChanged += InputColumnAdded;
        Editor.OutputColumns.CollectionChanged += OutputColumnAdded;
        Editor.ExampleRows.CollectionChanged += ExampleRowAdded;
        Editor.Saved += () => DialogResult = true;
        Editor.NotificationRequested += ShowLocalNotification;
        Closed += (_, _) =>
        {
            Notifications.Dispose();
            Editor.NotificationRequested -= ShowLocalNotification;
            Editor.InputColumns.CollectionChanged -= InputColumnAdded;
            Editor.OutputColumns.CollectionChanged -= OutputColumnAdded;
            Editor.ExampleRows.CollectionChanged -= ExampleRowAdded;
            Editor.PropertyChanged -= EditorPropertyChanged;
            Editor.Release();
        };
    }
    /// <summary>本窗口独立的通知中心（由 XAML 中的 NotificationHost 呈现；警告与错误会保留到用户关闭）。</summary>
    public NotificationCenter Notifications { get; } = new();

    private void ShowLocalNotification(NotificationType type, string message) => Notifications.Show(type, message);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attribute, ref int value, int size);

    private void FitToOwner()
    {
        if (WindowState != WindowState.Normal) return;
        var owner = Owner ?? Application.Current?.MainWindow;
        if (ReferenceEquals(owner, this)) owner = null;
        double ownerHeight = owner is { ActualHeight: > 0 } ? owner.ActualHeight : owner?.Height ?? SystemParameters.WorkArea.Height;
        if (double.IsNaN(ownerHeight) || double.IsInfinity(ownerHeight)) ownerHeight = SystemParameters.WorkArea.Height;
        double available = System.Math.Min(ownerHeight - 72, SystemParameters.WorkArea.Height - 48);
        double initialHeight = System.Math.Max(360, System.Math.Min(700, available));
        MinHeight = System.Math.Min(560, initialHeight);
        Height = initialHeight;
    }
    private void EditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Editor.Step) or nameof(Editor.IsBusy)) AddLanguageButton.IsDropDownOpen = false;
        if (e.PropertyName == nameof(Editor.Step)) UpdatePreview(false);
        else if (e.PropertyName == nameof(Editor.PreviewExpanded)) UpdatePreview(true);
    }
    private void FinishPreviewTransition(bool expanded)
    {
        // Commit the final base values before removing held animation clocks, avoiding a one-frame fallback.
        PreviewSlot.Width = expanded ? 254 : 0;
        TemplatePreviewPanel.Opacity = expanded ? 1 : 0;
        PreviewSlide.X = expanded ? 0 : 14;
        PreviewSlot.BeginAnimation(WidthProperty, null);
        TemplatePreviewPanel.BeginAnimation(OpacityProperty, null);
        PreviewSlide.BeginAnimation(TranslateTransform.XProperty, null);
        PreviewSlot.Visibility = TemplatePreviewPanel.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        PreviewSlot.IsHitTestVisible = expanded;
    }
    private void UpdatePreview(bool animate)
    {
        bool expanded = Editor.IsPreviewVisible;
        int version = ++_previewAnimationVersion;
        if (!animate || EditorSurface.ActualWidth <= 0 || !SystemParameters.ClientAreaAnimation)
        {
            FinishPreviewTransition(expanded); return;
        }
        // Read current animated values so a quick reversal continues from the visible position.
        double width = PreviewSlot.Width, opacity = TemplatePreviewPanel.Opacity, offset = PreviewSlide.X;
        double targetWidth = expanded ? 254 : 0;
        PreviewSlot.Width = width;
        TemplatePreviewPanel.Opacity = opacity;
        PreviewSlide.X = offset;
        PreviewSlot.BeginAnimation(WidthProperty, null);
        TemplatePreviewPanel.BeginAnimation(OpacityProperty, null);
        PreviewSlide.BeginAnimation(TranslateTransform.XProperty, null);
        PreviewSlot.Visibility = TemplatePreviewPanel.Visibility = Visibility.Visible;
        PreviewSlot.IsHitTestVisible = expanded;
        var resize = new DoubleAnimation(width, targetWidth, new Duration(System.TimeSpan.FromMilliseconds(240)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }, FillBehavior = FillBehavior.HoldEnd };
        resize.Completed += (_, _) => { if (version == _previewAnimationVersion) FinishPreviewTransition(expanded); };
        PreviewSlot.BeginAnimation(WidthProperty, resize, HandoffBehavior.SnapshotAndReplace);
        TemplatePreviewPanel.BeginAnimation(OpacityProperty,
            new DoubleAnimation(opacity, expanded ? 1 : 0, new Duration(System.TimeSpan.FromMilliseconds(180)))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd });
        PreviewSlide.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(offset, expanded ? 0 : 14, new Duration(System.TimeSpan.FromMilliseconds(240)))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd });
    }
    private void IconTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_composingIcon && sender is TextBox input && DataContext is EpmaTemplateEditorViewModel)
            UpdateIconText(input);
    }
    private void UpdateIconText(TextBox input)
    {
        if (Editor.IsReadOnly || _composingIcon) return;
        string text = EpmaTemplateIcon.Normalize(input.Text);
        if (input.Text != text)
        {
            int caret = input.CaretIndex;
            input.Text = text;
            input.CaretIndex = System.Math.Min(caret, text.Length);
        }
        Editor.TemplateIcon = text;
    }
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        var focused = Keyboard.FocusedElement as DependencyObject ?? FocusManager.GetFocusedElement(this) as DependencyObject;
        if (InputAncestor(focused) is not null && InputAncestor(e.OriginalSource as DependencyObject) is null)
        {
            Keyboard.ClearFocus();
            FocusManager.SetFocusedElement(this, EditorSurface);
            EditorSurface.Focus();
        }
        base.OnPreviewMouseDown(e);
    }
    protected override void OnDeactivated(System.EventArgs e)
    {
        FocusManager.SetFocusedElement(this, null);
        base.OnDeactivated(e);
    }
    private static DependencyObject? InputAncestor(DependencyObject? element)
    {
        for (var current = element; current is not null;)
        {
            if (current is TextBoxBase or ComboBox) return current;
            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        }
        return null;
    }
    private int AvailableDescriptionLineBreaks()
    {
        string remaining = DescriptionEditor.Text.Remove(DescriptionEditor.SelectionStart, DescriptionEditor.SelectionLength);
        return System.Math.Max(0, 4 - remaining.ReplaceLineEndings("\n").Count(c => c == '\n'));
    }
    private static string LimitDescriptionInsertion(string text, int availableBreaks)
    {
        string normalized = text.ReplaceLineEndings("\n");
        int breaks = 0;
        for (int i = 0; i < normalized.Length; i++)
            if (normalized[i] == '\n' && ++breaks > availableBreaks)
                return normalized[..i].ReplaceLineEndings(System.Environment.NewLine);
        return normalized.ReplaceLineEndings(System.Environment.NewLine);
    }
    private void DescriptionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && AvailableDescriptionLineBreaks() == 0) e.Handled = true;
    }
    private void DescriptionTextInput(object sender, TextCompositionEventArgs e)
    {
        if (DescriptionEditor.IsReadOnly) return;
        if (!e.Text.Contains('\r') && !e.Text.Contains('\n')) return;
        e.Handled = true;
        string insertion = LimitDescriptionInsertion(e.Text, AvailableDescriptionLineBreaks());
        int start = DescriptionEditor.SelectionStart;
        DescriptionEditor.SelectedText = insertion;
        DescriptionEditor.Select(start + insertion.Length, 0);
    }
    private void DescriptionPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (DescriptionEditor.IsReadOnly) return;
        string? text = e.DataObject.GetData(DataFormats.UnicodeText) as string ?? e.DataObject.GetData(DataFormats.Text) as string;
        if (text is null) return;
        string insertion = LimitDescriptionInsertion(text, AvailableDescriptionLineBreaks());
        if (insertion.Length == 0 && text.Length > 0) { e.CancelCommand(); return; }
        e.DataObject = new DataObject(DataFormats.UnicodeText, insertion);
    }
    private void ResizeDescription(double horizontal, double vertical)
    {
        if (horizontal != 0)
        {
            DescriptionResizeFrame.Width = System.Math.Clamp(DescriptionResizeFrame.ActualWidth + horizontal,
                DescriptionResizeFrame.MinWidth, System.Math.Max(DescriptionResizeFrame.MinWidth, DescriptionFieldContainer.ActualWidth));
            DescriptionResizeFrame.HorizontalAlignment = HorizontalAlignment.Left;
        }
        DescriptionResizeFrame.Height = System.Math.Clamp(DescriptionResizeFrame.ActualHeight + vertical,
            DescriptionResizeFrame.MinHeight, DescriptionResizeFrame.MaxHeight);
    }
    private void DescriptionResizeDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeDescription(e.HorizontalChange, e.VerticalChange); e.Handled = true;
    }
    private void DescriptionResizeKeyDown(object sender, KeyEventArgs e)
    {
        double step = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 24 : 8;
        switch (e.Key)
        {
            case Key.Left: ResizeDescription(-step, 0); break;
            case Key.Right: ResizeDescription(step, 0); break;
            case Key.Up: ResizeDescription(0, -step); break;
            case Key.Down: ResizeDescription(0, step); break;
            default: return;
        }
        e.Handled = true;
    }
    private void InputColumnAdded(object? sender, NotifyCollectionChangedEventArgs e) => FocusAddedColumn(e, InputsEditor, InputColumnsScroll);
    private void OutputColumnAdded(object? sender, NotifyCollectionChangedEventArgs e) => FocusAddedColumn(e, OutputsEditor, OutputColumnsScroll);
    private void ColumnTablesMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) return;
        e.Handled = true;
        ScriptPageScroll.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            { RoutedEvent = Mouse.MouseWheelEvent, Source = ScriptPageScroll });
    }
    private void ExampleRowAdded(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || !Editor.CanEdit || Editor.InputColumns.Count == 0) return;
        int row = e.NewStartingIndex;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new System.Action(() =>
        {
            if (row >= Editor.ExampleRows.Count || Editor.InputColumns.Count == 0) return;
            InputsEditor.UpdateLayout();
            InputColumnsScroll.ScrollToLeftEnd();
            var cell = Editor.InputColumns[0].Examples[row];
            if (FindExampleCell(InputsEditor, cell) is { } editor)
            {
                editor.Focus(); editor.SelectAll(); editor.BringIntoView();
            }
        }));
    }
    private static TextBox? FindExampleCell(DependencyObject root, EpmaEditorExampleCell cell)
    {
        if (root is TextBox editor && ReferenceEquals(editor.DataContext, cell)) return editor;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (FindExampleCell(VisualTreeHelper.GetChild(root, i), cell) is { } found) return found;
        return null;
    }
    private void FocusAddedColumn(NotifyCollectionChangedEventArgs e, ItemsControl table, ScrollViewer scroll)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is not { Count: > 0 } || !Editor.CanEdit) return;
        var column = e.NewItems[e.NewItems.Count - 1];
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new System.Action(() =>
        {
            if (!table.Items.Contains(column)) return;
            table.UpdateLayout();
            scroll.ScrollToRightEnd();
            if (table.ItemContainerGenerator.ContainerFromItem(column) is DependencyObject container
                && FindColumnName(container) is { } name)
            {
                name.Focus(); name.SelectAll(); name.BringIntoView();
            }
        }));
    }
    private static TextBox? FindColumnName(DependencyObject root)
    {
        if (root is TextBox name) return name;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (FindColumnName(VisualTreeHelper.GetChild(root, i)) is { } found) return found;
        return null;
    }
    private void ColumnCellPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Tab || (Keyboard.Modifiers & ~ModifierKeys.Shift) != 0 || sender is not TextBox cell) return;
        e.Handled = MoveColumnCellFocus(cell, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
    }
    private bool MoveColumnCellFocus(TextBox cell, bool backward)
    {
        ItemsControl? table = null;
        for (DependencyObject? parent = cell; parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (ReferenceEquals(parent, InputsEditor)) { table = InputsEditor; break; }
            if (ReferenceEquals(parent, OutputsEditor)) { table = OutputsEditor; break; }
        }
        if (table is null) return false;
        // Each item contains a whole column, including its delete button and
        // examples. Traverse the displayed cells row by row instead of following
        // that column-first visual tree. Off-screen columns remain in the order.
        table.UpdateLayout();
        var cells = ColumnCells(table)
            .Where(candidate => candidate.IsEnabled && candidate.Focusable && candidate.Visibility == Visibility.Visible)
            .OrderBy(candidate => candidate.TranslatePoint(new Point(), table).Y)
            .ThenBy(candidate => candidate.TranslatePoint(new Point(), table).X)
            .ToList();
        int current = cells.IndexOf(cell);
        if (current < 0) return false;
        int next = current + (backward ? -1 : 1);
        if (next < 0)
            return table.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        // The final cell is also the final text field in the visual tree, so
        // normal navigation can leave the table for its add-column button.
        if (next >= cells.Count) return false;
        var target = cells[next];
        target.Focus(); target.SelectAll(); target.BringIntoView();
        return true;
    }
    private static IEnumerable<TextBox> ColumnCells(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBox cell) yield return cell;
            else foreach (var nested in ColumnCells(child)) yield return nested;
        }
    }
    private void ColumnCellPasting(object sender, DataObjectPastingEventArgs e)
    {
        // 多行多列内容（如从 Excel 复制）按行列铺开到表格，而不是塞进同一个单元格
        EditorColumnGridPasteHelper.HandlePasting(sender as TextBox, InputsEditor, OutputsEditor, e, Editor.PasteGrid);
    }
    private void RemoveLanguage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GeoChemistryNexus.Models.LanguageTagModel language }) Editor.RemoveLanguage(language);
    }
    private void LocalizationClick(object sender, RoutedEventArgs e)
    {
        var sheet = Editor.CreateLocalizationSheet();
        if (new EpmaLocalizationWindow(sheet) { Owner = this }.ShowDialog() == true) Editor.ApplyLocalizationSheet(sheet);
    }
    private async void SaveClick(object sender, RoutedEventArgs e) => await Editor.SaveCommand.ExecuteAsync(null);
    private void CancelClick(object sender, RoutedEventArgs e) { if (Editor.IsBusy) Editor.CancelPending(); else Close(); }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!Editor.IsBusy) return;
        Editor.CancelPending(); e.Cancel = true;
    }
}
