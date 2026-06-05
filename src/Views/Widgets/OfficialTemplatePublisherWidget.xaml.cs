using GeoChemistryNexus.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class OfficialTemplatePublisherWidget : UserControl
    {
        public OfficialTemplatePublisherWidget()
        {
            InitializeComponent();
        }

        private void SecretKeyBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is OfficialTemplatePublisherViewModel vm && sender is PasswordBox box)
                vm.SecretKey = box.Password;
        }
    }
}
