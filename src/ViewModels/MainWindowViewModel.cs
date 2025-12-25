using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Views;
using HandyControl.Tools;
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
            IsSideBarVisible = true;
            //FunInit();
        }

        [ObservableProperty]
        private bool _isSideBarVisible;

        [RelayCommand]
        private void ToggleSideBar()
        {
            IsSideBarVisible = !IsSideBarVisible;
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
        /// 拖动窗口命令
        /// </summary>
        /// <param name="window">当前窗体</param>
        [RelayCommand]
        private void MoveWindow(Window window)
        {
            // 只有当鼠标左键按下时才触发拖动
            if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (window.WindowState == WindowState.Normal)
                {
                    window.DragMove();
                }
            }
        }

        /// <summary>
        /// 切换绘图页命令
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void HomePage(Frame nav)
        {
            nav.Navigate(MainPlotPage.GetPage());
            IsSideBarVisible = false;
        }

        /// <summary>
        /// 切换主页命令
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void StartPage(Frame nav)
        {
            nav.Navigate(HomePageView.GetPage());
        }

        /// <summary>
        /// 切换新温度计计算命令
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void GTMNewPage(Frame nav)
        {
            nav.Navigate(GeothermometerNewPageView.GetPage());
            IsSideBarVisible = false;
        }



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
        }


        /// <summary>
        /// 帮助按钮
        /// </summary>
        [RelayCommand]
        private void Help()
        {
            string url = "https://geochemistry-nexus.pages.dev/";
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

    }
}
