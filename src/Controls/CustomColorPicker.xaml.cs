using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    public partial class CustomColorPicker : UserControl
    {
        private bool _isUpdating;
        private double _hue = 0;
        private double _saturation = 1;
        private double _value = 1;
        private double _alpha = 1;

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(CustomColorPicker),
                new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        public static readonly DependencyProperty SelectedBrushProperty =
            DependencyProperty.Register("SelectedBrush", typeof(SolidColorBrush), typeof(CustomColorPicker),
                new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty AProperty =
            DependencyProperty.Register("A", typeof(byte), typeof(CustomColorPicker),
                new FrameworkPropertyMetadata((byte)255, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnARGBChanged));

        public static readonly DependencyProperty RProperty =
            DependencyProperty.Register("R", typeof(byte), typeof(CustomColorPicker),
                new FrameworkPropertyMetadata((byte)255, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnARGBChanged));

        public static readonly DependencyProperty GProperty =
            DependencyProperty.Register("G", typeof(byte), typeof(CustomColorPicker),
                new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnARGBChanged));

        public static readonly DependencyProperty BProperty =
            DependencyProperty.Register("B", typeof(byte), typeof(CustomColorPicker),
                new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnARGBChanged));

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public SolidColorBrush SelectedBrush
        {
            get { return (SolidColorBrush)GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        public byte A
        {
            get { return (byte)GetValue(AProperty); }
            set { SetValue(AProperty, value); }
        }

        public byte R
        {
            get { return (byte)GetValue(RProperty); }
            set { SetValue(RProperty, value); }
        }

        public byte G
        {
            get { return (byte)GetValue(GProperty); }
            set { SetValue(GProperty, value); }
        }

        public byte B
        {
            get { return (byte)GetValue(BProperty); }
            set { SetValue(BProperty, value); }
        }

        public CustomColorPicker()
        {
            InitializeComponent();
            Loaded += CustomColorPicker_Loaded;
        }

        private void CustomColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateThumbsFromColor(SelectedColor);
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (CustomColorPicker)d;
            if (picker._isUpdating) return;

            var newColor = (Color)e.NewValue;
            picker._isUpdating = true;
            picker.SelectedBrush = new SolidColorBrush(newColor);
            picker.A = newColor.A;
            picker.R = newColor.R;
            picker.G = newColor.G;
            picker.B = newColor.B;
            picker.UpdateThumbsFromColor(newColor);
            picker._isUpdating = false;
        }

        private static void OnARGBChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (CustomColorPicker)d;
            if (picker._isUpdating) return;

            picker._isUpdating = true;
            var color = Color.FromArgb(picker.A, picker.R, picker.G, picker.B);
            picker.SelectedColor = color;
            picker.SelectedBrush = new SolidColorBrush(color);
            picker.UpdateThumbsFromColor(color);
            picker._isUpdating = false;
        }

        private void UpdateThumbsFromColor(Color color)
        {
            // Convert RGB to HSV
            ColorToHSV(color, out _hue, out _saturation, out _value);
            _alpha = color.A / 255.0;

            // Update Hue Thumb
            if (HueCanvas.ActualWidth > 0)
            {
                Canvas.SetLeft(HueThumb, (_hue / 360.0) * HueCanvas.ActualWidth);
                HueMonitor.Background = new SolidColorBrush(HSVToColor(_hue, 1, 1));
            }

            // Update SV Thumb
            if (SvCanvas.ActualWidth > 0 && SvCanvas.ActualHeight > 0)
            {
                Canvas.SetLeft(SvThumb, _saturation * SvCanvas.ActualWidth - (SvThumb.ActualWidth / 2));
                Canvas.SetTop(SvThumb, (1 - _value) * SvCanvas.ActualHeight - (SvThumb.ActualHeight / 2));
            }

            // Update Alpha Thumb
            if (AlphaCanvas.ActualWidth > 0)
            {
                Canvas.SetLeft(AlphaThumb, _alpha * AlphaCanvas.ActualWidth);
                AlphaGradientStop.Color = Color.FromRgb(color.R, color.G, color.B);
            }
        }

        private void UpdateColorFromHSV()
        {
            if (_isUpdating) return;

            _isUpdating = true;
            var color = HSVToColor(_hue, _saturation, _value);
            // Apply Alpha
            color = Color.FromArgb((byte)(_alpha * 255), color.R, color.G, color.B);
            
            SelectedColor = color;
            SelectedBrush = new SolidColorBrush(color);
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
            
            HueMonitor.Background = new SolidColorBrush(HSVToColor(_hue, 1, 1));
            AlphaGradientStop.Color = Color.FromRgb(color.R, color.G, color.B);
            
            _isUpdating = false;
        }

        // Hue Slider Interaction
        private void HueThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            MoveHueThumb(e.HorizontalChange);
        }

        private void HueCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(HueCanvas);
            UpdateHue(pos.X);
            HueThumb.CaptureMouse();
        }

        private void HueCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            HueThumb.ReleaseMouseCapture();
        }

        private void HueCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (HueThumb.IsMouseCaptured)
            {
                var pos = e.GetPosition(HueCanvas);
                UpdateHue(pos.X);
            }
        }

        private void MoveHueThumb(double offset)
        {
            double currentLeft = Canvas.GetLeft(HueThumb);
            UpdateHue(currentLeft + offset);
        }

        private void UpdateHue(double x)
        {
            double width = HueCanvas.ActualWidth;
            if (width == 0) return;

            x = Math.Max(0, Math.Min(width, x));
            Canvas.SetLeft(HueThumb, x);

            _hue = (x / width) * 360.0;
            UpdateColorFromHSV();
        }

        // Alpha Slider Interaction
        private void AlphaThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            MoveAlphaThumb(e.HorizontalChange);
        }

        private void AlphaCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(AlphaCanvas);
            UpdateAlpha(pos.X);
            AlphaThumb.CaptureMouse();
        }

        private void AlphaCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AlphaThumb.ReleaseMouseCapture();
        }

        private void AlphaCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (AlphaThumb.IsMouseCaptured)
            {
                var pos = e.GetPosition(AlphaCanvas);
                UpdateAlpha(pos.X);
            }
        }

        private void MoveAlphaThumb(double offset)
        {
            double currentLeft = Canvas.GetLeft(AlphaThumb);
            UpdateAlpha(currentLeft + offset);
        }

        private void UpdateAlpha(double x)
        {
            double width = AlphaCanvas.ActualWidth;
            if (width == 0) return;

            x = Math.Max(0, Math.Min(width, x));
            Canvas.SetLeft(AlphaThumb, x);

            _alpha = x / width;
            UpdateColorFromHSV();
        }

        // SV Canvas Interaction
        private void SvThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            MoveSvThumb(e.HorizontalChange, e.VerticalChange);
        }

        private void SvCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(SvCanvas);
            UpdateSv(pos.X, pos.Y);
            SvThumb.CaptureMouse();
        }

        private void SvCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SvThumb.ReleaseMouseCapture();
        }

        private void SvCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (SvThumb.IsMouseCaptured)
            {
                var pos = e.GetPosition(SvCanvas);
                UpdateSv(pos.X, pos.Y);
            }
        }

        private void MoveSvThumb(double xOffset, double yOffset)
        {
            double currentLeft = Canvas.GetLeft(SvThumb);
            double currentTop = Canvas.GetTop(SvThumb);
            UpdateSv(currentLeft + SvThumb.ActualWidth/2 + xOffset, currentTop + SvThumb.ActualHeight/2 + yOffset);
        }

        private void UpdateSv(double x, double y)
        {
            double width = SvCanvas.ActualWidth;
            double height = SvCanvas.ActualHeight;
            if (width == 0 || height == 0) return;

            x = Math.Max(0, Math.Min(width, x));
            y = Math.Max(0, Math.Min(height, y));

            Canvas.SetLeft(SvThumb, x - SvThumb.ActualWidth / 2);
            Canvas.SetTop(SvThumb, y - SvThumb.ActualHeight / 2);

            _saturation = x / width;
            _value = 1 - (y / height);
            UpdateColorFromHSV();
        }

        private void QuickColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                try
                {
                    SelectedColor = (Color)ColorConverter.ConvertFromString(hex);
                }
                catch { }
            }
        }

        // Helper Methods
        private Color HSVToColor(double h, double s, double v)
        {
            double r = 0, g = 0, b = 0;

            if (s == 0)
            {
                r = v; g = v; b = v;
            }
            else
            {
                int i;
                double f, p, q, t;

                if (h == 360) h = 0;
                else h = h / 60;

                i = (int)Math.Truncate(h);
                f = h - i;

                p = v * (1.0 - s);
                q = v * (1.0 - (s * f));
                t = v * (1.0 - (s * (1.0 - f)));

                switch (i)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    default: r = v; g = p; b = q; break;
                }
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private void ColorToHSV(Color color, out double h, out double s, out double v)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double min = Math.Min(r, Math.Min(g, b));
            double max = Math.Max(r, Math.Max(g, b));
            double delta = max - min;

            v = max;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = delta / max;

                double del_R = (((max - r) / 6.0) + (delta / 2.0)) / delta;
                double del_G = (((max - g) / 6.0) + (delta / 2.0)) / delta;
                double del_B = (((max - b) / 6.0) + (delta / 2.0)) / delta;

                if (r == max) h = del_B - del_G;
                else if (g == max) h = (1.0 / 3.0) + del_R - del_B;
                else h = (2.0 / 3.0) + del_G - del_R;

                if (h < 0) h += 1;
                if (h > 1) h -= 1;

                h *= 360;
            }
        }
    }
}
