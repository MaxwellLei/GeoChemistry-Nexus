using GeoChemistryNexus.ViewModels;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// SGeothermometerPageView.xaml 的交互逻辑
    /// </summary>
    public partial class SGeothermometerPageView : Page
    {
        private static SGeothermometerPageView instance = null!;

        public SGeothermometerPageView()
        {
            InitializeComponent();
            this.DataContext = new SGeothermometerPageViewModel();
        }

        public static SGeothermometerPageView GetPage()
        {
            if (instance == null)
            {
                instance = new SGeothermometerPageView();
            }
            return instance;
        }
    }
}
