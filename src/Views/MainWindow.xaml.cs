using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Views;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus
{
    /// <summary>
    /// 主窗体，啥也不是
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            // 初始化窗体
            InitializeComponent();
            // 链接 ViewModel
            this.DataContext = new MainWindowViewModel();
            MyNav.Navigate(HomePageView.GetPage());
        }

        //显示窗口
        private void ShowWindow(object sender, RoutedEventArgs e)
        {
            // 显示窗口并将其置于屏幕的最顶层
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Topmost = true;
            this.Activate();

            // 将置顶属性重置为 false，在窗口获得焦点时再次激活
            Dispatcher.BeginInvoke(new Action(() => { this.Topmost = false; }));
        }

        //窗体加载完成后的按钮动画
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            //加载自定义的鼠标样式
            //System.Windows.Input.Cursor myCursor = new System.Windows.Input.Cursor(@"Data/Cursors/pointer.cur");
            //rootborder.Cursor = myCursor;

            // 将窗口置顶
            this.Topmost = true;
            this.Activate();
            // 将置顶属性重置为 false
            Dispatcher.BeginInvoke(new Action(() => { this.Topmost = false; }));
        }

        //窗体关闭触发
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ConfigHelper.GetConfig("exit_program_mode") == "0")
            {
                // 将窗口隐藏并最小化到托盘
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
                this.WindowState = WindowState.Minimized;
            }
        }

        //关闭窗口
        private void ShutDownWindow(object sender, RoutedEventArgs e)
        {
            // 关闭应用程序
            System.Windows.Application.Current.Shutdown();
        }
    }
}
