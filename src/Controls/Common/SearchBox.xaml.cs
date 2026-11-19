using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 全局统一搜索框。只暴露依赖属性（文本、占位提示、配色、圆角、清空按钮），不含业务逻辑。
    /// 默认外观：白底 + 灰色细边框 + 4px 圆角；各页面通过属性覆盖即可，无需再各自重写 ControlTemplate。
    /// </summary>
    public partial class SearchBox : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SearchBox),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    null, null, false, UpdateSourceTrigger.PropertyChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(SearchBox),
                new PropertyMetadata("\uE721"));

        public static readonly DependencyProperty IconFontSizeProperty =
            DependencyProperty.Register(nameof(IconFontSize), typeof(double), typeof(SearchBox),
                new PropertyMetadata(12d));

        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register(nameof(IconBrush), typeof(Brush), typeof(SearchBox),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99))));

        public static readonly DependencyProperty PlaceholderBrushProperty =
            DependencyProperty.Register(nameof(PlaceholderBrush), typeof(Brush), typeof(SearchBox),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99))));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(SearchBox),
                new PropertyMetadata(new CornerRadius(4)));

        public static readonly DependencyProperty FocusBorderBrushProperty =
            DependencyProperty.Register(nameof(FocusBorderBrush), typeof(Brush), typeof(SearchBox),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4))));

        /// <summary>聚焦时叠加的背景色；为 null 时聚焦不改变背景。</summary>
        public static readonly DependencyProperty FocusBackgroundProperty =
            DependencyProperty.Register(nameof(FocusBackground), typeof(Brush), typeof(SearchBox),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ShowClearButtonProperty =
            DependencyProperty.Register(nameof(ShowClearButton), typeof(bool), typeof(SearchBox),
                new PropertyMetadata(false));

        static SearchBox()
        {
            BackgroundProperty.OverrideMetadata(typeof(SearchBox),
                new FrameworkPropertyMetadata(Brushes.White));
            BorderBrushProperty.OverrideMetadata(typeof(SearchBox),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0))));
            BorderThicknessProperty.OverrideMetadata(typeof(SearchBox),
                new FrameworkPropertyMetadata(new Thickness(1)));
            PaddingProperty.OverrideMetadata(typeof(SearchBox),
                new FrameworkPropertyMetadata(new Thickness(8, 0, 8, 0)));
            FontSizeProperty.OverrideMetadata(typeof(SearchBox),
                new FrameworkPropertyMetadata(12d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.Inherits));
            ForegroundProperty.OverrideMetadata(typeof(SearchBox),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    FrameworkPropertyMetadataOptions.Inherits));
            HeightProperty.OverrideMetadata(typeof(SearchBox),
                new FrameworkPropertyMetadata(26d));
            VerticalAlignmentProperty.OverrideMetadata(typeof(SearchBox),
                new FrameworkPropertyMetadata(VerticalAlignment.Center));
        }

        public SearchBox()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public double IconFontSize
        {
            get => (double)GetValue(IconFontSizeProperty);
            set => SetValue(IconFontSizeProperty, value);
        }

        public Brush IconBrush
        {
            get => (Brush)GetValue(IconBrushProperty);
            set => SetValue(IconBrushProperty, value);
        }

        public Brush PlaceholderBrush
        {
            get => (Brush)GetValue(PlaceholderBrushProperty);
            set => SetValue(PlaceholderBrushProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Brush FocusBorderBrush
        {
            get => (Brush)GetValue(FocusBorderBrushProperty);
            set => SetValue(FocusBorderBrushProperty, value);
        }

        public Brush? FocusBackground
        {
            get => (Brush?)GetValue(FocusBackgroundProperty);
            set => SetValue(FocusBackgroundProperty, value);
        }

        public bool ShowClearButton
        {
            get => (bool)GetValue(ShowClearButtonProperty);
            set => SetValue(ShowClearButtonProperty, value);
        }

        /// <summary>让输入框获得焦点（例如快捷键定位搜索）。</summary>
        public new bool Focus() => PART_TextBox.Focus();

        /// <summary>
        /// 点击搜索框的空白区域（图标、边距）时聚焦输入框，并阻止事件冒泡：
        /// 搜索框常放在可拖动窗口的顶栏内，否则会被当作拖动而抢走输入焦点。
        /// </summary>
        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!PART_TextBox.IsKeyboardFocusWithin)
                PART_TextBox.Focus();
            e.Handled = true;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Text = string.Empty;
            PART_TextBox.Focus();
        }
    }
}
