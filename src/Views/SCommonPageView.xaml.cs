using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// SCommonPageView.xaml 的交互逻辑
    /// </summary>
    public partial class SCommonPageView : Page
    {
        private static SCommonPageView commonPage = null;

        public SCommonPageView()
        {
            InitializeComponent();
            this.DataContext = new SCommonPageViewModel();
            (this.DataContext as SCommonPageViewModel).CoverFlowMain = this.CoverFlow;
            (this.DataContext as SCommonPageViewModel).GetFlowPic();
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
