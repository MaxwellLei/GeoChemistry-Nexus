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
        private bool isFlipped = false;

        public MainWindow()
        {
            // 初始化窗体
            InitializeComponent();
            // 链接 ViewModel
            this.DataContext = new MainWindowViewModel(this.Nav);
            Nav.Navigate(MainPlotPage.GetPage());
        }

        //关闭
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //最小化窗体
        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        //最大化
        private void MaxMinButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized)
            {
                this.WindowState = WindowState.Maximized;
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            }
            else
            {
                this.WindowState = WindowState.Normal;

            }
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

        //帮助按钮
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/L-Seraphine/TimberValueEvaluationSystem/wiki/";
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

        //窗体加载完成后的按钮动画
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //创建动画过程
            var marginAnim = new ThicknessAnimation()
            {
                From = new Thickness(-200, 0, 0, 0),
                To = new Thickness(0, 0, 0, 0),
                EasingFunction = new QuadraticEase()
            };
            for (int i = 1; i < 4; i++)
            {
                Storyboard.SetTargetName(marginAnim, "RadioButton" + i);
                Storyboard.SetTargetProperty(marginAnim, new PropertyPath(MarginProperty));

                //延迟动画时间
                marginAnim.Duration = TimeSpan.FromSeconds(0.5 + i * 0.25);

                //创建动画版播放动画
                var sb = new Storyboard();
                sb.Children.Add(marginAnim);
                sb.Begin(this);

            }



            //加载自定义的鼠标样式
            //System.Windows.Input.Cursor myCursor = new System.Windows.Input.Cursor(@"Data/Cursors/pointer.cur");
            //rootborder.Cursor = myCursor;

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

        //彩蛋
        private void Stinger_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前日期和时间
            DateTime now = DateTime.Now;
            // 获取当前年份
            int currentYear = now.Year;
            MessageHelper.Success($"感谢您的使用🌹\n祝您 {currentYear} 年科研，生活一帆风顺！");
        }

        //关闭窗口
        private void ShutDownWindow(object sender, RoutedEventArgs e)
        {
            // 关闭应用程序
            System.Windows.Application.Current.Shutdown();
        }
    }
}
