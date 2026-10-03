using GeoChemistryNexus.ViewModels.Home;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class ChronostratNavigatorWidget : UserControl
    {
        public ChronostratNavigatorWidget()
        {
            InitializeComponent();
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ChronostratTreeNodeViewModel node)
            {
                if (DataContext is ChronostratNavigatorWidgetViewModel vm)
                {
                    vm.SelectUnitCommand.Execute(node.Unit);
                }
            }
        }
    }
}
