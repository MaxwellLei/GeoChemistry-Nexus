using System.Windows;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 输入类控件的标题位置。
    /// </summary>
    public enum InputTitlePlacement
    {
        /// <summary>标题显示在输入框上方。</summary>
        Top,

        /// <summary>标题显示在输入框左侧。</summary>
        Left
    }

    /// <summary>
    /// 为 TextBox / ComboBox / NumericUpDown 等输入控件提供的附加属性：占位提示、标题、圆角。
    /// 对应的外观由 Themes/Styles.xaml 中的基础控件样式负责渲染，主题色随 AppThemeHelper 即时切换。
    /// </summary>
    public static class ControlAssist
    {
        #region Placeholder

        /// <summary>
        /// 输入框为空时显示的占位提示文本。
        /// </summary>
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(ControlAssist),
                new FrameworkPropertyMetadata(string.Empty));

        public static string GetPlaceholder(DependencyObject element) =>
            (string)element.GetValue(PlaceholderProperty);

        public static void SetPlaceholder(DependencyObject element, string value) =>
            element.SetValue(PlaceholderProperty, value);

        #endregion

        #region Title

        /// <summary>
        /// 输入框的标题文本（例如坐标输入框前的 “X”、“Y”）。
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.RegisterAttached(
                "Title",
                typeof(string),
                typeof(ControlAssist),
                new FrameworkPropertyMetadata(string.Empty));

        public static string GetTitle(DependencyObject element) =>
            (string)element.GetValue(TitleProperty);

        public static void SetTitle(DependencyObject element, string value) =>
            element.SetValue(TitleProperty, value);

        #endregion

        #region TitlePlacement

        /// <summary>
        /// 标题相对输入框的位置。
        /// </summary>
        public static readonly DependencyProperty TitlePlacementProperty =
            DependencyProperty.RegisterAttached(
                "TitlePlacement",
                typeof(InputTitlePlacement),
                typeof(ControlAssist),
                new FrameworkPropertyMetadata(InputTitlePlacement.Top));

        public static InputTitlePlacement GetTitlePlacement(DependencyObject element) =>
            (InputTitlePlacement)element.GetValue(TitlePlacementProperty);

        public static void SetTitlePlacement(DependencyObject element, InputTitlePlacement value) =>
            element.SetValue(TitlePlacementProperty, value);

        #endregion

        #region TitleWidth

        /// <summary>
        /// 标题位于左侧时占用的宽度；NaN 表示按文字自适应。
        /// </summary>
        public static readonly DependencyProperty TitleWidthProperty =
            DependencyProperty.RegisterAttached(
                "TitleWidth",
                typeof(double),
                typeof(ControlAssist),
                new FrameworkPropertyMetadata(double.NaN));

        public static double GetTitleWidth(DependencyObject element) =>
            (double)element.GetValue(TitleWidthProperty);

        public static void SetTitleWidth(DependencyObject element, double value) =>
            element.SetValue(TitleWidthProperty, value);

        #endregion

        #region CornerRadius

        /// <summary>
        /// 输入控件外框圆角。基础控件样式的模板统一读取此属性，因此各处样式可以只改圆角而不重写模板。
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(ControlAssist),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        public static CornerRadius GetCornerRadius(DependencyObject element) =>
            (CornerRadius)element.GetValue(CornerRadiusProperty);

        public static void SetCornerRadius(DependencyObject element, CornerRadius value) =>
            element.SetValue(CornerRadiusProperty, value);

        #endregion
    }
}
