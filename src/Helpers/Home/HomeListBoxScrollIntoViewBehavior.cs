using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 选中项变化时把它滚动到可视区域。用于主页公告缩略条：轮播到屏幕外的公告时，条带自动跟随。
    /// </summary>
    public class HomeListBoxScrollIntoViewBehavior : Behavior<ListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += OnSelectionChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
            base.OnDetaching();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssociatedObject.SelectedItem is not { } item)
                return;

            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (AssociatedObject.IsLoaded)
                    AssociatedObject.ScrollIntoView(item);
            });
        }
    }
}
