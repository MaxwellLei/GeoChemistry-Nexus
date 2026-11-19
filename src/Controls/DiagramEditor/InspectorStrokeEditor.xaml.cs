using GeoChemistryNexus.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 属性面板通用的描边复合控件：颜色 + 宽度 + 线型（可选）一行搞定。
    /// 线条、函数、网格线（主 / 次）、多边形边框、点描边共用。
    /// </summary>
    public partial class InspectorStrokeEditor : UserControl
    {
        public static readonly DependencyProperty StrokeColorProperty =
            DependencyProperty.Register(nameof(StrokeColor), typeof(Color), typeof(InspectorStrokeEditor),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty StrokeWidthProperty =
            DependencyProperty.Register(nameof(StrokeWidth), typeof(double), typeof(InspectorStrokeEditor),
                new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register(nameof(LineStyle), typeof(LineDefinition.LineType), typeof(InspectorStrokeEditor),
                new FrameworkPropertyMetadata(LineDefinition.LineType.Solid, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>是否显示线型下拉；为 false 时仅有颜色 + 宽度。</summary>
        public static readonly DependencyProperty ShowLineStyleProperty =
            DependencyProperty.Register(nameof(ShowLineStyle), typeof(bool), typeof(InspectorStrokeEditor),
                new PropertyMetadata(true, OnShowLineStyleChanged));

        /// <summary>线宽允许的最小值。</summary>
        public static readonly DependencyProperty MinStrokeWidthProperty =
            DependencyProperty.Register(nameof(MinStrokeWidth), typeof(double), typeof(InspectorStrokeEditor),
                new PropertyMetadata(0d));

        public Color StrokeColor
        {
            get => (Color)GetValue(StrokeColorProperty);
            set => SetValue(StrokeColorProperty, value);
        }

        public double StrokeWidth
        {
            get => (double)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        public LineDefinition.LineType LineStyle
        {
            get => (LineDefinition.LineType)GetValue(LineStyleProperty);
            set => SetValue(LineStyleProperty, value);
        }

        public bool ShowLineStyle
        {
            get => (bool)GetValue(ShowLineStyleProperty);
            set => SetValue(ShowLineStyleProperty, value);
        }

        public double MinStrokeWidth
        {
            get => (double)GetValue(MinStrokeWidthProperty);
            set => SetValue(MinStrokeWidthProperty, value);
        }

        public InspectorStrokeEditor()
        {
            InitializeComponent();
        }

        private static void OnShowLineStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not InspectorStrokeEditor control) return;

            bool show = (bool)e.NewValue;
            control.LineStyleCombo.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            control.GapColumn.Width = show ? new GridLength(6) : new GridLength(0);
            control.StyleColumn.Width = show ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            control.WidthColumn.Width = show ? new GridLength(82) : new GridLength(1, GridUnitType.Star);
        }
    }
}
