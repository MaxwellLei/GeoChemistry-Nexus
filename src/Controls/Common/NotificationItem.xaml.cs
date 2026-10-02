using System.Windows.Controls;
using System.Windows.Input;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Controls
{
    public partial class NotificationItem : UserControl
    {
        public NotificationItem()
        {
            InitializeComponent();
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle || DataContext is not NotificationViewModel vm)
                return;

            if (vm.CloseCommand.CanExecute(null))
            {
                vm.CloseCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
