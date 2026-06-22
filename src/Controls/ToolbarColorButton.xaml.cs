using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    public partial class ToolbarColorButton : UserControl
    {
        private bool _isUpdating;
        private long _lastClosedTicks;

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(Color),
                typeof(ToolbarColorButton),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        public static readonly DependencyProperty IconTextProperty =
            DependencyProperty.Register(
                nameof(IconText),
                typeof(string),
                typeof(ToolbarColorButton),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconFontFamilyProperty =
            DependencyProperty.Register(
                nameof(IconFontFamily),
                typeof(FontFamily),
                typeof(ToolbarColorButton),
                new PropertyMetadata(new FontFamily("Segoe UI")));

        public static readonly DependencyProperty IndicatorBrushProperty =
            DependencyProperty.Register(
                nameof(IndicatorBrush),
                typeof(Brush),
                typeof(ToolbarColorButton),
                new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty SelectedBrushProperty =
            DependencyProperty.Register(
                nameof(SelectedBrush),
                typeof(SolidColorBrush),
                typeof(ToolbarColorButton),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedBrushChanged));

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public string IconText
        {
            get => (string)GetValue(IconTextProperty);
            set => SetValue(IconTextProperty, value);
        }

        public FontFamily IconFontFamily
        {
            get => (FontFamily)GetValue(IconFontFamilyProperty);
            set => SetValue(IconFontFamilyProperty, value);
        }

        public Brush IndicatorBrush
        {
            get => (Brush)GetValue(IndicatorBrushProperty);
            private set => SetValue(IndicatorBrushProperty, value);
        }

        public SolidColorBrush SelectedBrush
        {
            get => (SolidColorBrush)GetValue(SelectedBrushProperty);
            set => SetValue(SelectedBrushProperty, value);
        }

        public ICommand ShowColorPickerCommand { get; }

        public ToolbarColorButton()
        {
            InitializeComponent();
            ShowColorPickerCommand = new RelayCommand(ShowColorPicker);
            ColorPopup.Closed += (_, _) => _lastClosedTicks = DateTime.Now.Ticks;
            UpdateBrushesFromColor(SelectedColor);
        }

        private void ShowColorPicker()
        {
            if (DateTime.Now.Ticks - _lastClosedTicks < 200 * 10000)
                return;

            ColorPopup.IsOpen = true;
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ToolbarColorButton control || control._isUpdating)
                return;

            control._isUpdating = true;
            try
            {
                var color = NormalizeColor((Color)e.NewValue);
                control.SelectedBrush = new SolidColorBrush(color);
                control.UpdateBrushesFromColor(color);
            }
            finally
            {
                control._isUpdating = false;
            }
        }

        private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ToolbarColorButton control || control._isUpdating)
                return;

            if (e.NewValue is not SolidColorBrush brush)
                return;

            control._isUpdating = true;
            try
            {
                var color = NormalizeColor(brush.Color);
                control.SelectedColor = color;
                control.UpdateBrushesFromColor(color);
            }
            finally
            {
                control._isUpdating = false;
            }
        }

        private void UpdateBrushesFromColor(Color color)
        {
            var normalized = NormalizeColor(color);
            IndicatorBrush = new SolidColorBrush(normalized);
        }

        private static Color NormalizeColor(Color color)
        {
            return Color.FromArgb(255, color.R, color.G, color.B);
        }
    }
}
