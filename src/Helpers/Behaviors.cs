using System;
using System.Collections.Generic;
using System.IO;

using System.Linq;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Controls.Primitives;

using System.Windows.Input;

using System.Windows.Media;

using Microsoft.Xaml.Behaviors;



namespace GeoChemistryNexus.Helpers

{

    // 继承自 FrameworkElement 以便附加到 Border 或 StackPanel

    public class TreeViewItemExpandBehavior : Behavior<FrameworkElement>

    {

        protected override void OnAttached()

        {

            base.OnAttached();

            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;

        }



        protected override void OnDetaching()

        {

            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;

            base.OnDetaching();

        }



        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)

        {

            // 尝试获取 TreeViewItem

            var treeViewItem = AssociatedObject.TemplatedParent as TreeViewItem;

            if (treeViewItem == null)

            {

                // 如果 AssociatedObject 不是模板的一部分，

                // 或者 TemplatedParent 不是 TreeViewItem，尝试向上查找

                treeViewItem = FindAncestor<TreeViewItem>(AssociatedObject);

            }



            if (treeViewItem == null) return;



            // 检查是否点击了 Expander，如果是则不处理，交由 ToggleButton 自身处理

            var originalSource = e.OriginalSource as DependencyObject;

            if (FindAncestor<ToggleButton>(originalSource) != null)

            {

                return;

            }



            // 只有当 TreeViewItem 有子项时才处理展开/折叠

            if (treeViewItem.HasItems)

            {

                treeViewItem.IsExpanded = !treeViewItem.IsExpanded;

                e.Handled = true;

            }

        }



        private T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject

