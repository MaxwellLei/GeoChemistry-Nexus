using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace GeoChemistryNexus.Helpers
{
    // 继承自 FrameworkElement 以便附加到 Border 或 StackPanel
    public class TreeViewItemExpandBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            base.OnDetaching();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 尝试获取 TreeViewItem
            var treeViewItem = AssociatedObject.TemplatedParent as TreeViewItem;
            if (treeViewItem == null)
            {
                // 如果 AssociatedObject 不是模板的一部分，
                // 或者 TemplatedParent 不是 TreeViewItem，尝试向上查找
                treeViewItem = FindAncestor<TreeViewItem>(AssociatedObject);
            }

            if (treeViewItem == null) return;

            // 检查是否点击了 Expander，如果是则不处理，交由 ToggleButton 自身处理
            var originalSource = e.OriginalSource as DependencyObject;
            if (FindAncestor<ToggleButton>(originalSource) != null)
            {
                return;
            }

            // 只有当 TreeViewItem 有子项时才处理展开/折叠
            if (treeViewItem.HasItems)
            {
                treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                e.Handled = true;
            }
        }

        private T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }

    public class RaiseMenuItemClickAction : TriggerAction<Button>
    {
        protected override void Invoke(object parameter)
        {
            if (AssociatedObject != null)
            {
                var args = new RoutedEventArgs(MenuItem.ClickEvent, AssociatedObject);
                AssociatedObject.RaiseEvent(args);
            }
        }
    }
}
