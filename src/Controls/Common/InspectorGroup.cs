using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 属性面板中“开关关闭”时分组内容的呈现方式。
    /// </summary>
    public enum InspectorSwitchMode
    {
        /// <summary>内容变暗并禁用（仍占位）。</summary>
        Dim,

        /// <summary>内容直接折叠隐藏。</summary>
        Hide
    }

    /// <summary>
    /// 属性面板分组卡片：圆角白底，标题位于卡片内部的头部，
    /// 头部可附带数量角标、右侧自定义内容、总开关与折叠箭头。
    /// 外观由 Themes/Styles.xaml 中的 InspectorGroupStyle 提供。
    /// </summary>
    public class InspectorGroup : HeaderedContentControl
    {
        static InspectorGroup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(InspectorGroup),
                new FrameworkPropertyMetadata(typeof(InspectorGroup)));

            FocusableProperty.OverrideMetadata(typeof(InspectorGroup), new FrameworkPropertyMetadata(false));
            IsTabStopProperty.OverrideMetadata(typeof(InspectorGroup), new FrameworkPropertyMetadata(false));
        }

        /// <summary>头部右侧的自定义内容（如“交换”按钮）。</summary>
        public static readonly DependencyProperty HeaderRightProperty = DependencyProperty.Register(
            nameof(HeaderRight), typeof(object), typeof(InspectorGroup), new PropertyMetadata(null));

        /// <summary>标题后的数量角标文本；为空时不显示。</summary>
        public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
            nameof(Count), typeof(string), typeof(InspectorGroup), new PropertyMetadata(string.Empty));

        /// <summary>分组是否展开。</summary>
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            nameof(IsExpanded), typeof(bool), typeof(InspectorGroup),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>是否允许点击头部折叠 / 展开。</summary>
        public static readonly DependencyProperty IsCollapsibleProperty = DependencyProperty.Register(
            nameof(IsCollapsible), typeof(bool), typeof(InspectorGroup), new PropertyMetadata(true));

        /// <summary>是否在头部显示总开关。</summary>
        public static readonly DependencyProperty HasSwitchProperty = DependencyProperty.Register(
            nameof(HasSwitch), typeof(bool), typeof(InspectorGroup), new PropertyMetadata(false));

        /// <summary>总开关状态。</summary>
        public static readonly DependencyProperty IsSwitchOnProperty = DependencyProperty.Register(
            nameof(IsSwitchOn), typeof(bool), typeof(InspectorGroup),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>总开关关闭时内容的呈现方式。</summary>
        public static readonly DependencyProperty SwitchModeProperty = DependencyProperty.Register(
            nameof(SwitchMode), typeof(InspectorSwitchMode), typeof(InspectorGroup),
            new PropertyMetadata(InspectorSwitchMode.Dim));

        public object? HeaderRight
        {
            get => GetValue(HeaderRightProperty);
            set => SetValue(HeaderRightProperty, value);
        }

        public string Count
        {
            get => (string)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public bool IsCollapsible
        {
            get => (bool)GetValue(IsCollapsibleProperty);
            set => SetValue(IsCollapsibleProperty, value);
        }

        public bool HasSwitch
        {
            get => (bool)GetValue(HasSwitchProperty);
            set => SetValue(HasSwitchProperty, value);
        }

        public bool IsSwitchOn
        {
            get => (bool)GetValue(IsSwitchOnProperty);
            set => SetValue(IsSwitchOnProperty, value);
        }

        public InspectorSwitchMode SwitchMode
        {
            get => (InspectorSwitchMode)GetValue(SwitchModeProperty);
            set => SetValue(SwitchModeProperty, value);
        }
    }

    /// <summary>
    /// 属性面板顶部的单行标题栏：强调色小图标 + 标题 + 行内灰色摘要。
    /// 外观由 Themes/Styles.xaml 中的 InspectorHeaderStyle 提供。
    /// </summary>
    public class InspectorHeader : Control
    {
        static InspectorHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(InspectorHeader),
                new FrameworkPropertyMetadata(typeof(InspectorHeader)));

            FocusableProperty.OverrideMetadata(typeof(InspectorHeader), new FrameworkPropertyMetadata(false));
            IsTabStopProperty.OverrideMetadata(typeof(InspectorHeader), new FrameworkPropertyMetadata(false));
        }

        /// <summary>iconfont 图标字符。</summary>
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(InspectorHeader), new PropertyMetadata(string.Empty));

        /// <summary>标题文本。</summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(InspectorHeader), new PropertyMetadata(string.Empty));

        /// <summary>标题后的灰色摘要（如文本内容、公式），为空时不显示。</summary>
        public static readonly DependencyProperty SummaryProperty = DependencyProperty.Register(
            nameof(Summary), typeof(string), typeof(InspectorHeader), new PropertyMetadata(string.Empty));

        /// <summary>标题栏最右侧的自定义内容（如脚本面板的“只读”标签）。</summary>
        public static readonly DependencyProperty HeaderRightProperty = DependencyProperty.Register(
            nameof(HeaderRight), typeof(object), typeof(InspectorHeader), new PropertyMetadata(null));

        /// <summary>
        /// 右侧内容的可见性。绑在标题栏上，而不是绑在 <see cref="HeaderRight"/> 内部：
        /// 这块内容由模板里的 ContentPresenter 承载，不在外层控件的祖先链上。
        /// </summary>
        public static readonly DependencyProperty HeaderRightVisibilityProperty = DependencyProperty.Register(
            nameof(HeaderRightVisibility), typeof(Visibility), typeof(InspectorHeader), new PropertyMetadata(Visibility.Visible));

        public object? HeaderRight
        {
            get => GetValue(HeaderRightProperty);
            set => SetValue(HeaderRightProperty, value);
        }

        public Visibility HeaderRightVisibility
        {
            get => (Visibility)GetValue(HeaderRightVisibilityProperty);
            set => SetValue(HeaderRightVisibilityProperty, value);
        }

        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Summary
        {
            get => (string)GetValue(SummaryProperty);
            set => SetValue(SummaryProperty, value);
        }
    }
}
