using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 属性面板通用的“滑块 + 数值框（带单位）”复合控件。
    /// </summary>
    public partial class InspectorSliderBox : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(InspectorSliderBox),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(InspectorSliderBox), new PropertyMetadata(0d));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(InspectorSliderBox), new PropertyMetadata(100d));

        /// <summary>数值框增减按钮的步长。</summary>
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(nameof(Step), typeof(double), typeof(InspectorSliderBox), new PropertyMetadata(1d));

        /// <summary>滑块的最小调整步长（键盘 / 点击轨道）。</summary>
        public static readonly DependencyProperty SliderStepProperty =
            DependencyProperty.Register(nameof(SliderStep), typeof(double), typeof(InspectorSliderBox), new PropertyMetadata(1d));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(InspectorSliderBox), new PropertyMetadata(string.Empty));

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

        public double Step
        {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        public double SliderStep
        {
            get => (double)GetValue(SliderStepProperty);
            set => SetValue(SliderStepProperty, value);
        }

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public InspectorSliderBox()
        {
            InitializeComponent();
        }
    }
}
