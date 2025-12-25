using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    public partial class TextPropertyColorPicker : UserControl
    {
        private bool _isUpdating;

        public static readonly DependencyProperty SelectedBrushProperty =
            DependencyProperty.Register("SelectedBrush", typeof(SolidColorBrush), typeof(TextPropertyColorPicker),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedBrushChanged));

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(TextPropertyColorPicker),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        public static readonly DependencyProperty HexColorProperty =
            DependencyProperty.Register("HexColor", typeof(string), typeof(TextPropertyColorPicker),
                new FrameworkPropertyMetadata("#000000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHexColorChanged));

        public SolidColorBrush SelectedBrush
        {
            get { return (SolidColorBrush)GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public string HexColor
        {
            get { return (string)GetValue(HexColorProperty); }
            set { SetValue(HexColorProperty, value); }
        }

        public ICommand ShowColorPickerCommand { get; }

        private long _lastClosedTicks;

        public TextPropertyColorPicker()
        {
            InitializeComponent();
            ShowColorPickerCommand = new RelayCommand(ShowColorPicker);
            ColorPopup.Closed += (s, e) => _lastClosedTicks = DateTime.Now.Ticks;
        }

        private void ShowColorPicker()
        {
            if (DateTime.Now.Ticks - _lastClosedTicks < 200 * 10000) return;  // 200ºÁÃë·À¶¶
            ColorPopup.IsOpen = true;
        }

        private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextPropertyColorPicker)d;
            if (control._isUpdating) return;

            if (e.NewValue is SolidColorBrush newBrush)
            {
                control._isUpdating = true;
                control.SelectedColor = newBrush.Color;
                control.HexColor = newBrush.Color.ToString();
                control._isUpdating = false;
            }
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextPropertyColorPicker)d;
            if (control._isUpdating) return;

            var newColor = (Color)e.NewValue;
            control._isUpdating = true;
            control.SelectedBrush = new SolidColorBrush(newColor);
            control.HexColor = newColor.ToString();
            control._isUpdating = false;
        }

        private static void OnHexColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextPropertyColorPicker)d;
            if (control._isUpdating) return;

            if (e.NewValue is string hex)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    control._isUpdating = true;
                    control.SelectedColor = color;
                    control.SelectedBrush = new SolidColorBrush(color);
                    control._isUpdating = false;
                }
                catch
                {
                    // Ignore invalid hex
                }
            }
        }
    }
}
