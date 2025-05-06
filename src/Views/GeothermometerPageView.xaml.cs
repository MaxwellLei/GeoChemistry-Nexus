using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// GeothermometerPageView.xaml 的交互逻辑
    /// </summary>
    public partial class GeothermometerPageView : Page
    {
        private static GeothermometerPageView commonPage = null;

        public GeothermometerPageView()
        {
            InitializeComponent();
            this.DataContext = new GeothermometerPageViewModel(richTextBox);
        }

        public static Page GetPage()
        {
            if (commonPage == null)
            {
                commonPage = new GeothermometerPageView();
            }
            return commonPage;
        }
    }
}
