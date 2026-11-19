using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        /// <summary>是否显示十六进制输入框；为 false 时仅显示色块（用于紧凑的字体工具行）。</summary>
        public static readonly DependencyProperty ShowHexTextProperty =
            DependencyProperty.Register(nameof(ShowHexText), typeof(bool), typeof(TextPropertyColorPicker),
                new PropertyMetadata(true));

        public bool ShowHexText
        {
            get { return (bool)GetValue(ShowHexTextProperty); }
            set { SetValue(ShowHexTextProperty, value); }
        }

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

        /// <summary>弹层顶部的常用调色板（8 列 × 3 行）。</summary>
        public static IReadOnlyList<string> PaletteColors { get; } = new[]
        {
            "#000000", "#4B5563", "#9CA3AF", "#D1D5DB", "#F3F4F6", "#FFFFFF", "#EF4444", "#F97316",
            "#F59E0B", "#EAB308", "#84CC16", "#22C55E", "#14B8A6", "#06B6D4", "#3B82F6", "#6366F1",
            "#8B5CF6", "#A855F7", "#EC4899", "#F43F5E", "#7C2D12", "#1E3A8A", "#064E3B", "#831843"
        };

        public ICommand ShowColorPickerCommand { get; }

        /// <summary>选择调色板中的一个颜色（参数为十六进制字符串）。</summary>
        public ICommand SelectPaletteColorCommand { get; }

        private long _lastClosedTicks;

        public TextPropertyColorPicker()
        {
            InitializeComponent();
            ShowColorPickerCommand = new RelayCommand(ShowColorPicker);
            SelectPaletteColorCommand = new RelayCommand<string>(SelectPaletteColor);
            ColorPopup.Closed += (s, e) =>
            {
                _lastClosedTicks = DateTime.Now.Ticks;
                MoreColorsToggle.IsChecked = false;
            };
            Loaded += (_, _) => SetPopupChannelText(SelectedColor);
        }

        private void ShowColorPicker()
        {
            if (DateTime.Now.Ticks - _lastClosedTicks < 200 * 10000) return;  // 200 毫秒内忽略重复点击
            ColorPopup.IsOpen = true;
        }

        /// <summary>从屏幕上点一个像素。保留当前透明度，只替换红绿蓝。</summary>
        private void EyedropperButton_Click(object sender, RoutedEventArgs e)
        {
            var staysOpen = ColorPopup.StaysOpen;
            var wasOpen = ColorPopup.IsOpen;
            var moreColors = MoreColorsToggle.IsChecked == true;
            ColorPopup.StaysOpen = true;
            var previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Cross;
            try
            {
                var dropper = new ScreenColorDropperWindow { Owner = Window.GetWindow(this) };
                if (dropper.ShowDialog() == true && dropper.Picked is Color picked)
                    ApplyColor(Color.FromArgb(SelectedColor.A, picked.R, picked.G, picked.B));
            }
            finally
            {
                Mouse.OverrideCursor = previousCursor;
                ColorPopup.StaysOpen = staysOpen;
                if (wasOpen)
                {
                    ColorPopup.IsOpen = true;
                    MoreColorsToggle.IsChecked = moreColors;
                }
            }
        }

        private void SelectPaletteColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return;
            HexColor = hex;
        }

        /// <summary>回车立刻提交十六进制，不必等焦点离开。</summary>
        private void PopupHexBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            CommitPopupHex();
            e.Handled = true;
        }

        /// <summary>离开输入框时提交。3/4 位缩写在这里展开；写到一半的内容会退回当前颜色。</summary>
        private void PopupHexBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CommitPopupHex();
        }

        private void CommitPopupHex()
        {
            if (_isUpdating || PopupHexBox == null) return;

            if (TryParseHex(PopupHexBox.Text, out var color, allowShort: true))
            {
                ApplyColor(color);
                return;
            }

            _isUpdating = true;
            HexColor = ColorToHexRgb(SelectedColor);
            _isUpdating = false;
        }

        private void PopupChannel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            CommitPopupChannel(sender as TextBox);
            e.Handled = true;
        }

        private void PopupChannel_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CommitPopupChannel(sender as TextBox);
        }

        private void CommitPopupChannel(TextBox? box)
        {
            if (_isUpdating || box == null) return;
            if (!byte.TryParse(box.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                if (box == PopupRBox) WriteChannelBox(box, SelectedColor.R);
                else if (box == PopupGBox) WriteChannelBox(box, SelectedColor.G);
                else if (box == PopupBBox) WriteChannelBox(box, SelectedColor.B);
                else if (box == PopupABox) WriteChannelBox(box, SelectedColor.A);
                return;
            }

            var color = SelectedColor;
            if (box == PopupRBox) color.R = value;
            else if (box == PopupGBox) color.G = value;
            else if (box == PopupBBox) color.B = value;
            else if (box == PopupABox) color.A = value;
            else return;

            ApplyColor(color);
            WriteChannelBox(box, value);
        }

        private void ApplyColor(Color color)
        {
            _isUpdating = true;
            SelectedColor = color;
            SelectedBrush = new SolidColorBrush(color);
            HexColor = ColorToHexRgb(color);
            _isUpdating = false;
            SetPopupChannelText(color);
        }

        private void SetPopupChannelText(Color color)
        {
            SetChannelBoxText(PopupRBox, color.R);
            SetChannelBoxText(PopupGBox, color.G);
            SetChannelBoxText(PopupBBox, color.B);
            SetChannelBoxText(PopupABox, color.A);
        }

        private static void SetChannelBoxText(TextBox? box, byte value)
        {
            if (box == null || box.IsKeyboardFocused) return;
            WriteChannelBox(box, value);
        }

        private static void WriteChannelBox(TextBox box, byte value)
        {
            var text = value.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(box.Text, text, StringComparison.Ordinal))
                box.Text = text;
        }

        private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextPropertyColorPicker)d;
            if (control._isUpdating) return;

            if (e.NewValue is SolidColorBrush newBrush)
            {
                control._isUpdating = true;
                control.SelectedColor = newBrush.Color;
                control.HexColor = ColorToHexRgb(newBrush.Color);
                control._isUpdating = false;
                control.SetPopupChannelText(newBrush.Color);
            }
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextPropertyColorPicker)d;
            if (control._isUpdating) return;

            var newColor = (Color)e.NewValue;
            control._isUpdating = true;
            control.SelectedBrush = new SolidColorBrush(newColor);
            control.HexColor = ColorToHexRgb(newColor);
            control._isUpdating = false;
            control.SetPopupChannelText(newColor);
        }

        private static void OnHexColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextPropertyColorPicker)d;
            if (control._isUpdating) return;

            if (e.NewValue is string hex)
            {
                if (string.IsNullOrWhiteSpace(hex))
                {
                    control._isUpdating = true;
                    control.SelectedColor = Colors.Transparent;
                    control.SelectedBrush = new SolidColorBrush(Colors.Transparent);
                    control._isUpdating = false;
                    return;
                }

                if (!TryParseHex(hex, out var color, allowShort: false)) return;

                control._isUpdating = true;
                control.SelectedColor = color;
                control.SelectedBrush = new SolidColorBrush(color);
                control._isUpdating = false;
                control.SetPopupChannelText(color);
            }
        }

        /// <summary>不透明时用 #RRGGBB，带透明时用 #AARRGGBB。也接受不带 # 的 3/4/6/8 位十六进制。</summary>
        private static string ColorToHexRgb(Color color)
        {
            if (color.A == 255)
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static bool TryParseHex(string text, out Color color, bool allowShort)
        {
            color = default;
            var body = text.Trim();
            if (body.StartsWith("#", StringComparison.Ordinal)) body = body.Substring(1);
            var lengthOk = body.Length is 6 or 8 || (allowShort && body.Length is 3 or 4);
            if (!lengthOk) return false;
            foreach (var ch in body)
            {
                if (!Uri.IsHexDigit(ch)) return false;
            }

            try
            {
                color = (Color)ColorConverter.ConvertFromString("#" + body);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }

    /// <summary>调色板色块和当前十六进制是否同一颜色。</summary>
    public class HexMatchConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            return string.Equals(Normalize(values[0]), Normalize(values[1]), StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static string Normalize(object value)
        {
            var text = value as string ?? string.Empty;
            text = text.Trim();
            if (text.StartsWith("#", StringComparison.Ordinal)) text = text.Substring(1);
            return text;
        }
    }
}
