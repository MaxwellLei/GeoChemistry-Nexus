using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GeoChemistryNexus.ViewModels
{
    class SettingPageViewModel
    {
        private Frame Nav;  //导航对象
        public RelayCommand CommonPage { get; private set; }   //切换常规设置命令
        public RelayCommand PlotPage { get; private set; }     //切换绘图设置命令
        public RelayCommand ExteriorPage { get; private set; }   //切换外观设置命令
        public RelayCommand AboutPage { get; private set; }   //切换外观设置命令

        public SettingPageViewModel(Frame nav)
        {
            Nav = nav;
            CommonPage = new RelayCommand(ExecuteCommonPage);
            PlotPage = new RelayCommand(ExecutePlotPage);
            ExteriorPage = new RelayCommand(ExecuteExteriorPage);
            AboutPage = new RelayCommand(ExecuteAboutPage);
            Nav.Navigate(SCommonPageView.GetPage());
        }

        //切换绘图设置命令
        private void ExecutePlotPage()
        {
            Nav.Navigate(SPlotPageView.GetPage());
        }

        //切换关于命令
        private void ExecuteAboutPage()
        {
            Nav.Navigate(SAboutPageView.GetPage());
        }

        //切换外观设置命令
        private void ExecuteExteriorPage()
        {
            //Nav.Navigate(SExteriorPageView.GetPage());
        }

        //切换常规设置命令
        private void ExecuteCommonPage()
        {
            Nav.Navigate(SCommonPageView.GetPage());
        }
    }
}
