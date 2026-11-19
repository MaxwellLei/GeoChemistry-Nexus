using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shell;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Views.Widgets
{
    /// <summary>
    /// Shared chrome for home tools, including native drag, snap and resizing.
    /// 标题栏中部提供 CaptionContent 槽位：小组件把工具栏、搜索、主操作按钮等放进来，
    /// 不必再在页面顶部单独占一行。
    /// </summary>
    public class WidgetWindow : Window
    {
        public static readonly DependencyProperty WidgetIconProperty = DependencyProperty.Register(
            nameof(WidgetIcon), typeof(string), typeof(WidgetWindow), new PropertyMetadata("\uE80A"));

        /// <summary>
        /// 标题栏内容。既可直接设置在 WidgetWindow 上，也可设置在作为窗体 Content 的小组件上，
        /// 后者会在小组件加载后自动转交给所在的 WidgetWindow，并沿用小组件的 DataContext。
        /// </summary>
        public static readonly DependencyProperty CaptionContentProperty = DependencyProperty.RegisterAttached(
            "CaptionContent", typeof(UIElement), typeof(WidgetWindow),
            new PropertyMetadata(null, OnCaptionContentChanged));

        public static UIElement? GetCaptionContent(DependencyObject element) =>
            (UIElement?)element.GetValue(CaptionContentProperty);

        public static void SetCaptionContent(DependencyObject element, UIElement? value) =>
            element.SetValue(CaptionContentProperty, value);

        /// <summary>
        /// 通栏侧栏（与图解新建窗口一致：侧栏从窗体顶部贯穿到底部，标题栏只覆盖右侧内容区）。
        /// 设置方式与 CaptionContent 相同，设置后窗体自带的图标和标题会让位给侧栏。
        /// </summary>
        public static readonly DependencyProperty SidebarContentProperty = DependencyProperty.RegisterAttached(
            "SidebarContent", typeof(UIElement), typeof(WidgetWindow),
            new PropertyMetadata(null, OnSidebarContentChanged));

        public static UIElement? GetSidebarContent(DependencyObject element) =>
            (UIElement?)element.GetValue(SidebarContentProperty);

        public static void SetSidebarContent(DependencyObject element, UIElement? value) =>
            element.SetValue(SidebarContentProperty, value);

        public static readonly DependencyProperty HasSidebarProperty = DependencyProperty.Register(
            nameof(HasSidebar), typeof(bool), typeof(WidgetWindow), new PropertyMetadata(false));

        public bool HasSidebar
        {
            get => (bool)GetValue(HasSidebarProperty);
            private set => SetValue(HasSidebarProperty, value);
        }
        public string WidgetIcon
        {
            get => (string)GetValue(WidgetIconProperty);
            set => SetValue(WidgetIconProperty, value);
        }

        public WidgetWindow()
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/GeoChemistryNexus;component/Themes/WidgetStyles.xaml")
            });
            Style = (Style)Resources["WidgetWindowStyle"];
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 44,
                ResizeBorderThickness = new Thickness(6),
                GlassFrameThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = false
            });
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
                (_, _) => SystemCommands.MinimizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand,
                (_, _) =>
                {
                    if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
                    else SystemCommands.MaximizeWindow(this);
                }));
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand,
                (_, _) => Close()));
            Loaded += (_, _) => AdoptHostContent(Content as DependencyObject, 0);
            UiScaleHelper.Attach(this);
            WindowWorkAreaHelper.Attach(this);
        }

        /// <summary>
        /// 兜底：窗体加载完成后，再沿逻辑树（浅层）找一遍声明了标题栏 / 侧栏内容的小组件，
        /// 避免个别小组件因加载时序未能自行转交。
        /// </summary>
        private void AdoptHostContent(DependencyObject? node, int depth)
        {
            if (node == null || depth > 3)
                return;

            if (node is FrameworkElement host
                && (GetCaptionContent(host) != null || GetSidebarContent(host) != null))
            {
                AttachToWindow(host);
            }

            foreach (var child in LogicalTreeHelper.GetChildren(node))
            {
                if (child is DependencyObject dependencyChild)
                    AdoptHostContent(dependencyChild, depth + 1);
            }
        }
        private static void OnCaptionContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 直接设置在窗体上时由模板负责呈现；这里只需保证其中的控件可点击
            if (d is WidgetWindow)
            {
                if (e.NewValue is DependencyObject content)
                    WindowChrome.SetIsHitTestVisibleInChrome((IInputElement)content, true);
                return;
            }

            HookHost(d);
        }

        private static void OnSidebarContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WidgetWindow window)
            {
                window.HasSidebar = e.NewValue != null;
                return;
            }

            HookHost(d);
        }

        private static void HookHost(DependencyObject d)
        {
            if (d is not FrameworkElement host)
                return;

            host.Loaded -= OnHostLoaded;
            host.Loaded += OnHostLoaded;
            if (host.IsLoaded)
                AttachToWindow(host);
        }

        private static void OnHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement host)
                AttachToWindow(host);
        }

        /// <summary>把小组件声明的标题栏 / 侧栏内容转交给所在的 WidgetWindow，并沿用小组件的 DataContext。</summary>
        private static void AttachToWindow(FrameworkElement host)
        {
            if (Window.GetWindow(host) is not WidgetWindow window)
                return;

            if (GetCaptionContent(host) is FrameworkElement caption)
            {
                // 让标题栏控件可点击（其余空白区域仍可拖动窗体）
                WindowChrome.SetIsHitTestVisibleInChrome(caption, true);
                if (caption.HorizontalAlignment == HorizontalAlignment.Stretch)
                    caption.HorizontalAlignment = HorizontalAlignment.Right;

                BindHostDataContext(caption, host);
                SetCaptionContent(window, caption);
            }

            if (GetSidebarContent(host) is FrameworkElement sidebar)
            {
                BindHostDataContext(sidebar, host);
                SetSidebarContent(window, sidebar);
            }
        }

        private static void BindHostDataContext(FrameworkElement target, FrameworkElement host)
        {
            BindingOperations.SetBinding(target, FrameworkElement.DataContextProperty,
                new Binding(nameof(FrameworkElement.DataContext)) { Source = host });
        }
    }
}
