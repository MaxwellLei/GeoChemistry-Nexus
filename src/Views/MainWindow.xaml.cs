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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus
{
    /// <summary>
    /// 主窗体,啥也不是
    /// </summary>
    public partial class MainWindow
    {
        #region Win32 API for Window Message Handling
        private const int WM_GETMINMAXINFO = 0x0024;
        
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
        
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
        #endregion
        
        private DateTime _lastClickTime = DateTime.MinValue;
        private const int DoubleClickMilliseconds = 500; // 双击的时间窗口
    
        public MainWindow()
        {
            // 初始化窗体
            InitializeComponent();
            // 链接 ViewModel
            this.DataContext = new MainWindowViewModel();
            MyNav.Navigate(HomePageView.GetPage());
            
            // 监听窗口状态变化
            this.StateChanged += MainWindow_StateChanged;
        }

        /// <summary>
        /// 窗口状态变化事件处理
        /// </summary>
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsWindowMaximized = (this.WindowState == WindowState.Maximized);
            }
        }

        /// <summary>
        /// 窗口初始化完成，添加消息钩子处理最大化不覆盖任务栏，并设置初始位置
        /// </summary>
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }
            
            // 在窗口句柄创建后立即设置位置，确保在鼠标所在屏幕上居中
            CenterWindowOnScreen();
        }

        /// <summary>
        /// 窗口消息处理，确保最大化时不覆盖任务栏
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // 获取当前显示器的工作区域（不包括任务栏）
                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
                    
                    if (GetMonitorInfo(monitor, ref monitorInfo))
                    {
                        var workArea = monitorInfo.rcWork;
                        var monitorArea = monitorInfo.rcMonitor;
                        
                        var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                        
                        // 设置最大化位置和尺寸为工作区域（避免覆盖任务栏）
                        mmi.ptMaxPosition.X = workArea.Left - monitorArea.Left;
                        mmi.ptMaxPosition.Y = workArea.Top - monitorArea.Top;
                        mmi.ptMaxSize.X = workArea.Right - workArea.Left;
                        mmi.ptMaxSize.Y = workArea.Bottom - workArea.Top;
                        
                        Marshal.StructureToPtr(mmi, lParam, true);
                        handled = true;
                    }
                }
            }
            return IntPtr.Zero;
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

        /// <summary>
        /// 在鼠标所在屏幕上居中显示窗口
        /// </summary>
        private void CenterWindowOnScreen()
        {
            if (this.WindowState == WindowState.Maximized) return;

            // 获取当前鼠标位置
            POINT cursorPos;
            if (!GetCursorPos(out cursorPos))
            {
                // 如果获取鼠标位置失败，则使用主屏幕
                double screenWidth = SystemParameters.WorkArea.Width;
                double screenHeight = SystemParameters.WorkArea.Height;
                double screenLeft = SystemParameters.WorkArea.Left;
                double screenTop = SystemParameters.WorkArea.Top;
                
                this.Left = screenLeft + (screenWidth - this.Width) / 2;
                this.Top = screenTop + (screenHeight - this.Height) / 2;
                return;
            }

            // 根据鼠标位置获取所在的显示器
            var monitor = MonitorFromPoint(cursorPos, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
                
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var workArea = monitorInfo.rcWork;
                    
                    // 获取窗口的DPI缩放系数
                    PresentationSource source = PresentationSource.FromVisual(this);
                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;
                    
                    if (source != null && source.CompositionTarget != null)
                    {
                        dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                        dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                    }
                    
                    // 计算该显示器工作区域的宽度和高度（转换为WPF坐标）
                    double screenWidth = (workArea.Right - workArea.Left) / dpiScaleX;
                    double screenHeight = (workArea.Bottom - workArea.Top) / dpiScaleY;
                    double screenLeft = workArea.Left / dpiScaleX;
                    double screenTop = workArea.Top / dpiScaleY;
                    
                    // 计算居中位置
                    this.Left = screenLeft + (screenWidth - this.Width) / 2;
                    this.Top = screenTop + (screenHeight - this.Height) / 2;
                    return;
                }
            }
            
            // 如果以上方法都失败，使用主屏幕
            this.Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - this.Width) / 2;
            this.Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - this.Height) / 2;
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

        /// <summary>
        /// 标题栏鼠标左键按下事件处理
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var now = DateTime.Now;
            var timeSinceLastClick = (now - _lastClickTime).TotalMilliseconds;
            _lastClickTime = now;

            // 如果是双击（两次点击间隔小于阈值），触发最大化按钮的命令
            if (timeSinceLastClick < DoubleClickMilliseconds && timeSinceLastClick > 0)
            {
                // 直接触发最大化按钮的命令，确保效果完全一致
                if (MaximizeButton.Command != null && MaximizeButton.Command.CanExecute(MaximizeButton.CommandParameter))
                {
                    MaximizeButton.Command.Execute(MaximizeButton.CommandParameter);
                }
                return;
            }

            // 单击时执行拖动命令
            if (this.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.MoveWindowCommand.Execute(this);
            }
        }
    }
}
