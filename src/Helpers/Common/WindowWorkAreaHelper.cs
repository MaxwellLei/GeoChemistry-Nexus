using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>Keep custom-chrome windows inside the current monitor's work area.</summary>
    public static class WindowWorkAreaHelper
    {
        private static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached(
            "IsAttached", typeof(bool), typeof(WindowWorkAreaHelper), new PropertyMetadata(false));

        public static void Attach(Window window)
        {
            if ((bool)window.GetValue(IsAttachedProperty)) return;
            window.SetValue(IsAttachedProperty, true);
            HwndSource? source = null;
            void CorrectBounds()
            {
                if (window.WindowState != WindowState.Maximized || source == null) return;
                if (!TryGetMonitor(source.Handle, out var monitor)) return;
                var work = monitor.Work;
                if (GetWindowRect(source.Handle, out var bounds) && bounds.Equals(work)) return;
                SetWindowPos(source.Handle, IntPtr.Zero, work.Left, work.Top,
                    work.Right - work.Left, work.Bottom - work.Top, 0x0004 | 0x0010);
            }
            IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (message == 0x0024 && TryGetMonitor(hwnd, out var monitor)) // WM_GETMINMAXINFO
                {
                    var limits = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                    limits.MaxPosition.X = monitor.Work.Left - monitor.Bounds.Left;
                    limits.MaxPosition.Y = monitor.Work.Top - monitor.Bounds.Top;
                    limits.MaxSize.X = monitor.Work.Right - monitor.Work.Left;
                    limits.MaxSize.Y = monitor.Work.Bottom - monitor.Work.Top;
                    Marshal.StructureToPtr(limits, lParam, false);
                    handled = true;
                }
                else if (message == 0x0046 && window.WindowState == WindowState.Maximized
                    && IsZoomed(hwnd) && !IsIconic(hwnd) && TryGetMonitor(hwnd, out monitor)) // WM_WINDOWPOSCHANGING
                {
                    // Windows can compensate the maximum size again on a larger secondary monitor.
                    var position = Marshal.PtrToStructure<WindowPosition>(lParam);
                    if ((position.Flags & 0x0001) == 0) // SWP_NOSIZE
                    {
                        position.X = monitor.Work.Left;
                        position.Y = monitor.Work.Top;
                        position.Width = monitor.Work.Right - monitor.Work.Left;
                        position.Height = monitor.Work.Bottom - monitor.Work.Top;
                        Marshal.StructureToPtr(position, lParam, false);
                    }
                }
                return IntPtr.Zero;
            }
            void Initialize(object? sender, EventArgs args)
            {
                source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                source?.AddHook(Hook);
            }
            window.SourceInitialized += Initialize;
            window.StateChanged += (_, _) => window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(CorrectBounds));
            window.DpiChanged += (_, _) => window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(CorrectBounds));
            window.Closed += (_, _) =>
            {
                source?.RemoveHook(Hook);
                source = null;
            };
            if (new WindowInteropHelper(window).Handle != IntPtr.Zero) Initialize(null, EventArgs.Empty);
        }

        private static bool TryGetMonitor(IntPtr hwnd, out MonitorInfo info)
        {
            info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            return GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo { public Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo { public int Size; public NativeRect Bounds, Work; public uint Flags; }
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowPosition { public IntPtr Hwnd, InsertAfter; public int X, Y, Width, Height; public uint Flags; }

        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    }
}
