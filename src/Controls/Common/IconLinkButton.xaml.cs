using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 通用「图标 + 主题色文字」链接按钮。仅暴露依赖属性，不含业务逻辑；配色由 ThemeLinkButtonStyle 统一提供。
    /// 默认图标为 Segoe MDL2 Assets 的刷新图标，可通过 Icon / IconFontFamily 替换。
    /// </summary>
    public partial class IconLinkButton : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(IconLinkButton),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(IconLinkButton),
                new PropertyMetadata("\uE72C"));

        public static readonly DependencyProperty IconFontFamilyProperty =
            DependencyProperty.Register(nameof(IconFontFamily), typeof(FontFamily), typeof(IconLinkButton),
                new PropertyMetadata(new FontFamily("Segoe MDL2 Assets")));

        public static readonly DependencyProperty IconFontSizeProperty =
            DependencyProperty.Register(nameof(IconFontSize), typeof(double), typeof(IconLinkButton),
                new PropertyMetadata(11d));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(IconLinkButton),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(IconLinkButton),
                new PropertyMetadata(null));

        public IconLinkButton()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public FontFamily IconFontFamily
        {
            get => (FontFamily)GetValue(IconFontFamilyProperty);
            set => SetValue(IconFontFamilyProperty, value);
        }

        public double IconFontSize
        {
            get => (double)GetValue(IconFontSizeProperty);
            set => SetValue(IconFontSizeProperty, value);
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
    }
}
