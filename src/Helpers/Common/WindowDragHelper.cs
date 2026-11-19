using GeoChemistryNexus.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 窗口拖动附加属性：把任意元素变成“标题栏拖动条”。
    /// 左键按下拖动窗口，双击最大化/还原；逻辑复用 MainWindowViewModel 的命令。
    /// 用法：helpers:WindowDragHelper.IsDragHandle="True"（元素需有 Background 才能命中，可用 Transparent）
    /// 子控件（按钮、菜单、文本框等）会自行处理鼠标事件，不会触发拖动。
    /// </summary>
    public static class WindowDragHelper
    {
        public static readonly DependencyProperty IsDragHandleProperty =
            DependencyProperty.RegisterAttached(
                "IsDragHandle",
                typeof(bool),
                typeof(WindowDragHelper),
                new PropertyMetadata(false, OnIsDragHandleChanged));

        public static bool GetIsDragHandle(DependencyObject obj) => (bool)obj.GetValue(IsDragHandleProperty);

        public static void SetIsDragHandle(DependencyObject obj, bool value) => obj.SetValue(IsDragHandleProperty, value);

        private static void OnIsDragHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;

            element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            if ((bool)e.NewValue)
                element.MouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled || sender is not DependencyObject source)
                return;

            var window = Window.GetWindow(source);
            if (window?.DataContext is not MainWindowViewModel vm)
                return;

            if (e.ClickCount >= 2)
            {
                if (vm.MaximizeWindowCommand.CanExecute(window))
                    vm.MaximizeWindowCommand.Execute(window);
            }
            else
            {
                vm.MoveWindowCommand.Execute(window);
            }

            e.Handled = true;
        }
    }
}
