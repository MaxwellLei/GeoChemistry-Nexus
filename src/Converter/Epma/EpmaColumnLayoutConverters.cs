using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace GeoChemistryNexus.Converter;

// Column names and sample values share one width, measured with the actual UI font.
public sealed class EpmaColumnWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[2] is not FrameworkElement element) return 40d;
        double Measure(object value)
        {
            var text = new FormattedText(value as string ?? "", culture, element.FlowDirection,
                new Typeface(TextElement.GetFontFamily(element), TextElement.GetFontStyle(element),
                    TextElement.GetFontWeight(element), TextElement.GetFontStretch(element)),
                TextElement.GetFontSize(element), Brushes.Black, VisualTreeHelper.GetDpi(element).PixelsPerDip);
            return text.WidthIncludingTrailingWhitespace;
        }
        // 10 px cell padding, 12 px reserved for the column's remove button, and a caret allowance.
        double sampleWidth = values[1] is IEnumerable<string> examples
            ? examples.Select(value => Measure(value)).DefaultIfEmpty(0).Max() : Measure(values[1]);
        double dpi = VisualTreeHelper.GetDpi(element).DpiScaleX;
        double minimum = Math.Ceiling(Math.Max(80, Math.Max(Measure(values[0]) + 26, sampleWidth + 14)) * dpi) / dpi;
        double share = 0;
        if (values.Length >= 6 && values[3] is double viewport && values[4] is int count && count > 0 && values[5] is int index)
        {
            // Allocate every remaining pixel, including the division remainder, so the last cell meets the table edge.
            int pixels = (int)Math.Round(viewport * dpi);
            share = (pixels / count + (index < pixels % count ? 1 : 0)) / dpi;
        }
        return Math.Max(minimum, share);
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class EpmaColumnDividerConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        new Thickness(0, 0, values.Length >= 2 && values[0] is int index && values[1] is int count && index == count - 1 ? 0 : 1, 0);
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class EpmaLanguageNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        GeoChemistryNexus.ViewModels.EpmaTemplateEditorViewModel.LanguageName(value as string ?? "");
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class EpmaBasicPageVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

// Keep the plus button attached to the last column until the table needs horizontal scrolling.
public sealed class EpmaColumnTableWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[0] is not double content || values[1] is not double available || available <= 0)
            return DependencyProperty.UnsetValue;
        bool readOnly = values[2] is true;
        return Math.Min(available, content + 96 + (readOnly ? 0 : 16) + 2);
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

// Keep a useful editor viewport while sample rows expand the scrollable page.
public sealed class EpmaScriptEditorHeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length >= 2 && values[0] is double viewport && values[1] is double columns
            ? Math.Max(520, viewport - columns - 12 - (values.Length >= 3 && values[2] is double title ? title : 0)) : 520d;
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
