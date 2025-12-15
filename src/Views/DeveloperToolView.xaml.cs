using System.Windows.Controls;

namespace GeoChemistryNexus.Views
{
    public partial class DeveloperToolView : UserControl
    {
        public DeveloperToolView()
        {
            InitializeComponent();
            this.DataContext = new GeoChemistryNexus.ViewModels.DeveloperToolViewModel();
        }
    }
}
