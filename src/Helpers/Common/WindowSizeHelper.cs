using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 按当前显示器工作区自适应普通窗口尺寸：
    /// 大屏放大（避免 4K 下窗口过小），小屏缩小并限制在工作区内。
    /// </summary>
    public static class WindowSizeHelper
    {
        /// <summary>相对 1080p 设计的默认客户区尺寸（与 MainWindow XAML 一致）。</summary>
        public const double DesignWidth = 1360;
        public const double DesignHeight = 780;

        /// <summary>尺寸换算参考工作区（1080p）。</summary>
        private const double ReferenceWorkWidth = 1920;
        private const double ReferenceWorkHeight = 1080;

        private const double WorkAreaMargin = 0.92;
        private const double MinWidth = 800;
        private const double MinHeight = 500;
        private const uint MonitorDefaultToNearest = 2;

        /// <summary>
        /// 计算窗口在指定工作区（DIP）下的推荐尺寸。
        /// 1080p 级屏幕使用设计稿原尺寸；更大屏幕按比例放大；放不下时再缩小。
        /// </summary>
        public static Size ComputePreferredSize(Rect workAreaDip, double uiScale = 1.0)
        {
            if (workAreaDip.Width <= 0 || workAreaDip.Height <= 0)
                return new Size(DesignWidth, DesignHeight);

            var maxWidth = workAreaDip.Width * WorkAreaMargin;
            var maxHeight = workAreaDip.Height * WorkAreaMargin;

            // 默认就是 1080p 设计尺寸，不因任务栏导致的工作区略矮而缩小
            double width = DesignWidth;
            double height = DesignHeight;

            var screenFactor = Math.Min(
                workAreaDip.Width / ReferenceWorkWidth,
                workAreaDip.Height / ReferenceWorkHeight);

            // 明显大于 1080p（如 1440p / 4K）时才放大窗口
            if (screenFactor > 1.15)
            {
                width = DesignWidth * screenFactor;
                height = DesignHeight * screenFactor;
            }

            // UI 放大时同步放大窗口，避免缩放后可视内容过少
            if (uiScale > 1.01)
            {
                width = Math.Max(width, DesignWidth * uiScale);
                height = Math.Max(height, DesignHeight * uiScale);
            }

            // 仅在超出工作区时缩小（小屏 / 缩放后过大）
            width = Math.Min(width, maxWidth);
            height = Math.Min(height, maxHeight);

            // 软下限：只有工作区够大时才保证最小尺寸
            if (maxWidth >= MinWidth)
                width = Math.Max(width, MinWidth);
            if (maxHeight >= MinHeight)
                height = Math.Max(height, MinHeight);

            width = Math.Min(width, maxWidth);
            height = Math.Min(height, maxHeight);

            return new Size(Math.Max(1, width), Math.Max(1, height));
        }

        /// <summary>
        /// 将窗口调整为当前显示器推荐尺寸，并可选择居中。
        /// </summary>
        /// <param name="preferCursorMonitor">启动时为 true，按鼠标所在屏定位（此时窗口矩形可能还不在目标屏）。</param>
        public static void ApplyAdaptiveSize(Window window, bool centerInWorkArea = true, bool preferCursorMonitor = false)
        {
            if (window == null || window.WindowState == WindowState.Maximized)
                return;

            if (!TryGetWorkAreaDip(window, out var workArea, preferCursorMonitor))
                return;

            var uiScale = UiScaleHelper.ComputeScale(window);
            var preferred = ComputePreferredSize(workArea, uiScale);

            window.Width = preferred.Width;
            window.Height = preferred.Height;

            if (centerInWorkArea)
            {
                window.Left = workArea.Left + (workArea.Width - preferred.Width) / 2;
                window.Top = workArea.Top + (workArea.Height - preferred.Height) / 2;
            }
            else
            {
                ClampToWorkArea(window, workArea);
            }
        }

        /// <summary>
        /// 仅保证窗口不超出工作区（小屏/跨屏时用），不主动放大。
        /// </summary>
        public static void EnsureFitsWorkArea(Window window)
        {
            if (window == null || window.WindowState == WindowState.Maximized)
                return;

            if (!TryGetWorkAreaDip(window, out var workArea))
                return;

            var maxWidth = workArea.Width * WorkAreaMargin;
            var maxHeight = workArea.Height * WorkAreaMargin;

            if (window.Width > maxWidth)
                window.Width = maxWidth;
            if (window.Height > maxHeight)
                window.Height = maxHeight;

            ClampToWorkArea(window, workArea);
        }

        /// <summary>
        /// 跨屏后：按新屏幕推荐尺寸调整，并尽量保持窗口中心落在新屏工作区内。
        /// </summary>
        public static void ApplyAdaptiveSizePreservingCenter(Window window)
        {
            if (window == null || window.WindowState == WindowState.Maximized)
                return;

            if (!TryGetWorkAreaDip(window, out var workArea))
                return;

            var centerX = window.Left + window.Width / 2;
            var centerY = window.Top + window.Height / 2;

            var uiScale = UiScaleHelper.ComputeScale(window);
            var preferred = ComputePreferredSize(workArea, uiScale);

            window.Width = preferred.Width;
            window.Height = preferred.Height;
            window.Left = centerX - preferred.Width / 2;
            window.Top = centerY - preferred.Height / 2;

            ClampToWorkArea(window, workArea);
        }

        private static void ClampToWorkArea(Window window, Rect workArea)
        {
            if (window.Width > workArea.Width)
                window.Width = Math.Max(1, workArea.Width * WorkAreaMargin);
            if (window.Height > workArea.Height)
                window.Height = Math.Max(1, workArea.Height * WorkAreaMargin);

            if (window.Left < workArea.Left)
                window.Left = workArea.Left;
            if (window.Top < workArea.Top)
                window.Top = workArea.Top;
            if (window.Left + window.Width > workArea.Right)
                window.Left = workArea.Right - window.Width;
            if (window.Top + window.Height > workArea.Bottom)
                window.Top = workArea.Bottom - window.Height;
        }

        /// <summary>
        /// 以物理像素为单位，将窗口居中到目标显示器工作区。
        /// 启动时 HWND 先创建在主屏 DPI 下，移到不同缩放的副屏/2K 屏后会触发 DPI 变化并按比例缩放窗口，
        /// 用 DIP 设置的 Left/Top 因此会偏移；DPI 稳定后直接按像素重新居中可避免该问题。
        /// </summary>
        public static void CenterInWorkAreaPixels(Window window, bool preferCursorMonitor = false)
        {
            if (window == null || window.WindowState != WindowState.Normal)
                return;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var rect))
                return;

            IntPtr monitor = IntPtr.Zero;
            if (preferCursorMonitor && GetCursorPos(out var cursor))
                monitor = MonitorFromPoint(cursor, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
                monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
                return;

            var info = new MonitorInfo { cbSize = (uint)Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return;

            var work = info.rcWork;
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            int left = work.Left + ((work.Right - work.Left) - width) / 2;
            int top = work.Top + ((work.Bottom - work.Top) - height) / 2;

            if (left == rect.Left && top == rect.Top)
                return;

            SetWindowPos(hwnd, IntPtr.Zero, left, top, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
        }

        public const int WmDpiChanged = 0x02E0;

        /// <summary>
        /// 在 WM_DPICHANGED 消息里把系统建议的新窗口矩形（lParam 指向 RECT，物理像素）改为目标显示器工作区正中。
        /// WPF 随后会直接采用这个矩形，窗口一出现就在正中，不会先偏后跳。
        /// 在 HwndSource 钩子里调用，且不要把消息标记为已处理。
        /// </summary>
        public static void CenterDpiSuggestedRect(IntPtr lParam, bool preferCursorMonitor = true)
        {
            if (lParam == IntPtr.Zero)
                return;

            var rect = Marshal.PtrToStructure<RectApi>(lParam);

            IntPtr monitor = IntPtr.Zero;
            if (preferCursorMonitor && GetCursorPos(out var cursor))
                monitor = MonitorFromPoint(cursor, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
            {
                var center = new PointApi
                {
                    X = rect.Left + (rect.Right - rect.Left) / 2,
                    Y = rect.Top + (rect.Bottom - rect.Top) / 2
                };
                monitor = MonitorFromPoint(center, MonitorDefaultToNearest);
            }
            if (monitor == IntPtr.Zero)
                return;

            var info = new MonitorInfo { cbSize = (uint)Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return;

            var work = info.rcWork;
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            int left = work.Left + ((work.Right - work.Left) - width) / 2;
            int top = work.Top + ((work.Bottom - work.Top) - height) / 2;

            rect.Left = left;
            rect.Top = top;
            rect.Right = left + width;
            rect.Bottom = top + height;
            Marshal.StructureToPtr(rect, lParam, false);
        }

        public static bool TryGetWorkAreaDip(Window window, out Rect workAreaDip, bool preferCursorMonitor = false)
        {
            workAreaDip = Rect.Empty;

            IntPtr monitor = IntPtr.Zero;

            if (preferCursorMonitor && GetCursorPos(out var cursor))
                monitor = MonitorFromPoint(cursor, MonitorDefaultToNearest);

            if (monitor == IntPtr.Zero)
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    if (GetWindowRect(hwnd, out var rect))
                    {
                        var center = new PointApi
                        {
                            X = rect.Left + (rect.Right - rect.Left) / 2,
                            Y = rect.Top + (rect.Bottom - rect.Top) / 2
                        };
                        monitor = MonitorFromPoint(center, MonitorDefaultToNearest);
                    }
                    else
                    {
                        monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
                    }
                }
                else if (GetCursorPos(out cursor))
                {
                    monitor = MonitorFromPoint(cursor, MonitorDefaultToNearest);
                }
                else
                {
                    workAreaDip = SystemParameters.WorkArea;
                    return workAreaDip.Width > 0 && workAreaDip.Height > 0;
                }
            }

            if (monitor == IntPtr.Zero)
                return false;

            var info = new MonitorInfo { cbSize = (uint)Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return false;

            var work = info.rcWork;

            // 优先按目标显示器自身 DPI 换算：启动时窗口句柄还在主屏 DPI 下，
            // 用窗口当前 DPI 换算会得到错误的 DIP 尺寸，窗口显示后又要被二次调整（表现为闪一下）
            double monitorScale = GetMonitorDipScale(monitor);
            if (monitorScale > 0)
            {
                workAreaDip = new Rect(
                    work.Left / monitorScale,
                    work.Top / monitorScale,
                    (work.Right - work.Left) / monitorScale,
                    (work.Bottom - work.Top) / monitorScale);
                return workAreaDip.Width > 0 && workAreaDip.Height > 0;
            }

            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget != null)
            {
                var toDip = source.CompositionTarget.TransformFromDevice;
                var topLeft = toDip.Transform(new System.Windows.Point(work.Left, work.Top));
                var bottomRight = toDip.Transform(new System.Windows.Point(work.Right, work.Bottom));
                workAreaDip = new Rect(topLeft, bottomRight);
            }
            else
            {
                var scale = GetFallbackDipScale(window);
                workAreaDip = new Rect(
                    work.Left / scale,
                    work.Top / scale,
                    (work.Right - work.Left) / scale,
                    (work.Bottom - work.Top) / scale);
            }

            return workAreaDip.Width > 0 && workAreaDip.Height > 0;
        }

        /// <summary>显示器缩放比例（有效 DPI / 96），失败返回 0。</summary>
        private static double GetMonitorDipScale(IntPtr monitor)
        {
            try
            {
                if (GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0 && dpiX > 0)
                    return dpiX / 96.0;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
            }

            return 0;
        }

        private static double GetFallbackDipScale(Window window)
        {
            try
            {
                return VisualTreeHelper.GetDpi(window).DpiScaleX;
            }
            catch
            {
                return 1.0;
            }
        }

        #region Win32

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(PointApi pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RectApi lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out PointApi lpPoint);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct PointApi
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RectApi
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public uint cbSize;
            public RectApi rcMonitor;
            public RectApi rcWork;
            public uint dwFlags;
        }

        #endregion
    }
}
