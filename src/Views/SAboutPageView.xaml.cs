using System;
using System.Collections.Generic;
using System.Linq;
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
    /// SAboutPageView.xaml 的交互逻辑
    /// </summary>
    public partial class SAboutPageView : Page
    {
        private static SAboutPageView sAboutPageView;

        public SAboutPageView()
        {
            InitializeComponent();
        }

        // 返回对象
        public static SAboutPageView GetPage()
        {
            if (sAboutPageView == null)
            {
                sAboutPageView = new SAboutPageView();
            }
            return sAboutPageView;
        }
    }
}
