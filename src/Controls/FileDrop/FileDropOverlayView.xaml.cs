using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    public sealed class FileDropOverlayContent
    {
        public bool Accepted { get; init; }
        public string? Kicker { get; init; }
        public string? Title { get; init; }
        public string? Subtitle { get; init; }
        public IReadOnlyList<string> Formats { get; init; } = Array.Empty<string>();
        public string? CountFormat { get; init; }
        public int FileCount { get; init; }
    }

    /// <summary>
    /// 文件拖放时盖住页面的蒙层和中间卡片。只负责呈现，不判断文件是否可导入。
    /// </summary>
    public partial class FileDropOverlayView : UserControl
    {
        private static readonly SolidColorBrush DangerBrush = CreateDangerBrush();
        private string? _signature;

        public FileDropOverlayView()
        {
            InitializeComponent();
        }

        public void Show(FileDropOverlayContent content)
        {
            string signature = string.Join("\u001f",
                content.Accepted,
                content.Kicker,
                content.Title,
                content.Subtitle,
                content.CountFormat,
                content.FileCount,
                string.Join(",", content.Formats));

            if (!string.Equals(signature, _signature, StringComparison.Ordinal))
            {
                Apply(content);
                _signature = signature;
            }

            Root.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            Root.Visibility = Visibility.Collapsed;
            _signature = null;
        }

        private void Apply(FileDropOverlayContent content)
        {
            KickerText.Text = content.Kicker ?? string.Empty;
            TitleText.Text = content.Title ?? string.Empty;
            SubText.Text = content.Subtitle ?? string.Empty;

            if (content.Accepted)
            {
                TitleText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeAccentBrush");
                Card.SetResourceReference(Border.BorderBrushProperty, "ThemeAccentBrush");
                AcceptedArt.Opacity = 1;
                RejectedArt.Opacity = 0;
            }
            else
            {
                TitleText.Foreground = DangerBrush;
                Card.BorderBrush = DangerBrush;
                AcceptedArt.Opacity = 0;
                RejectedArt.Opacity = 1;
            }

            Chips.Children.Clear();
            foreach (string format in content.Formats)
            {
                var text = new TextBlock { Text = format };
                text.SetResourceReference(FrameworkElement.StyleProperty, "FileDropOverlayChipTextStyle");
                var chip = new Border { Child = text };
                chip.SetResourceReference(FrameworkElement.StyleProperty, "FileDropOverlayChipStyle");
                Chips.Children.Add(chip);
            }

            if (content.Accepted && content.FileCount > 0)
            {
                string format = string.IsNullOrWhiteSpace(content.CountFormat) ? "{0}" : content.CountFormat;
                try
                {
                    CountText.Text = string.Format(CultureInfo.CurrentCulture, format, content.FileCount);
                }
                catch (FormatException)
                {
                    CountText.Text = content.FileCount.ToString(CultureInfo.CurrentCulture);
                }

                CountText.Visibility = Visibility.Visible;
            }
            else
            {
                CountText.Visibility = Visibility.Collapsed;
            }
        }

        private static SolidColorBrush CreateDangerBrush()
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
            brush.Freeze();
            return brush;
        }
    }
}
