using GeoChemistryNexus.ViewModels;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views
{
    public partial class SPlotPageView : Page
    {
        private static SPlotPageView plotPage = null;

        public SPlotPageView()
        {
            InitializeComponent();
            this.DataContext = new SPlotPageViewModel();
        }

        public static Page GetPage()
        {
            if (plotPage == null)
            {
                plotPage = new SPlotPageView();
            }
            return plotPage;
        }
    }
}
