using GeoChemistryNexus.Helpers;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class FontSizeControl : UserControl
    {
        private bool _isUpdatingFromSource;

        public static readonly DependencyProperty FontSizeValueProperty =
            DependencyProperty.Register(
                nameof(FontSizeValue),
                typeof(float),
                typeof(FontSizeControl),
                new FrameworkPropertyMetadata(10f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFontSizeValueChanged));

        public float FontSizeValue
        {
            get => (float)GetValue(FontSizeValueProperty);
            set => SetValue(FontSizeValueProperty, value);
        }

        public FontSizeControl()
        {
            InitializeComponent();
            FontSizeComboBox.ItemsSource = ReoGridFormatHelper.CommonFontSizes
                .Select(size => size.ToString("0.##", CultureInfo.CurrentCulture))
                .ToArray();
            Loaded += (_, _) => UpdateComboFromValue(FontSizeValue);
        }

        private static void OnFontSizeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FontSizeControl control && control.IsLoaded)
                control.UpdateComboFromValue((float)e.NewValue);
        }

        private void UpdateComboFromValue(float value)
        {
            if (value <= 0)
                return;

            _isUpdatingFromSource = true;
            try
            {
                string text = value.ToString("0.##", CultureInfo.CurrentCulture);
                FontSizeComboBox.Text = text;

                var match = ReoGridFormatHelper.CommonFontSizes.FirstOrDefault(size => Math.Abs(size - value) < 0.001f);
                if (match > 0)
                    FontSizeComboBox.SelectedItem = match.ToString("0.##", CultureInfo.CurrentCulture);
                else
                    FontSizeComboBox.SelectedItem = null;
            }
            finally
            {
                _isUpdatingFromSource = false;
            }
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFromSource || FontSizeComboBox.SelectedItem is not string selectedText)
                return;

            TryApplyFontSizeText(selectedText);
        }

        private void FontSizeComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFromSource)
                return;

            TryApplyFontSizeText(FontSizeComboBox.Text);
        }

        private void TryApplyFontSizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out float size) || size <= 0)
                return;

            if (Math.Abs(FontSizeValue - size) > 0.001f)
                FontSizeValue = size;
        }
    }
}
