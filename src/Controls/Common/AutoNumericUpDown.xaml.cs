using System;
using System.Windows;

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
        /// <summary>写回绑定的数值最多保留的小数位数。</summary>
        private const int ValuePrecision = 4;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(AutoNumericUpDown),
                new FrameworkPropertyMetadata(
                    double.NaN,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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

        /// <summary>内部数值框使用的样式；为空时沿用 ModernNumericUpDownStyle（属性面板可传入 InspectorNumericUpDownStyle）。</summary>
        public static readonly DependencyProperty NumericStyleProperty =
            DependencyProperty.Register(
                nameof(NumericStyle),
                typeof(Style),
                typeof(AutoNumericUpDown),
                new PropertyMetadata(null));

        public Style? NumericStyle
        {
            get => (Style?)GetValue(NumericStyleProperty);
            set => SetValue(NumericStyleProperty, value);
        }

        /// <summary>数值框内的前缀标题（如“从 / 到”），传给内部 NumericUpDown 的 ControlAssist.Title。</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(AutoNumericUpDown),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public AutoNumericUpDown()
        {
            InitializeComponent();
        }

        /// <summary>当前值；NaN 表示自动（未设定）。</summary>
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

        /// <summary>
        /// 内部数值变化：把写回绑定的数值规整到固定小数位，避免出现 0.30000000000000004 之类的尾数。
        /// </summary>
        private void InnerNumeric_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            if (double.IsNaN(value))
                return;

            double rounded = Math.Round(value, ValuePrecision);
            if (rounded != value)
                InnerNumeric.SetCurrentValue(NumericUpDown.ValueProperty, rounded);
        }
    }
}
