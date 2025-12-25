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
        private static SettingPageView settingPage = null;

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

        //动画重新播放
        public static void RefeshAn()
        {
            //创建动画过程
            //var marginAnim = new ThicknessAnimation()
            //{
            //    From = new Thickness(0, 0, -300, 0),
            //    To = new Thickness(0, 0, 0, 0),
            //    EasingFunction = new QuadraticEase()
            //};
            //for (int i = 1; i < 4; i++)
            //{
            //    Storyboard.SetTargetName(marginAnim, "RadioButton" + i);
            //    Storyboard.SetTargetProperty(marginAnim, new PropertyPath(MarginProperty));

            //    //延迟动画时间
            //    marginAnim.Duration = TimeSpan.FromSeconds(0.4 + i * 0.05);

            //    //创建动画版播放动画
            //    var sb = new Storyboard();
            //    sb.Children.Add(marginAnim);
            //    sb.Begin(settingPage);
            //}
        }

    }
}
