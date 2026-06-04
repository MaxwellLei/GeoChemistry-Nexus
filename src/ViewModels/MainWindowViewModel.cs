using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
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
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IRecipient<MainSidebarCollapseModeChangedMessage>
    {
        //初始化
        public MainWindowViewModel()
        {
            IsSideBarVisible = true;
            LoadMainSidebarCollapseMode();
            WeakReferenceMessenger.Default.Register(this);

            if (IsSideBarInIconMode)
            {
                IsSideBarNavCentered = true;
            }
        }

        [ObservableProperty]
        private string _title = "GeoChemistry Nexus";

        [ObservableProperty]
        private bool _isSideBarVisible;

        [ObservableProperty]
        private bool _isWindowMaximized = false;

        [ObservableProperty]
        private bool _useIconOnlySidebarCollapse;

        [ObservableProperty]
        private int _sidebarCollapseModeTransition;

        /// <summary>
        /// 图标模式收缩动画结束后再为 true，用于窄边栏内居中图标，避免动画过程中布局跳动。
        /// </summary>
        [ObservableProperty]
        private bool _isSideBarNavCentered;

        public bool IsSideBarInIconMode => !IsSideBarVisible && UseIconOnlySidebarCollapse;

        public bool HasVisibleSideBarSurface => IsSideBarVisible || UseIconOnlySidebarCollapse;

        private int _sideBarNavCenteredGeneration;

        private const int SidebarIconCollapseAnimationMs = 280;

        partial void OnIsSideBarVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(IsSideBarInIconMode));
            OnPropertyChanged(nameof(HasVisibleSideBarSurface));
            UpdateSideBarNavCentered();
        }

        partial void OnUseIconOnlySidebarCollapseChanged(bool value)
        {
            OnPropertyChanged(nameof(IsSideBarInIconMode));
            OnPropertyChanged(nameof(HasVisibleSideBarSurface));
            UpdateSideBarNavCentered();
        }

        private void UpdateSideBarNavCentered()
        {
            var generation = ++_sideBarNavCenteredGeneration;

            if (!IsSideBarInIconMode)
            {
                IsSideBarNavCentered = false;
                return;
            }

            IsSideBarNavCentered = false;
            _ = SetSideBarNavCenteredAfterCollapseAsync(generation);
        }

        private async Task SetSideBarNavCenteredAfterCollapseAsync(int generation)
        {
            await Task.Delay(SidebarIconCollapseAnimationMs);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (generation != _sideBarNavCenteredGeneration || !IsSideBarInIconMode)
                {
                    return;
                }

                IsSideBarNavCentered = true;
            }, DispatcherPriority.Render);
        }

        private void LoadMainSidebarCollapseMode()
        {
            UseIconOnlySidebarCollapse = Helpers.ConfigHelper.GetConfig("main_sidebar_collapse_mode") == "1";
        }

        public void Receive(MainSidebarCollapseModeChangedMessage message)
        {
            if (UseIconOnlySidebarCollapse == message.Value)
            {
                return;
            }

            if (!IsSideBarVisible)
            {
                SidebarCollapseModeTransition = UseIconOnlySidebarCollapse ? 1 : 2;
            }

            UseIconOnlySidebarCollapse = message.Value;

            if (SidebarCollapseModeTransition != 0)
            {
                _ = ResetSidebarCollapseModeTransitionAsync();
            }

            UpdateSideBarNavCentered();
        }

        private async Task ResetSidebarCollapseModeTransitionAsync()
        {
            await Task.Delay(320);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SidebarCollapseModeTransition = 0;
            }, DispatcherPriority.Render);
        }

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

            if (window.WindowState == WindowState.Normal)
            {
                // 使用系统原生最大化
                window.WindowState = WindowState.Maximized;
                IsWindowMaximized = true;
            }
            else
            {
                // 还原窗口
                window.WindowState = WindowState.Normal;
                IsWindowMaximized = false;
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
                // 如果窗口是最大化状态，先还原
                if (window.WindowState == WindowState.Maximized)
                {
                    // 计算鼠标在窗口中的相对位置比例
                    var mouseX = System.Windows.Input.Mouse.GetPosition(window).X;
                    var mouseRatio = mouseX / window.ActualWidth;
                    
                    // 还原窗口
                    window.WindowState = WindowState.Normal;
                    
                    // 计算新的窗口位置，使鼠标保持在标题栏的相对位置
                    var screenPoint = window.PointToScreen(System.Windows.Input.Mouse.GetPosition(window));
                    window.Left = screenPoint.X - (window.ActualWidth * mouseRatio);
                    window.Top = screenPoint.Y - 20; // 减去标题栏高度的一半
                    
                    IsWindowMaximized = false;
                }
                
                window.DragMove();
            }
        }

        /// <summary>
        /// 导航并清除历史记录（防止内存堆积）
        /// </summary>
        private void NavigateToPage(Frame nav, object pageContent)
        {
            nav.Navigate(pageContent);
            // 清除后退栈，防止页面在内存中堆积
            while (nav.CanGoBack)
            {
                nav.RemoveBackEntry();
            }
            // 强制垃圾回收以释放未被引用的页面内存
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// 切换绘图页命令
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void HomePage(Frame nav)
        {
            NavigateToPage(nav, MainPlotPage.GetPage());
            IsSideBarVisible = false;
        }

        /// <summary>
        /// 切换主页命令
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void StartPage(Frame nav)
        {
            NavigateToPage(nav, HomePageView.GetPage());
        }

        /// <summary>
        /// 切换数据预处理与校正页面
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void DataPreprocessingPage(Frame nav)
        {
            NavigateToPage(nav, DataPreprocessingPageView.GetPage());
            IsSideBarVisible = false;
        }

        /// <summary>
        /// 切换新温度计计算命令（GTM 页面）
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void GTMNewPage(Frame nav)
        {
            NavigateToPage(nav, GeothermometerPageView.GetPage());
            IsSideBarVisible = false;
        }

        /// <summary>
        /// 切换CIPW标准矿物计算页面
        /// </summary>
        /// <param name="nav">导航</param>
        [RelayCommand]
        private void CipwPage(Frame nav)
        {
            NavigateToPage(nav, CipwPageView.GetPage());
            IsSideBarVisible = false;
        }



        ///切换设置命令
        [RelayCommand]
        private void SettingPage(Frame nav)
        {
            nav.Navigate(SettingPageView.GetPage());
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
