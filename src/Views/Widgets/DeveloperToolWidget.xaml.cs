using System.Windows.Controls;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class DeveloperToolWidget : UserControl
    {
        public DeveloperToolWidget()
        {
            InitializeComponent();
            this.DataContext = new GeoChemistryNexus.ViewModels.DeveloperToolViewModel();
        }
    }
}
