using GeoChemistryNexus.ViewModels;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// SCommonPageView.xaml 的交互逻辑
    /// </summary>
    public partial class SCommonPageView : Page
    {
        private static SCommonPageView commonPage = null!;

        public SCommonPageView()
        {
            InitializeComponent();
            DataContext = new SCommonPageViewModel();
        }

        public static Page GetPage()
        {
            if (commonPage == null)
            {
                commonPage = new SCommonPageView();
            }
            return commonPage;
        }

    }
}
