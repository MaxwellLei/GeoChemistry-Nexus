using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Views;
using CommunityToolkit.Mvvm.Messaging;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Threading;

namespace GeoChemistryNexus
{
    /// <summary>
    /// 主窗体,啥也不是
    /// </summary>
    public partial class MainWindow : INotificationCenterHost
    {
        /// <summary>主窗口通知中心，与右下角通知区绑定的是同一个实例。</summary>
        public NotificationCenter Notifications => NotificationManager.Instance;

        #region Win32 API for Window Message Handling
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_WINDOWPOSCHANGING = 0x0046;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

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

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }
        #endregion

        private IntPtr _lastMonitorHandle = IntPtr.Zero;
        private bool _isApplyingAdaptiveWindowSize;

        /// <summary>首帧渲染前为 false：此阶段 DPI 变化时把系统建议矩形改为正中。</summary>
        private bool _startupCentered;

        /// <summary>
        /// 页面切换保护期：用户在中间手动调整过窗口大小/位置后，切换页面（尤其是首次创建的懒加载页面）
        /// 期间产生的自适应复位（回到默认 1360×780 并居中）一律不允许发生。
        /// </summary>
        private static readonly TimeSpan NavigationGuardDuration = TimeSpan.FromMilliseconds(1500);
        private readonly DispatcherTimer _navigationGuardTimer;
        private bool _isNavigationGuardActive;
        private bool _isRestoringGuardedBounds;
        private Rect _navigationGuardBounds;

        public MainWindow()
        {
            // 初始化窗体
            InitializeComponent();
            // 链接 ViewModel
            this.DataContext = new MainWindowViewModel();
            MyNav.Navigate(HomePageView.GetPage());

            _navigationGuardTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = NavigationGuardDuration
            };
            _navigationGuardTimer.Tick += (_, _) => EndNavigationSizeGuard();
            MyNav.Navigating += MyNav_Navigating;
            this.SizeChanged += (_, _) => RestoreBoundsChangedDuringNavigation();
            // 监听窗口状态 / 位置 / DPI 变化，校正混 DPI 多屏最大化尺寸
            this.StateChanged += MainWindow_StateChanged;
            this.LocationChanged += MainWindow_LocationChanged;
            this.DpiChanged += MainWindow_DpiChanged;
            this.Loaded += MainWindow_Loaded;
            this.ContentRendered += MainWindow_ContentRendered;
            // handledEventsToo：输入法或控件抢先标记 Handled 时仍要收到 Ctrl+,
            AddHandler(PreviewKeyDownEvent, new KeyEventHandler(MainWindow_PreviewKeyDown), handledEventsToo: true);

