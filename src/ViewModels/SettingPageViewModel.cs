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
        public RelayCommand ShortcutPage { get; private set; } //切换快捷键说明命令
        public RelayCommand AboutPage { get; private set; }   //切换关于命令
        public RelayCommand GeothermometerPage { get; private set; }   //切换地质温度计设置命令

        public SettingPageViewModel(Frame nav)
        {
            Nav = nav;
            CommonPage = new RelayCommand(ExecuteCommonPage);
            PlotPage = new RelayCommand(ExecutePlotPage);
            ShortcutPage = new RelayCommand(ExecuteShortcutPage);
            AboutPage = new RelayCommand(ExecuteAboutPage);
            GeothermometerPage = new RelayCommand(ExecuteGeothermometerPage);
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

        //切换快捷键说明命令
        private void ExecuteShortcutPage()
        {
            Nav.Navigate(SShortcutPageView.GetPage());
        }

        //切换常规设置命令
        private void ExecuteCommonPage()
        {
            Nav.Navigate(SCommonPageView.GetPage());
        }

        //切换地质温度计设置命令
        private void ExecuteGeothermometerPage()
        {
            Nav.Navigate(SGeothermometerPageView.GetPage());
        }
    }
}
