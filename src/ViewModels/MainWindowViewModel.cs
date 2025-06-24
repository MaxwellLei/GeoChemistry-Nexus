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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainWindowViewModel: ObservableObject
    {

        //初始化
        public MainWindowViewModel()
        {

            //FunInit();
        }

        /// <summary>
        /// 最小化窗口
        /// </summary>
        /// <param name="window">当前窗体</param>
        [RelayCommand]
        private void MinimizeWindow(Window window)
        {
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 最大化/还原窗口
        /// </summary>
        /// <param name="window">当前窗体</param>
        [RelayCommand]
        private void MaximizeWindow(Window window)
        {
            if (window == null) return;

            if (window.WindowState != WindowState.Maximized)
            {
                window.WindowState = WindowState.Maximized;
                window.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight+2;
                window.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth+2;
            }
            else
            {
                window.WindowState = WindowState.Normal;
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="window">当前窗体</param>
        [RelayCommand]
        private void CloseWindow(Window window)
        {
            window?.Close();
        }


        /// <summary>
        /// 彩蛋
        /// </summary>
        [RelayCommand]
        private void Stinger()
        {
            // 获取当前日期和时间
            DateTime now = DateTime.Now;
            // 获取当前年份
            int currentYear = now.Year;
            MessageHelper.Success($"感谢您的使用🌹\n祝您 {currentYear} 年科研，生活一帆风顺！");
        }

        /// <summary>
        /// 切换主页命令
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void HomePage(Frame nav)
        {
            nav.Navigate(MainPlotPage.GetPage());
        }

        /// <summary>
        /// 切换新温度计计算命令
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void GTMNewPage(Frame nav)
        {
            nav.Navigate(GeothermometerNewPageView.GetPage());
        }

        /// <summary>
        /// 切换科学计算命令
        /// </summary>
        //[RelayCommand]
        //private void ExecuteSCICalPage()
        //{
        //    //Nav.Navigate(ModelPageView.GetPage());
        //    //ModelPageView.RefeshAn();
        //}


        ///切换设置命令
        [RelayCommand]
        private void SettingPage(Frame nav)
        {
            nav.Navigate(SettingPageView.GetPage());
            SettingPageView.RefeshAn();
        }


        /// <summary>
        /// 托盘菜单显示主窗体
        /// </summary>
        [RelayCommand]
        private void ShowWindow(Window window)
        {
            // 显示窗口并将其置于屏幕的最顶层
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Topmost = true;
            window.Activate();

            // 将置顶属性重置为 false，在窗口获得焦点时再次激活
            //Dispatcher.BeginInvoke(new Action(() => { window.Topmost = false; }));
        }


        /// <summary>
        /// 帮助按钮
        /// </summary>
        [RelayCommand]
        private void Help()
        {
            string url = "https://geonweb.pages.dev/";
            //拉起浏览器
            try
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            catch (Exception ex)
            {
                MessageHelper.Warning((string)System.Windows.Application.Current.Resources["OpenBrowserError"] + ex.Message);
            }
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
