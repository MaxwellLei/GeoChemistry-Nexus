using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 弹出窗体（小组件、新建模板窗体、各类对话框）的定位修复。
    /// 与主窗体同样的问题：HWND 先创建在主屏 DPI 下，移到不同缩放 / 分辨率的显示器后，
    /// WPF 用 DIP 计算的 CenterOwner / CenterScreen 位置会偏向右下。
    /// 这里改为按物理像素直接居中到所属主窗体（或其所在显示器工作区）：
    /// 1. SourceInitialized：先把窗口移到目标位置；
    /// 2. WM_DPICHANGED：把系统建议矩形改为目标居中矩形（窗口出现时即在正中，不会先偏后跳）；
    /// 3. ContentRendered：DPI 稳定后按像素兜底再居中一次。
    /// 仅对 CenterOwner / CenterScreen 的窗口生效；Manual 窗口保持原样。
    /// </summary>
    public static class ChildWindowPlacementHelper
    {
        private const int WmDpiChanged = 0x02E0;
        private const uint MonitorDefaultToNearest = 2;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        private static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached(
            "IsPlacementAttached", typeof(bool), typeof(ChildWindowPlacementHelper), new PropertyMetadata(false));

        public static void Attach(Window window)
        {
            if (window == null || (bool)window.GetValue(IsAttachedProperty))
                return;
            window.SetValue(IsAttachedProperty, true);

            bool placed = false;
            HwndSource? source = null;

            IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (message == WmDpiChanged && !placed && ShouldPlace(window))
                    AdjustDpiSuggestedRect(window, lParam);
                return IntPtr.Zero;
            }

            void Detach()
            {
                source?.RemoveHook(Hook);
                source = null;
            }

            window.SourceInitialized += (_, _) =>
            {
                if (!ShouldPlace(window))
                    return;

                source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                source?.AddHook(Hook);
                CenterNow(window);
            };

            window.ContentRendered += (_, _) =>
            {
                if (placed)
                    return;
                placed = true;
                Detach();

                if (ShouldPlace(window))
                    CenterNow(window);
            };

            window.Closed += (_, _) => Detach();
        }

        private static bool ShouldPlace(Window window) =>
            window.WindowState == WindowState.Normal
            && window.WindowStartupLocation != WindowStartupLocation.Manual;

        /// <summary>优先所属窗体；CenterOwner 且没有 Owner 时退回主窗体。</summary>
        private static Window? ResolveReference(Window window)
        {
            Window? reference = window.Owner;
            if (reference == null && window.WindowStartupLocation == WindowStartupLocation.CenterOwner)
                reference = Application.Current?.MainWindow;

            if (reference == null || ReferenceEquals(reference, window))
                return null;
            return reference;
        }

        private static void CenterNow(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            // 最多两轮：移动到另一块显示器时系统会触发 DPI 变化并改变窗口像素尺寸，需要按新尺寸再对一次
            for (int i = 0; i < 2; i++)
            {
                if (!GetWindowRect(hwnd, out var rect))
                    return;

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (!TryComputeTarget(window, width, height, out int left, out int top))
                    return;

                if (Math.Abs(left - rect.Left) <= 1 && Math.Abs(top - rect.Top) <= 1)
                    return;

                SetWindowPos(hwnd, IntPtr.Zero, left, top, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
            }
        }

        private static void AdjustDpiSuggestedRect(Window window, IntPtr lParam)
        {
            if (lParam == IntPtr.Zero)
                return;

            var rect = Marshal.PtrToStructure<RectApi>(lParam);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (!TryComputeTarget(window, width, height, out int left, out int top))
                return;

            rect.Left = left;
            rect.Top = top;
            rect.Right = left + width;
            rect.Bottom = top + height;
            Marshal.StructureToPtr(rect, lParam, false);
        }

        /// <summary>按物理像素计算目标左上角：居中到参考窗体，并限制在其所在显示器工作区内。</summary>
        private static bool TryComputeTarget(Window window, int width, int height, out int left, out int top)
        {
            left = top = 0;

            RectApi? referenceRect = null;
            IntPtr monitor = IntPtr.Zero;

            var reference = ResolveReference(window);
            var referenceHwnd = reference != null ? new WindowInteropHelper(reference).Handle : IntPtr.Zero;
            if (referenceHwnd != IntPtr.Zero
                && IsWindowVisible(referenceHwnd)
                && !IsIconic(referenceHwnd)
                && GetWindowRect(referenceHwnd, out var rect))
            {
                referenceRect = rect;
                monitor = MonitorFromWindow(referenceHwnd, MonitorDefaultToNearest);
            }

            if (monitor == IntPtr.Zero)
            {
                // 没有可用的参考窗体：落到主窗体所在显示器，再不行用鼠标所在显示器
                var main = Application.Current?.MainWindow;
                var mainHwnd = main != null ? new WindowInteropHelper(main).Handle : IntPtr.Zero;
                if (mainHwnd != IntPtr.Zero && IsWindowVisible(mainHwnd) && !IsIconic(mainHwnd))
                    monitor = MonitorFromWindow(mainHwnd, MonitorDefaultToNearest);
                else if (GetCursorPos(out var cursor))
                    monitor = MonitorFromPoint(cursor, MonitorDefaultToNearest);
            }

            if (monitor == IntPtr.Zero)
                return false;

            var info = new MonitorInfo { cbSize = (uint)Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return false;

            var work = info.rcWork;
            int centerX, centerY;
            if (referenceRect is { } r)
            {
                centerX = r.Left + (r.Right - r.Left) / 2;
                centerY = r.Top + (r.Bottom - r.Top) / 2;
            }
            else
            {
                centerX = work.Left + (work.Right - work.Left) / 2;
                centerY = work.Top + (work.Bottom - work.Top) / 2;
            }

            left = centerX - width / 2;
            top = centerY - height / 2;

            // 限制在工作区内（窗口比工作区大时贴左上角）
            left = Math.Max(work.Left, Math.Min(left, work.Right - width));
            top = Math.Max(work.Top, Math.Min(top, work.Bottom - height));
            return true;
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

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

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
