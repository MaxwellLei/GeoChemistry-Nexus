using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 自动关闭时间步进器：计时环 + 减 / 增按钮。
    /// 只处理纯界面交互；数值通过 <see cref="Seconds"/> 双向绑定交给 ViewModel，业务逻辑不在此处。
    /// </summary>
    public partial class AutoCloseStepper : UserControl
    {
        /// <summary>计时环周长（以线宽为单位）：π × 直径(18 - 2) / 线宽(2)。</summary>
        private const double RingLength = Math.PI * 16 / 2;

        #region 依赖属性

        public static readonly DependencyProperty SecondsProperty =
            DependencyProperty.Register(nameof(Seconds), typeof(int), typeof(AutoCloseStepper),
                new FrameworkPropertyMetadata(5,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSecondsChanged,
                    CoerceSeconds));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(AutoCloseStepper),
                new PropertyMetadata(2, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(AutoCloseStepper),
                new PropertyMetadata(5, OnRangeChanged));

        public static readonly DependencyProperty UnitTextProperty =
            DependencyProperty.Register(nameof(UnitText), typeof(string), typeof(AutoCloseStepper),
                new PropertyMetadata(string.Empty, (d, _) => ((AutoCloseStepper)d).UpdateVisual(false)));

        /// <summary>当前秒数。</summary>
        public int Seconds
        {
            get => (int)GetValue(SecondsProperty);
            set => SetValue(SecondsProperty, value);
        }

        /// <summary>最小秒数。</summary>
        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>最大秒数。</summary>
        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>数值右侧的单位文字（例如“秒”）。</summary>
        public string UnitText
        {
            get => (string)GetValue(UnitTextProperty);
            set => SetValue(UnitTextProperty, value);
        }

        #endregion

        public AutoCloseStepper()
        {
            InitializeComponent();
            Loaded += (_, _) => UpdateVisual(false);
        }

        private static object CoerceSeconds(DependencyObject d, object baseValue)
        {
            var control = (AutoCloseStepper)d;
            int min = control.Minimum;
            int max = Math.Max(control.Maximum, min);
            return Math.Clamp((int)baseValue, min, max);
        }

        private static void OnSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((AutoCloseStepper)d).UpdateVisual(true);

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(SecondsProperty);
            ((AutoCloseStepper)d).UpdateVisual(false);
        }

        private void OnDecreaseClick(object sender, RoutedEventArgs e) => Seconds--;

        private void OnIncreaseClick(object sender, RoutedEventArgs e) => Seconds++;

        private void UpdateVisual(bool animate)
        {
            if (ValueText == null)
                return;

            int seconds = Seconds;
            ValueText.Text = seconds.ToString();
            UnitTextBlock.Text = UnitText;
            UnitTextBlock.Visibility = string.IsNullOrEmpty(UnitText) ? Visibility.Collapsed : Visibility.Visible;

            DecreaseButton.IsEnabled = seconds > Minimum;
            IncreaseButton.IsEnabled = seconds < Maximum;

            double fraction = Maximum <= 0 ? 0 : Math.Clamp((double)seconds / Maximum, 0, 1);
            RingFill.StrokeDashArray = new DoubleCollection { RingLength * fraction, RingLength * 2 };

            if (!animate)
                return;

            // 数值切换时从上方轻微滑入
            var shift = new DoubleAnimation(-4, 0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ValueShift.BeginAnimation(TranslateTransform.YProperty, shift);

            var fade = new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(220));
            ValueText.BeginAnimation(OpacityProperty, fade);
        }
    }
}
