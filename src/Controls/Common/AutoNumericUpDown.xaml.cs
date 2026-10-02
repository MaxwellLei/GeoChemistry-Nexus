using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using HandyControl.Controls;
using HandyControl.Data;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 数值调节控件包装：未设定（NaN）时清空文本并显示本地化 “Auto” 占位符；
    /// 设定后表现为普通 NumericUpDown。清空文本失焦后恢复自动。
    /// 显示最多 4 位小数且去掉末尾 0（1 显示为 1，而非 1.0000）。
    /// 输入过程中一旦文本可解析为数值即立即写回绑定，无需等待失焦。
    /// </summary>
    public partial class AutoNumericUpDown : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(AutoNumericUpDown),
                new FrameworkPropertyMetadata(
                    double.NaN,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(AutoNumericUpDown),
                new PropertyMetadata(double.MinValue));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(AutoNumericUpDown),
                new PropertyMetadata(double.MaxValue));

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(
                nameof(Increment),
                typeof(double),
                typeof(AutoNumericUpDown),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(
                nameof(DecimalPlaces),
                typeof(int?),
                typeof(AutoNumericUpDown),
                new PropertyMetadata(null));

        /// <summary>
        /// 显示格式。默认最多 4 位小数且去掉末尾 0。
        /// </summary>
        public static readonly DependencyProperty ValueFormatProperty =
            DependencyProperty.Register(
                nameof(ValueFormat),
                typeof(string),
                typeof(AutoNumericUpDown),
                new PropertyMetadata("0.####"));

        private const int ValuePrecision = 4;

        private bool _syncing;
        private bool _isAuto = true;
        /// <summary>失焦前文本已被清空，等待压制 HandyControl 写回的 0。</summary>
        private bool _pendingAuto;
        private WpfTextBox? _innerTextBox;

        public AutoNumericUpDown()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

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

        public string ValueFormat
        {
            get => (string)GetValue(ValueFormatProperty);
            set => SetValue(ValueFormatProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AutoNumericUpDown)d).SyncFromValue();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureInnerTextBoxHooked();
            SyncFromValue();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnhookInnerTextBox();
        }

        private void EnsureInnerTextBoxHooked()
        {
            InnerNumeric.ApplyTemplate();
            var textBox = FindInnerTextBox(InnerNumeric);
            if (ReferenceEquals(_innerTextBox, textBox))
                return;

            UnhookInnerTextBox();
            _innerTextBox = textBox;
            if (_innerTextBox != null)
            {
                _innerTextBox.PreviewLostKeyboardFocus += InnerTextBox_PreviewLostKeyboardFocus;
                _innerTextBox.LostFocus += InnerTextBox_LostFocus;
                _innerTextBox.TextChanged += InnerTextBox_TextChanged;
            }
        }

        private void UnhookInnerTextBox()
        {
            if (_innerTextBox == null)
                return;

            _innerTextBox.PreviewLostKeyboardFocus -= InnerTextBox_PreviewLostKeyboardFocus;
            _innerTextBox.LostFocus -= InnerTextBox_LostFocus;
            _innerTextBox.TextChanged -= InnerTextBox_TextChanged;
            _innerTextBox = null;
        }

        private static WpfTextBox? FindInnerTextBox(DependencyObject root)
        {
            if (root is WpfTextBox direct)
                return direct;

            if (root is System.Windows.Controls.Control control)
            {
                control.ApplyTemplate();
                var fromTemplate = control.Template?.FindName("PART_TextBox", control) as WpfTextBox;
                if (fromTemplate != null)
                    return fromTemplate;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var found = FindInnerTextBox(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                    return found;
            }

            return null;
        }

        private void SyncFromValue()
        {
            if (_syncing)
                return;

            EnsureInnerTextBoxHooked();

            _syncing = true;
            try
            {
                if (IsAutoValue(Value))
                {
                    ApplyAutoDisplay();
                }
                else
                {
                    _isAuto = false;
                    _pendingAuto = false;
                    InnerNumeric.Value = Math.Round(Value, ValuePrecision);
                }
            }
            finally
            {
                _syncing = false;
            }
        }

        private void ApplyAutoDisplay()
        {
            _isAuto = true;
            _pendingAuto = false;

            // HandyControl 不接受 NaN；内部保持 0，但必须把文本清空才能露出 Placeholder
            if (Math.Abs(InnerNumeric.Value) > double.Epsilon)
                InnerNumeric.Value = 0;

            ClearInnerText();
            // SetValue/Coerce 可能在之后再次 SetText("0")，再清一次
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isAuto)
                    ClearInnerText();
            }), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 将数值写回 Value（立即触发双向绑定）。_syncing 期间跳过 SyncFromValue，避免重写文本干扰输入。
        /// </summary>
        private void CommitNumericValue(double raw)
        {
            double rounded = Math.Round(raw, ValuePrecision);
            if (!_isAuto && !IsAutoValue(Value) && Math.Abs(Value - rounded) < 1e-12)
                return;

            _isAuto = false;
            _pendingAuto = false;
            _syncing = true;
            try
            {
                Value = rounded;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void InnerNumeric_ValueChanged(object sender, FunctionEventArgs<double> e)
        {
            if (_syncing)
                return;

            // 清空失焦时 HC 会强制写成 0；仅在「待恢复 Auto」或文本仍为空时忽略
            if (_pendingAuto || (_isAuto && IsInnerTextEmpty()))
                return;

            CommitNumericValue(e.Info);
        }

        private void InnerTextBox_PreviewLostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            // 在 HC LostFocus 把空文本写成 0 之前先打标（只有清空才回 Auto，输入 0 不算）
            if (IsInnerTextEmpty())
                _pendingAuto = true;
        }

        private void InnerTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_syncing)
                return;

            // 等 HC 处理完 LostFocus 后再裁定：空文本 → Auto；有数字（含 0）→ 提交数值
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_pendingAuto || IsInnerTextEmpty())
                {
                    _syncing = true;
                    try
                    {
                        Value = double.NaN;
                        ApplyAutoDisplay();
                    }
                    finally
                    {
                        _syncing = false;
                    }
                    return;
                }

                if (TryParseCurrentText(out double parsed))
                    CommitNumericValue(parsed);
            }), DispatcherPriority.Loaded);
        }

        private void InnerTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_syncing)
                return;

            // 仅压制「清空后 HC 写回 0」；用户正在输入时不拦截
            if (_pendingAuto && IsZeroLikeText())
            {
                ClearInnerText();
                return;
            }

            // 输入过程中文本一旦可解析为数值，立即写回绑定（无需等待失焦）
            if (!_pendingAuto && TryParseCurrentText(out double parsed))
                CommitNumericValue(parsed);
        }

        private bool TryParseCurrentText(out double parsed)
        {
            parsed = 0;
            if (_innerTextBox == null || string.IsNullOrWhiteSpace(_innerTextBox.Text))
                return false;

            string text = _innerTextBox.Text.Trim();
            // 尚未形成合法数字的中间态（如 "-", ".", "-."）不提交
            if (text is "-" or "." or "-." or "+")
                return false;

            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed)
                   || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
        }

        private void ClearInnerText()
        {
            if (_innerTextBox == null)
                EnsureInnerTextBoxHooked();

            if (_innerTextBox != null && !string.IsNullOrEmpty(_innerTextBox.Text))
                _innerTextBox.Text = string.Empty;
        }

        private bool IsInnerTextEmpty() =>
            _innerTextBox == null || string.IsNullOrWhiteSpace(_innerTextBox.Text);

        private bool IsZeroLikeText() =>
            TryParseCurrentText(out double v) && IsZeroLike(v);

        private static bool IsZeroLike(double value) =>
            Math.Abs(value) < 1e-12;

        private static bool IsAutoValue(double value) =>
            double.IsNaN(value) || double.IsInfinity(value);
    }
}
