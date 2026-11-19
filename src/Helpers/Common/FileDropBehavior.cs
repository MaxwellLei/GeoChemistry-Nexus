using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Services;
using Microsoft.Xaml.Behaviors;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 从资源管理器拖入文件时盖上导入卡片，松开后把可导入的路径交给 ViewModel 命令。
    /// 后缀按完整文件名匹配，因此 .diag.zip 不会被当成裸 .zip。
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
                new PropertyMetadata(TemplatePackageFileExtensions.DiagramDropAllowed));

        public static readonly DependencyProperty HintTextProperty =
            DependencyProperty.Register(nameof(HintText), typeof(string), typeof(FileDropBehavior));

        public static readonly DependencyProperty SubHintTextProperty =
            DependencyProperty.Register(nameof(SubHintText), typeof(string), typeof(FileDropBehavior));

        public static readonly DependencyProperty InvalidHintTextProperty =
            DependencyProperty.Register(nameof(InvalidHintText), typeof(string), typeof(FileDropBehavior));

        public static readonly DependencyProperty InvalidSubHintTextProperty =
            DependencyProperty.Register(nameof(InvalidSubHintText), typeof(string), typeof(FileDropBehavior));

        public static readonly DependencyProperty KickerTextProperty =
            DependencyProperty.Register(nameof(KickerText), typeof(string), typeof(FileDropBehavior));

        public static readonly DependencyProperty OverlayFormatsProperty =
            DependencyProperty.Register(nameof(OverlayFormats), typeof(string), typeof(FileDropBehavior));

        public static readonly DependencyProperty RecognizedCountFormatProperty =
            DependencyProperty.Register(nameof(RecognizedCountFormat), typeof(string), typeof(FileDropBehavior));

        private FileDropOverlayView? _overlay;
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

        public string? InvalidSubHintText
        {
            get => (string?)GetValue(InvalidSubHintTextProperty);
            set => SetValue(InvalidSubHintTextProperty, value);
        }

        public string? KickerText
        {
            get => (string?)GetValue(KickerTextProperty);
            set => SetValue(KickerTextProperty, value);
        }

        /// <summary>卡片上展示的后缀。为空时改用 <see cref="AllowedExtensions"/>。</summary>
        public string? OverlayFormats
        {
            get => (string?)GetValue(OverlayFormatsProperty);
            set => SetValue(OverlayFormatsProperty, value);
        }

        public string? RecognizedCountFormat
        {
            get => (string?)GetValue(RecognizedCountFormatProperty);
            set => SetValue(RecognizedCountFormatProperty, value);
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

            bool hasValid = TryGetValidFiles(e, out IReadOnlyList<string> filePaths);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                NotifyRejectedFiles(e, filePaths);

            if (!hasValid)
            {
                e.Handled = true;
                return;
            }

            if (DropCommand?.CanExecute(filePaths) == true)
                DropCommand.Execute(filePaths);

            e.Handled = true;
        }

        /// <summary>把被后缀过滤掉的文件（含目录）逐个提示出来，避免静默忽略。</summary>
        private static void NotifyRejectedFiles(DragEventArgs e, IReadOnlyList<string> acceptedPaths)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
                return;

            var accepted = new HashSet<string>(acceptedPaths, StringComparer.OrdinalIgnoreCase);
            var rejectedNames = files
                .Where(path => !string.IsNullOrWhiteSpace(path) && !accepted.Contains(path))
                .Select(path => Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
                .ToList();

            if (rejectedNames.Count == 0)
                return;

            string names = rejectedNames.Count <= 3
                ? string.Join(", ", rejectedNames)
                : string.Join(", ", rejectedNames.Take(3)) + $" (+{rejectedNames.Count - 3})";

            MessageHelper.Error($"{names}: {LanguageService.Instance["file_drop_invalid_format"]}");
        }

        private void UpdateOverlay(DragEventArgs e)
        {
            EnsureOverlay();
            if (_overlay == null)
                return;

            bool accepted = TryGetValidFiles(e, out IReadOnlyList<string> files);
            string formatSource = string.IsNullOrWhiteSpace(OverlayFormats) ? AllowedExtensions : OverlayFormats;
            _overlay.Show(new FileDropOverlayContent
            {
                Accepted = accepted,
                Kicker = KickerText,
                Title = accepted ? HintText : InvalidHintText ?? HintText,
                Subtitle = accepted ? SubHintText : InvalidSubHintText ?? SubHintText,
                Formats = TemplatePackageFileExtensions.SplitSuffixList(formatSource),
                CountFormat = RecognizedCountFormat,
                FileCount = files.Count
            });
        }

        private void HideOverlay()
        {
            _overlay?.Hide();
        }

        private void EnsureOverlay()
        {
            if (_overlay != null || AssociatedObject is not Panel panel)
                return;

            _overlay = new FileDropOverlayView
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false
            };
            Panel.SetZIndex(_overlay, 10000);

            if (panel is Grid grid)
            {
                Grid.SetColumn(_overlay, 0);
                Grid.SetRow(_overlay, 0);
                Grid.SetColumnSpan(_overlay, Math.Max(1, grid.ColumnDefinitions.Count));
                Grid.SetRowSpan(_overlay, Math.Max(1, grid.RowDefinitions.Count));
            }

            panel.Children.Add(_overlay);
        }

        private void RemoveOverlay()
        {
            if (_overlay == null || AssociatedObject is not Panel panel)
                return;

            panel.Children.Remove(_overlay);
            _overlay = null;
        }

        private bool TryGetValidFiles(DragEventArgs e, out IReadOnlyList<string> filePaths)
        {
            filePaths = Array.Empty<string>();
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return false;

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
                return false;

            IReadOnlyList<string> allowed = TemplatePackageFileExtensions.SplitSuffixList(AllowedExtensions);
            filePaths = files
                .Where(path => !string.IsNullOrWhiteSpace(path)
                    && File.Exists(path)
                    && TemplatePackageFileExtensions.MatchesAnySuffix(path, allowed))
                .ToList();

            return filePaths.Count > 0;
        }
    }
}