        {

            while (current != null)

            {

                if (current is T ancestor)

                {

                    return ancestor;

                }

                current = VisualTreeHelper.GetParent(current);

            }

            return null;

        }

    }



    /// <summary>

    /// 支持从资源管理器拖入模板文件并触发 ViewModel 命令，拖入时显示遮罩提示。

    /// </summary>

    public class FileDropBehavior : Behavior<FrameworkElement>

    {

        public static readonly DependencyProperty DropCommandProperty =

            DependencyProperty.Register(nameof(DropCommand), typeof(ICommand), typeof(FileDropBehavior));



        public static readonly DependencyProperty AllowedExtensionsProperty =

            DependencyProperty.Register(

                nameof(AllowedExtensions),

                typeof(string),

                typeof(FileDropBehavior),

                new PropertyMetadata(".json,.gndiag,.zip"));



        public static readonly DependencyProperty HintTextProperty =

            DependencyProperty.Register(nameof(HintText), typeof(string), typeof(FileDropBehavior));



        public static readonly DependencyProperty SubHintTextProperty =

            DependencyProperty.Register(nameof(SubHintText), typeof(string), typeof(FileDropBehavior));



        public static readonly DependencyProperty InvalidHintTextProperty =

            DependencyProperty.Register(nameof(InvalidHintText), typeof(string), typeof(FileDropBehavior));



        public static readonly DependencyProperty OverlayIconProperty =

            DependencyProperty.Register(nameof(OverlayIcon), typeof(string), typeof(FileDropBehavior), new PropertyMetadata("\uE8B5"));



        private Border? _overlay;

        private TextBlock? _hintTextBlock;

        private TextBlock? _subHintTextBlock;

        private TextBlock? _iconTextBlock;

        private int _dragCounter;



        public ICommand? DropCommand

        {

            get => (ICommand?)GetValue(DropCommandProperty);

            set => SetValue(DropCommandProperty, value);

        }



        public string AllowedExtensions

        {

            get => (string)GetValue(AllowedExtensionsProperty);

            set => SetValue(AllowedExtensionsProperty, value);

        }



        public string? HintText

        {

            get => (string?)GetValue(HintTextProperty);

            set => SetValue(HintTextProperty, value);

        }



        public string? SubHintText

        {

            get => (string?)GetValue(SubHintTextProperty);

            set => SetValue(SubHintTextProperty, value);

        }



        public string? InvalidHintText

        {

            get => (string?)GetValue(InvalidHintTextProperty);

            set => SetValue(InvalidHintTextProperty, value);

        }



        public string OverlayIcon

        {

            get => (string)GetValue(OverlayIconProperty);

            set => SetValue(OverlayIconProperty, value);

        }



        protected override void OnAttached()

        {

            base.OnAttached();

            AssociatedObject.AllowDrop = true;

            AssociatedObject.PreviewDragEnter += OnPreviewDragEnter;

            AssociatedObject.PreviewDragOver += OnPreviewDragOver;

            AssociatedObject.PreviewDragLeave += OnPreviewDragLeave;

            AssociatedObject.PreviewDrop += OnPreviewDrop;

        }



        protected override void OnDetaching()

        {

            AssociatedObject.PreviewDragEnter -= OnPreviewDragEnter;

            AssociatedObject.PreviewDragOver -= OnPreviewDragOver;

            AssociatedObject.PreviewDragLeave -= OnPreviewDragLeave;

            AssociatedObject.PreviewDrop -= OnPreviewDrop;

            RemoveOverlay();

            base.OnDetaching();

        }



        private void OnPreviewDragEnter(object sender, DragEventArgs e)

        {

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))

                return;



            _dragCounter++;

            UpdateOverlay(e);

        }



        private void OnPreviewDragOver(object sender, DragEventArgs e)

        {

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))

                return;



            e.Effects = TryGetValidFiles(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;

            e.Handled = true;

            UpdateOverlay(e);

        }



        private void OnPreviewDragLeave(object sender, DragEventArgs e)

        {

            _dragCounter--;

            if (_dragCounter <= 0)

            {

                _dragCounter = 0;

                HideOverlay();

            }

        }



        private void OnPreviewDrop(object sender, DragEventArgs e)

        {

            _dragCounter = 0;

            HideOverlay();



            if (!TryGetValidFiles(e, out IReadOnlyList<string> filePaths))

                return;



            if (DropCommand?.CanExecute(filePaths) == true)

                DropCommand.Execute(filePaths);



            e.Handled = true;

        }



        private void UpdateOverlay(DragEventArgs e)

        {

            EnsureOverlay();

            if (_overlay == null)

                return;



            bool isValid = TryGetValidFiles(e, out _);

            if (_hintTextBlock != null)

                _hintTextBlock.Text = isValid ? HintText ?? string.Empty : InvalidHintText ?? HintText ?? string.Empty;



            if (_subHintTextBlock != null)

                _subHintTextBlock.Text = SubHintText ?? string.Empty;



            if (_iconTextBlock != null)

                _iconTextBlock.Text = OverlayIcon;



            _overlay.Visibility = Visibility.Visible;

        }



        private void HideOverlay()

        {

            if (_overlay != null)

                _overlay.Visibility = Visibility.Collapsed;

        }



        private void EnsureOverlay()

        {

            if (_overlay != null || AssociatedObject is not Panel panel)

                return;



            _iconTextBlock = new TextBlock();

            _iconTextBlock.SetResourceReference(FrameworkElement.StyleProperty, "FileDropOverlayIconStyle");



            _hintTextBlock = new TextBlock();

            _hintTextBlock.SetResourceReference(FrameworkElement.StyleProperty, "FileDropOverlayHintStyle");



            _subHintTextBlock = new TextBlock();

            _subHintTextBlock.SetResourceReference(FrameworkElement.StyleProperty, "FileDropOverlaySubHintStyle");



            var content = new StackPanel

            {

                HorizontalAlignment = HorizontalAlignment.Center,

                VerticalAlignment = VerticalAlignment.Center

            };

            content.Children.Add(_iconTextBlock);

            content.Children.Add(_hintTextBlock);

            content.Children.Add(_subHintTextBlock);



            var card = new Border { Child = content };

            card.SetResourceReference(FrameworkElement.StyleProperty, "FileDropOverlayCardStyle");



            _overlay = new Border { Child = card };

            _overlay.SetResourceReference(FrameworkElement.StyleProperty, "FileDropOverlayMaskStyle");

            Panel.SetZIndex(_overlay, 10000);

            panel.Children.Add(_overlay);

        }



        private void RemoveOverlay()

        {

            if (_overlay == null || AssociatedObject is not Panel panel)

                return;



            panel.Children.Remove(_overlay);

            _overlay = null;

            _hintTextBlock = null;

            _subHintTextBlock = null;

            _iconTextBlock = null;

        }



        private bool TryGetValidFiles(DragEventArgs e, out IReadOnlyList<string> filePaths)

        {

            filePaths = Array.Empty<string>();

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))

                return false;



            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)

                return false;



            var allowed = AllowedExtensions

                .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)

                .Select(ext => ext.StartsWith('.') ? ext : "." + ext)

                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);



            filePaths = files

                .Where(f =>

                    !string.IsNullOrWhiteSpace(f) &&

                    File.Exists(f) &&

                    allowed.Contains(Path.GetExtension(f)))

                .ToList();



            return filePaths.Count > 0;

        }

    }

}


