using System.Windows.Controls;
using System.Windows.Input;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class PeriodicTableWidget : UserControl
    {
        public PeriodicTableWidget()
        {
            InitializeComponent();
            Focusable = true;
            Loaded += (_, _) => Focus();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not PeriodicTableWidgetViewModel vm)
                return;

            if (e.Key == Key.Enter)
            {
                vm.FindNextFromSearchCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                vm.ClearGroupCommand.Execute(null);
                vm.ClearSearchCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
