using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 属性面板通用的字体编辑块：字体下拉 + 字号 / 粗体 / 斜体 / 颜色。
    /// 文本、标题、坐标轴标题、副标题、刻度标签等面板共用，只需绑定下面的 5 个值。
    /// </summary>
    public partial class InspectorFontEditor : UserControl
    {
        public static readonly DependencyProperty FontFamilyNameProperty =
            DependencyProperty.Register(nameof(FontFamilyName), typeof(string), typeof(InspectorFontEditor),
                new FrameworkPropertyMetadata("Arial", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty PointSizeProperty =
            DependencyProperty.Register(nameof(PointSize), typeof(double), typeof(InspectorFontEditor),
                new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsBoldProperty =
            DependencyProperty.Register(nameof(IsBold), typeof(bool), typeof(InspectorFontEditor),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsItalicProperty =
            DependencyProperty.Register(nameof(IsItalic), typeof(bool), typeof(InspectorFontEditor),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty FontColorProperty =
            DependencyProperty.Register(nameof(FontColor), typeof(Color), typeof(InspectorFontEditor),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>是否显示字体下拉框（只调整字号 / 样式 / 颜色时可隐藏）。</summary>
        public static readonly DependencyProperty ShowFamilyProperty =
            DependencyProperty.Register(nameof(ShowFamily), typeof(bool), typeof(InspectorFontEditor),
                new PropertyMetadata(true));

        public string FontFamilyName
        {
            get => (string)GetValue(FontFamilyNameProperty);
            set => SetValue(FontFamilyNameProperty, value);
        }

        /// <summary>字号（pt）。</summary>
        public double PointSize
        {
            get => (double)GetValue(PointSizeProperty);
            set => SetValue(PointSizeProperty, value);
        }

        public bool IsBold
        {
            get => (bool)GetValue(IsBoldProperty);
            set => SetValue(IsBoldProperty, value);
        }

        public bool IsItalic
        {
            get => (bool)GetValue(IsItalicProperty);
            set => SetValue(IsItalicProperty, value);
        }

        public Color FontColor
        {
            get => (Color)GetValue(FontColorProperty);
            set => SetValue(FontColorProperty, value);
        }

        public bool ShowFamily
        {
            get => (bool)GetValue(ShowFamilyProperty);
            set => SetValue(ShowFamilyProperty, value);
        }

        public InspectorFontEditor()
        {
            InitializeComponent();
        }
    }
}
