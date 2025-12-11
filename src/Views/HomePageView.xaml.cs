using GeoChemistryNexus.ViewModels;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views
{
    public partial class HomePageView : Page
    {
        private static HomePageView _instance;

        public HomePageView()
        {
            InitializeComponent();
            this.DataContext = new HomePageViewModel();
        }

        public static HomePageView GetPage()
        {
            if (_instance == null)
            {
                _instance = new HomePageView();
            }
            return _instance;
        }
    }
}
