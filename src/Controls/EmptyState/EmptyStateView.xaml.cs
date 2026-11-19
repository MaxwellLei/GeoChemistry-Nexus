using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 统一的“空状态”展示控件：动态插画 + 标题 + 任意附加内容。
    /// 仅包含依赖属性，无业务逻辑，所有数据均通过绑定提供。
    /// <code>
    /// &lt;controls:EmptyStateView IconKind="DiagramSearch" Title="{Binding Hint}"&gt;
    ///     &lt;controls:EmptyStateView.Description&gt;
    ///         &lt;TextBlock Text="..."/&gt;
    ///     &lt;/controls:EmptyStateView.Description&gt;
    /// &lt;/controls:EmptyStateView&gt;
    /// </code>
    /// </summary>
    public partial class EmptyStateView : UserControl
    {
        public EmptyStateView()
        {
            InitializeComponent();
        }

        /// <summary>插画类型</summary>
        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(nameof(IconKind), typeof(EmptyStateIconKind), typeof(EmptyStateView),
                new PropertyMetadata(EmptyStateIconKind.None));

        public EmptyStateIconKind IconKind
        {
            get => (EmptyStateIconKind)GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }

        /// <summary>插画宽度（高度按插画比例自适应）</summary>
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(EmptyStateView),
                new PropertyMetadata(240d));

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        /// <summary>标题文字（为空时隐藏）</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyStateView),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleFontSizeProperty =
            DependencyProperty.Register(nameof(TitleFontSize), typeof(double), typeof(EmptyStateView),
                new PropertyMetadata(14d));

        public double TitleFontSize
        {
            get => (double)GetValue(TitleFontSizeProperty);
            set => SetValue(TitleFontSizeProperty, value);
        }

        public static readonly DependencyProperty TitleForegroundProperty =
            DependencyProperty.Register(nameof(TitleForeground), typeof(Brush), typeof(EmptyStateView),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA))));

        public Brush TitleForeground
        {
            get => (Brush)GetValue(TitleForegroundProperty);
            set => SetValue(TitleForegroundProperty, value);
        }

        /// <summary>标题下方的附加内容（描述、反馈链接、按钮等）</summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(object), typeof(EmptyStateView),
                new PropertyMetadata(null));

        public object Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
    }
}
