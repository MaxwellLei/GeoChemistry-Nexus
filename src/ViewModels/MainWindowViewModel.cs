using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Views;
using HandyControl.Tools;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Grids;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using ScottPlot.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Controls;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainWindowViewModel: ObservableObject
    {
        private Frame Nav;  //导航对象
        public RelayCommand HomePage { get; private set; }  //切换主页命令
        public RelayCommand GeothermometerPage { get; private set; }  //切换模型命令
        public RelayCommand SettingPage { get; private set; }  //切换设置命令

        //初始化
        public MainWindowViewModel(Frame nav)
        {
            Nav = nav;
            //MapPage = new RelayCommand(ExecuteMapPage);
            HomePage = new RelayCommand(ExecuteHomePage);
            GeothermometerPage = new RelayCommand(ExecuteTepPage);
            //DataPage = new RelayCommand(ExecuteDataPage);
            SettingPage = new RelayCommand(ExecuteSettingPage);
            //ThemeModeChange = new RelayCommand(ExecuteThemeModeChange);

            //FunInit();
        }
        //切换主页命令
        private void ExecuteHomePage()
        {
            Nav.Navigate(MainPlotPage.GetPage());
        }

        //切换温度计计算命令
        private void ExecuteTepPage()
        {
            Nav.Navigate(GeothermometerPageView.GetPage());
        }

        //切换科学计算命令
        private void ExecuteSCICalPage()
        {
            //Nav.Navigate(ModelPageView.GetPage());
            //ModelPageView.RefeshAn();
        }

        //切换设置命令
        private void ExecuteSettingPage()
        {
            Nav.Navigate(SettingPageView.GetPage());
            SettingPageView.RefeshAn();
        }

        ////功能初始化
        //private void FunInit()
        //{
        //    //初始化自动关闭时间
        //    MessageHelper.waitTime = Convert.ToInt32(
        //        ConfigHelper.GetConfig("auto_off_time"));
        //    //自动更新
        //    if (ConfigHelper.GetConfig("auto_check_update") == "True")
        //    {
        //        UpdateHelper.CheckForUpdatesAsync();
        //    }
        //}

    }
}
