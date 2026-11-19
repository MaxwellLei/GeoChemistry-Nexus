using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 数值调节控件：文本输入 + 上下调节按钮。
    /// 外观由 Themes/Styles.xaml 中的 AppNumericUpDownStyle 提供（主题色随 AppThemeHelper 切换），
    /// 占位提示、标题与圆角通过 <see cref="ControlAssist"/> 的附加属性设置。
    /// </summary>
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartUpButton, Type = typeof(RepeatButton))]
    [TemplatePart(Name = PartDownButton, Type = typeof(RepeatButton))]
    public class NumericUpDown : Control
    {
        public const string PartTextBox = "PART_TextBox";
        public const string PartUpButton = "PART_UpButton";
        public const string PartDownButton = "PART_DownButton";

        private TextBox? _textBox;
        private RepeatButton? _upButton;
        private RepeatButton? _downButton;

        /// <summary>程序化刷新文本期间置位，避免文本变化事件反向写回数值。</summary>
        private bool _isUpdatingText;

        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NumericUpDown),
                new FrameworkPropertyMetadata(typeof(NumericUpDown)));

            // 键盘焦点交给模板内部的输入框：外层控件自身不参与 Tab 切换，
            // 同时 IsKeyboardFocusWithin 仍可用于焦点高亮。
            FocusableProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(false));
            IsTabStopProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(false));
        }

        #region 依赖属性

        /// <summary>
        /// 当前值。<see cref="AllowEmpty"/> 为 true 时允许 NaN，表示“未设定”（文本为空）。
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(
                0d,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged,
                CoerceNumber));

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(NumericUpDown),
            new PropertyMetadata(double.MinValue, OnMinimumChanged, CoerceMinimum),
            IsFiniteNumber);

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(NumericUpDown),
            new PropertyMetadata(double.MaxValue, OnMaximumChanged, CoerceMaximum),
            IsFiniteNumber);

        /// <summary>每次点击上下按钮 / 方向键 / 滚轮增减的步长。</summary>
        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
            nameof(Increment),
            typeof(double),
            typeof(NumericUpDown),
            new PropertyMetadata(1d));

        /// <summary>显示的小数位数；为 null 时按默认格式显示。</summary>
        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
            nameof(DecimalPlaces),
            typeof(int?),
            typeof(NumericUpDown),
            new PropertyMetadata(null, OnDisplayFormatChanged));

        /// <summary>显示格式（标准或自定义数字格式串），设置后覆盖 <see cref="DecimalPlaces"/>。</summary>
        public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
            nameof(ValueFormat),
            typeof(string),
            typeof(NumericUpDown),
            new PropertyMetadata(null, OnDisplayFormatChanged));

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(NumericUpDown),
            new PropertyMetadata(false));

        /// <summary>是否显示上下调节按钮。</summary>
        public static readonly DependencyProperty ShowUpDownButtonProperty = DependencyProperty.Register(
            nameof(ShowUpDownButton),
            typeof(bool),
            typeof(NumericUpDown),
            new PropertyMetadata(true));

        /// <summary>
        /// 是否允许“空值”：为 true 时清空文本表示 NaN（未设定），否则清空文本会回到 0。
        /// </summary>
        public static readonly DependencyProperty AllowEmptyProperty = DependencyProperty.Register(
            nameof(AllowEmpty),
            typeof(bool),
            typeof(NumericUpDown),
            new PropertyMetadata(false, OnAllowEmptyChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Increment
        {
            get => (double)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public int? DecimalPlaces
        {
            get => (int?)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        public string? ValueFormat
        {
            get => (string?)GetValue(ValueFormatProperty);
            set => SetValue(ValueFormatProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool ShowUpDownButton
        {
            get => (bool)GetValue(ShowUpDownButtonProperty);
            set => SetValue(ShowUpDownButtonProperty, value);
        }

        public bool AllowEmpty
        {
            get => (bool)GetValue(AllowEmptyProperty);
            set => SetValue(AllowEmptyProperty, value);
        }

        #endregion

        #region 路由事件

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(ValueChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<double>),
            typeof(NumericUpDown));

        /// <summary>数值变化事件。事件参数的 NewValue 为 NaN 表示被清空（仅 <see cref="AllowEmpty"/> 时出现）。</summary>
        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        #endregion

        #region 属性回调

        private static bool IsFiniteNumber(object value)
        {
            double number = (double)value;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

        private static object CoerceNumber(DependencyObject d, object baseValue)
        {
            var control = (NumericUpDown)d;
            double number = (double)baseValue;

            // NaN / 无穷大都视为“未设定”：允许空值时保留为空，否则回到 0
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                return control.AllowEmpty ? double.NaN : Math.Clamp(0d, control.Minimum, control.Maximum);
            }

            return Math.Clamp(number, control.Minimum, control.Maximum);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control.UpdateText(force: false);
            control.RaiseEvent(new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue,
                (double)e.NewValue,
                ValueChangedEvent));
        }

        private static object CoerceMinimum(DependencyObject d, object baseValue)
        {
            double maximum = ((NumericUpDown)d).Maximum;
            return (double)baseValue > maximum ? maximum : baseValue;
        }

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control.CoerceValue(MaximumProperty);
            control.CoerceValue(ValueProperty);
        }

        private static object CoerceMaximum(DependencyObject d, object baseValue)
        {
            double minimum = ((NumericUpDown)d).Minimum;
            return (double)baseValue < minimum ? minimum : baseValue;
        }

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control.CoerceValue(MinimumProperty);
            control.CoerceValue(ValueProperty);
        }

        private static void OnDisplayFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((NumericUpDown)d).UpdateText(force: true);
        }

        private static void OnAllowEmptyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((NumericUpDown)d).CoerceValue(ValueProperty);
        }

        #endregion

        #region 模板与交互

        public override void OnApplyTemplate()
        {
            if (_textBox != null)
            {
                _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                _textBox.TextChanged -= TextBox_TextChanged;
                _textBox.LostFocus -= TextBox_LostFocus;
            }

            if (_upButton != null)
                _upButton.Click -= UpButton_Click;

            if (_downButton != null)
                _downButton.Click -= DownButton_Click;

            base.OnApplyTemplate();

            _textBox = GetTemplateChild(PartTextBox) as TextBox;
            _upButton = GetTemplateChild(PartUpButton) as RepeatButton;
            _downButton = GetTemplateChild(PartDownButton) as RepeatButton;

            if (_textBox != null)
            {
                _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _textBox.TextChanged += TextBox_TextChanged;
                _textBox.LostFocus += TextBox_LostFocus;
            }

            if (_upButton != null)
                _upButton.Click += UpButton_Click;

            if (_downButton != null)
                _downButton.Click += DownButton_Click;

            UpdateText(force: true);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (_textBox is { IsKeyboardFocused: true } && !IsReadOnly)
            {
                Step(e.Delta > 0 ? 1 : -1);
                e.Handled = true;
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e) => Step(1);

        private void DownButton_Click(object sender, RoutedEventArgs e) => Step(-1);

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsReadOnly)
                return;

            if (e.Key == Key.Up)
            {
                Step(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                Step(-1);
                e.Handled = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText || _textBox == null)
                return;

            if (string.IsNullOrWhiteSpace(_textBox.Text))
            {
                // 清空文本：允许空值时立即标记为“未设定”，否则等失焦再回到 0
                if (AllowEmpty)
                    SetCurrentValue(ValueProperty, double.NaN);
                return;
            }

            // 输入过程中文本一旦可解析且在范围内，立即写回绑定，无需等待失焦
            if (TryParse(_textBox.Text, out double number) && number >= Minimum && number <= Maximum)
                SetCurrentValue(ValueProperty, number);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_textBox == null)
                return;

            if (string.IsNullOrWhiteSpace(_textBox.Text))
            {
                SetCurrentValue(ValueProperty, AllowEmpty ? double.NaN : 0d);
            }
            else if (TryParse(_textBox.Text, out double number))
            {
                SetCurrentValue(ValueProperty, number);
            }

            // 无论数值是否变化，失焦后都把文本规整为标准显示格式（例如 007 → 7）
            UpdateText(force: true);
        }

        private void Step(int direction)
        {
            if (IsReadOnly)
                return;

            double current = double.IsNaN(Value) ? 0d : Value;

            // 取整到 10 位小数，避免 0.1 + 0.2 之类的浮点累积误差显示成 0.30000000000000004
            SetCurrentValue(ValueProperty, Math.Round(current + direction * Increment, 10));
            UpdateText(force: true);
        }

        #endregion

        #region 文本显示

        private string CurrentText
        {
            get
            {
                double number = Value;
                if (double.IsNaN(number))
                    return string.Empty;

                string? format = ValueFormat;
                if (!string.IsNullOrWhiteSpace(format))
                    return number.ToString(format, CultureInfo.CurrentCulture);

                if (DecimalPlaces.HasValue)
                    return number.ToString("F" + Math.Max(0, DecimalPlaces.Value), CultureInfo.CurrentCulture);

                return number.ToString(CultureInfo.CurrentCulture);
            }
        }

        private void UpdateText(bool force)
        {
            if (_textBox == null || (!force && _textBox.IsKeyboardFocused))
                return;

            string text = CurrentText;
            if (_textBox.Text == text)
                return;

            _isUpdatingText = true;
            try
            {
                _textBox.Text = text;
                _textBox.CaretIndex = text.Length;
            }
            finally
            {
                _isUpdatingText = false;
            }
        }

        private static bool TryParse(string text, out double number)
        {
            text = text.Trim();

            // 尚未形成合法数字的中间态（如 "-"、"."、"-."）不提交
            if (text is "-" or "." or "-." or "+")
            {
                number = 0;
                return false;
            }

            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number)
                   || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        #endregion
    }
}
