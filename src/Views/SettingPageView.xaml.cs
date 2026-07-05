using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using ScottPlot.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// SettingPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPageView
    {
        // 单例模式
        private static SettingPageView settingPage = null!;

        public SettingPageView()
        {
            InitializeComponent();
            // 连接 ViewModel
            this.DataContext = new SettingPageViewModel(SettingNav);
        }

        // 返回对象
        public static SettingPageView GetPage()
        {
            if (settingPage == null)
            {
                settingPage = new SettingPageView();
            }
            return settingPage;
        }

    }
}