            // 高分屏 UI 自适应缩放（Auto 跟当前显示器；设置可覆盖）
            UiScaleHelper.Attach(this);
            WeakReferenceMessenger.Default.Register<UiScaleModeChangedMessage>(this, (_, __) =>
            {
                // 用户主动修改界面缩放，属于明确意图，不受页面切换保护期限制
                Dispatcher.BeginInvoke(new Action(() => ApplyAdaptiveWindowSizeForCurrentMonitor(force: true)));
            });
        }

        /// <summary>开始页面切换保护：记录切换前用户设置的窗口位置与大小。</summary>
        private void MyNav_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (WindowState != WindowState.Normal || !IsLoaded)
                return;

            _navigationGuardBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
            _isNavigationGuardActive = true;
            _navigationGuardTimer.Stop();
            _navigationGuardTimer.Start();
        }

        private void EndNavigationSizeGuard()
        {
            _navigationGuardTimer.Stop();
            _isNavigationGuardActive = false;
        }

        /// <summary>
        /// 保护期内窗口大小/位置被非用户操作改变时，恢复为切换页面前的状态。
        /// 用户正在按住鼠标拖动 / 调整窗口，或窗口已最大化/最小化时不干预。
        /// </summary>
        private void RestoreBoundsChangedDuringNavigation()
        {
            if (!_isNavigationGuardActive || _isRestoringGuardedBounds)
                return;

            if (WindowState != WindowState.Normal)
            {
                EndNavigationSizeGuard();
                return;
            }

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                // 用户正在手动调整，以用户的新尺寸为准
                _navigationGuardBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
                return;
            }

            var target = _navigationGuardBounds;
            if (DoubleUtilEquals(Width, target.Width) && DoubleUtilEquals(Height, target.Height)
                && DoubleUtilEquals(Left, target.Left) && DoubleUtilEquals(Top, target.Top))
                return;

            _isRestoringGuardedBounds = true;
            try
            {
                Width = target.Width;
                Height = target.Height;
                Left = target.Left;
                Top = target.Top;
            }
            finally
            {
                _isRestoringGuardedBounds = false;
            }
        }

        /// <summary>
        /// 全局快捷键：Ctrl+, 打开设置页（仅转发给 ViewModel 命令）。
        /// 中文输入法下 e.Key 通常是 ImeProcessed，真实按键在 ImeProcessedKey，不能只比较 OemComma。
        /// </summary>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsCtrlComma(e) || NotificationManager.Instance.IsModal)
                return;

            if (DataContext is MainWindowViewModel vm && vm.OpenSettingsByShortcutCommand.CanExecute(MyNav))
            {
                vm.OpenSettingsByShortcutCommand.Execute(MyNav);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 仅 Ctrl（不含 Shift / Alt / Win）时，当前键是否为逗号。
        /// </summary>
        private static bool IsCtrlComma(KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)
                || Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
                return false;

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                return false;

            var key = e.Key switch
            {
                Key.ImeProcessed when e.ImeProcessedKey != Key.None => e.ImeProcessedKey,
                Key.DeadCharProcessed when e.DeadCharProcessedKey != Key.None => e.DeadCharProcessedKey,
                Key.System when e.SystemKey != Key.None => e.SystemKey,
                _ => e.Key
            };

            if (key == Key.OemComma)
                return true;

            // 输入法把键值藏起来时，事件不再是 OemComma，但逗号键仍处于按下状态
            return (key is Key.None or Key.ImeProcessed or Key.DeadCharProcessed or Key.System)
                && Keyboard.IsKeyDown(Key.OemComma);
        }

        private void ApplyAdaptiveWindowSizeForCurrentMonitor(bool force = false)
        {
            if (WindowState != WindowState.Normal || _isApplyingAdaptiveWindowSize)
                return;

            // 页面切换期间不做自适应复位，避免把用户调小的窗口撑回默认尺寸
            if (_isNavigationGuardActive && !force)
                return;

            _isApplyingAdaptiveWindowSize = true;
            try
            {
                UiScaleHelper.Apply(this);
                WindowSizeHelper.ApplyAdaptiveSizePreservingCenter(this);
                UpdateMaxSizeFromCurrentMonitor();
            }
            finally
            {
                _isApplyingAdaptiveWindowSize = false;
            }
        }

        /// <summary>
        /// 首次渲染后（DPI 变化已处理完毕）按物理像素把窗口精确居中，仅执行一次。
        /// </summary>
        private void MainWindow_ContentRendered(object? sender, EventArgs e)
        {
            ContentRendered -= MainWindow_ContentRendered;
            _startupCentered = true;

            // 兜底校正：正常情况下窗口已在正中，这里不会产生移动
            _isApplyingAdaptiveWindowSize = true;
            try
            {
                WindowSizeHelper.CenterInWorkAreaPixels(this, preferCursorMonitor: true);
            }
            finally
            {
                _isApplyingAdaptiveWindowSize = false;
            }

            _lastMonitorHandle = GetCurrentMonitorHandle();
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            UpdateMaxSizeFromCurrentMonitor();
            await AppUpdateService.TryAutoCheckOnStartupAsync();
            await TryOpenPendingAssociatedPackageAsync();
        }

        /// <summary>
        /// 处理启动参数传入的图解或温压计包：二次确认后导航到对应模块并导入。
        /// </summary>
        public async Task TryOpenAssociatedPackageAsync(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            if (!TemplatePackageFileExtensions.IsAssociatedPackagePath(filePath))
                return;

            if (DataContext is not MainWindowViewModel mainVm)
                return;

            string fileName = System.IO.Path.GetFileName(filePath);
            string confirmMessage = string.Format(
                LanguageService.GetString(
                    "confirm_import_associated_package",
                    "Do you want to import the template file \"{0}\"?"),
                fileName);

            bool confirmed = await MessageHelper.ShowAsyncDialog(
                confirmMessage,
                LanguageService.Instance["Cancel"] ?? "Cancel",
                LanguageService.Instance["Confirm"] ?? "Confirm");

            if (!confirmed)
                return;

            if (TemplatePackageFileExtensions.IsDiagramAssociatedPath(filePath))
            {
                if (mainVm.PlotPageCommand.CanExecute(MyNav))
                    await mainVm.PlotPageCommand.ExecuteAsync(MyNav);

                var page = MainPlotPage.GetPage();
                if (page.DataContext is MainPlotViewModel plotVm &&
                    plotVm.ImportCustomTemplateFromPathCommand.CanExecute(filePath))
                {
                    await plotVm.ImportCustomTemplateFromPathCommand.ExecuteAsync(filePath);
                }

                return;
            }

            if (TemplatePackageFileExtensions.IsGeothermometerAssociatedPath(filePath))
            {
                if (mainVm.GTMNewPageCommand.CanExecute(MyNav))
                    await mainVm.GTMNewPageCommand.ExecuteAsync(MyNav);

                var page = (GeothermometerPageView)GeothermometerPageView.GetPage();
                if (page.DataContext is GeothermometerPageViewModel geoVm &&
                    geoVm.ImportPluginFromPathCommand.CanExecute(filePath))
                {
                    geoVm.ImportPluginFromPathCommand.Execute(filePath);
                }
            }
        }

        private async Task TryOpenPendingAssociatedPackageAsync()
        {
            await SingleInstanceHelper.DrainPendingPackagesAsync(this);
        }

        /// <summary>
        /// 窗口状态变化事件处理
        /// </summary>
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            UpdateMaxSizeFromCurrentMonitor();

            if (WindowState == WindowState.Maximized)
            {
                // Win32 在「副屏 > 主屏」时会错误放大最大化尺寸；布局完成后强制钉到工作区
                Dispatcher.BeginInvoke(CorrectMaximizedWindowBounds, DispatcherPriority.ApplicationIdle);
            }

            if (this.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsWindowMaximized = (this.WindowState == WindowState.Maximized);
            }
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            RestoreBoundsChangedDuringNavigation();

            // 启动阶段位置/尺寸已由 SourceInitialized 与 WM_DPICHANGED 钩子确定，不再二次调整
            if (_isApplyingAdaptiveWindowSize || !_startupCentered)
                return;

            // 跨屏拖动时：先刷新 UI 缩放，再按新屏推荐尺寸调整窗口
            var monitor = GetCurrentMonitorHandle();
            if (monitor != IntPtr.Zero && monitor != _lastMonitorHandle)
            {
                _lastMonitorHandle = monitor;
                ApplyAdaptiveWindowSizeForCurrentMonitor();
            }
        }

        private void MainWindow_DpiChanged(object? sender, DpiChangedEventArgs e)
        {
            // DpiChanged 是冒泡路由事件：页面里任何子元素（如模板卡片缩略图 Image）加入可视树时
            // 都会向上冒泡到窗口，只有窗口自身真正的 DPI 变化才允许重算窗口尺寸，
            // 否则切换模板分类时会把用户调整过的窗口撑回默认大小。
            if (!ReferenceEquals(e.OriginalSource, this))
                return;

            // 启动阶段 DIP 尺寸已按目标显示器 DPI 算好，系统缩放建议矩形会保持该 DIP 尺寸，无需再调整
            if (!_startupCentered)
                return;

            // DPI 实际未变化时不需要重新计算窗口尺寸
            if (e.OldDpi.Equals(e.NewDpi))
                return;

            ApplyAdaptiveWindowSizeForCurrentMonitor();
        }

        /// <summary>
        /// 窗口初始化完成，添加消息钩子处理最大化不覆盖任务栏，并设置初始位置
        /// </summary>
        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }

            // 先落到鼠标所在屏并给出初始尺寸，再按该屏计算 UI 缩放，最后按缩放修正窗口大小
            _isApplyingAdaptiveWindowSize = true;
            try
            {
                WindowSizeHelper.ApplyAdaptiveSize(this, centerInWorkArea: true, preferCursorMonitor: true);
                UiScaleHelper.Apply(this);
                WindowSizeHelper.ApplyAdaptiveSizePreservingCenter(this);
                // 最后按物理像素精确居中（此时 DPI 变化通常已处理完毕）
                WindowSizeHelper.CenterInWorkAreaPixels(this, preferCursorMonitor: true);
            }
            finally
            {
                _isApplyingAdaptiveWindowSize = false;
            }

            UpdateMaxSizeFromCurrentMonitor();
        }

        /// <summary>
        /// 用当前显示器工作区（DIP）限制 MaxWidth/MaxHeight。
        /// 副屏分辨率大于主屏时，Win32 会对 ptMaxSize 做主屏补偿，导致最大化过大并盖住任务栏；
        /// WPF 层钳制是第一层兜底。
        /// </summary>
        private void UpdateMaxSizeFromCurrentMonitor()
        {
            if (!TryGetCurrentMonitorWorkAreaDip(out var workArea))
                return;

            // 略减 1 DIP，避免舍入后仍溢出 1 像素
            var maxWidth = Math.Max(1, workArea.Width - 1);
            var maxHeight = Math.Max(1, workArea.Height - 1);

            if (!DoubleUtilEquals(MaxWidth, maxWidth))
                MaxWidth = maxWidth;
            if (!DoubleUtilEquals(MaxHeight, maxHeight))
                MaxHeight = maxHeight;
        }

        /// <summary>
        /// 最大化后把 HWND 直接对齐到当前显示器工作区（物理像素），彻底避免盖住任务栏/超出屏幕。
        /// </summary>
        private void CorrectMaximizedWindowBounds()
        {
            if (WindowState != WindowState.Maximized)
                return;

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            var monitor = GetCurrentMonitorHandle();
            if (!TryGetMonitorInfo(monitor, out var monitorInfo))
                return;

            var work = monitorInfo.rcWork;
            var width = work.Right - work.Left;
            var height = work.Bottom - work.Top;
            if (width <= 0 || height <= 0)
                return;

            if (GetWindowRect(hwnd, out var current) &&
                current.Left == work.Left &&
                current.Top == work.Top &&
                current.Right - current.Left == width &&
                current.Bottom - current.Top == height)
            {
                return;
            }

            SetWindowPos(hwnd, IntPtr.Zero, work.Left, work.Top, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        }

        private static bool DoubleUtilEquals(double a, double b) =>
            Math.Abs(a - b) < 0.5;

        /// <summary>
        /// 获取窗口当前所在显示器句柄（优先用窗口中心点，跨屏/最大化过程中更稳）
        /// </summary>
        private IntPtr GetCurrentMonitorHandle()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return IntPtr.Zero;

            if (GetWindowRect(hwnd, out var rect))
            {
                var center = new POINT
                {
                    X = rect.Left + (rect.Right - rect.Left) / 2,
                    Y = rect.Top + (rect.Bottom - rect.Top) / 2
                };
                var fromPoint = MonitorFromPoint(center, MONITOR_DEFAULTTONEAREST);
                if (fromPoint != IntPtr.Zero)
                    return fromPoint;
            }

            return MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        }

        private bool TryGetMonitorInfo(IntPtr monitor, out MONITORINFO monitorInfo)
        {
            monitorInfo = new MONITORINFO();
            if (monitor == IntPtr.Zero)
                return false;

            monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
            return GetMonitorInfo(monitor, ref monitorInfo);
        }

        /// <summary>
        /// 当前显示器工作区，转换为 WPF DIP 坐标
        /// </summary>
        private bool TryGetCurrentMonitorWorkAreaDip(out Rect workAreaDip)
        {
            workAreaDip = Rect.Empty;

            var monitor = GetCurrentMonitorHandle();
            if (!TryGetMonitorInfo(monitor, out var monitorInfo))
                return false;

            _lastMonitorHandle = monitor;

            // 按目标显示器自身 DPI 换算，避免启动时窗口 DPI 尚未更新导致 MaxWidth/MaxHeight 偏小而挤压窗口
            if (WindowSizeHelper.TryGetWorkAreaDip(this, out workAreaDip))
                return true;

            var work = monitorInfo.rcWork;

            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                var toDip = source.CompositionTarget.TransformFromDevice;
                var topLeft = toDip.Transform(new System.Windows.Point(work.Left, work.Top));
                var bottomRight = toDip.Transform(new System.Windows.Point(work.Right, work.Bottom));
                workAreaDip = new Rect(topLeft, bottomRight);
                return workAreaDip.Width > 0 && workAreaDip.Height > 0;
            }

            workAreaDip = new Rect(work.Left, work.Top, work.Right - work.Left, work.Bottom - work.Top);
            return workAreaDip.Width > 0 && workAreaDip.Height > 0;
        }

        /// <summary>
        /// 窗口消息处理：最大化贴合工作区，避免混 DPI 多屏下超出屏幕/盖住任务栏
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 启动阶段移动到不同缩放的显示器时，把系统建议矩形改为正中（不标记已处理，WPF 会采用修改后的矩形）
            if (msg == WindowSizeHelper.WmDpiChanged && !_startupCentered)
                WindowSizeHelper.CenterDpiSuggestedRect(lParam, preferCursorMonitor: true);

            if (msg == WM_GETMINMAXINFO)
            {
                // 用窗口中心点取显示器，避免最大化过程中 MonitorFromWindow 偶发回到主屏
                var monitor = GetCurrentMonitorHandle();
                if (monitor == IntPtr.Zero)
                    monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (TryGetMonitorInfo(monitor, out var monitorInfo))
                {
                    var workArea = monitorInfo.rcWork;
                    var monitorArea = monitorInfo.rcMonitor;
                    var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                    // 相对当前显示器的工作区位置/尺寸（物理像素）。
                    // 副屏大于主屏时系统仍可能再次放大 ptMaxSize，真正的尺寸兜底见
                    // WM_WINDOWPOSCHANGING / CorrectMaximizedWindowBounds / MaxWidth|Height。
                    mmi.ptMaxPosition.X = workArea.Left - monitorArea.Left;
                    mmi.ptMaxPosition.Y = workArea.Top - monitorArea.Top;
                    mmi.ptMaxSize.X = workArea.Right - workArea.Left;
                    mmi.ptMaxSize.Y = workArea.Bottom - workArea.Top;
                    mmi.ptMaxTrackSize.X = mmi.ptMaxSize.X;
                    mmi.ptMaxTrackSize.Y = mmi.ptMaxSize.Y;

                    Marshal.StructureToPtr(mmi, lParam, true);
                    handled = true;
                }
            }
            else if (msg == WM_WINDOWPOSCHANGING)
            {
                // 只改 WINDOWPOS，不标记 handled，让系统继续用修正后的矩形完成布局
                TryClampWindowPosToWorkArea(lParam);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 将 WINDOWPOS 钳制到当前显示器工作区（仅在即将超出时介入）
        /// </summary>
        private bool TryClampWindowPosToWorkArea(IntPtr lParam)
        {
            var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            if ((pos.flags & SWP_NOSIZE) != 0)
                return false;

            var monitor = GetCurrentMonitorHandle();
            if (!TryGetMonitorInfo(monitor, out var monitorInfo))
                return false;

            var work = monitorInfo.rcWork;
            var width = work.Right - work.Left;
            var height = work.Bottom - work.Top;
            if (width <= 0 || height <= 0)
                return false;

            // 只纠正「比工作区还大」的错误最大化（副屏 > 主屏时的系统补偿）
            if (pos.cx <= width && pos.cy <= height)
                return false;

            pos.cx = width;
            pos.cy = height;

            if ((pos.flags & SWP_NOMOVE) == 0)
            {
                pos.x = work.Left;
                pos.y = work.Top;
            }

            Marshal.StructureToPtr(pos, lParam, true);
            return true;
        }

        //显示窗口
        private void ShowWindow(object? sender, RoutedEventArgs e)
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
        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            // 将窗口置顶
            this.Topmost = true;
            this.Activate();
            // 将置顶属性重置为 false
            Dispatcher.BeginInvoke(new Action(() => { this.Topmost = false; }));
        }

        //窗体关闭触发
        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ConfigHelper.GetConfig(ConfigKeys.ExitProgramMode) == "0")
            {
                // 将窗口隐藏并最小化到托盘
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
                this.WindowState = WindowState.Minimized;
            }
        }

        //关闭窗口
        private void ShutDownWindow(object? sender, RoutedEventArgs e)
        {
            // 关闭应用程序
            System.Windows.Application.Current.Shutdown();
        }
    }
}
