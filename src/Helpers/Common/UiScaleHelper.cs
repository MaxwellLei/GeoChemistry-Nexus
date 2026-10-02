using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 应用级 UI 缩放：在高分辨率且系统 DPI 偏低时放大界面（近似系统缩放效果）。
    /// Auto 跟随窗口所在显示器；固定档位全局生效。
    /// </summary>
    public static class UiScaleHelper
    {
        public const string ConfigKey = "ui_scale_mode";

        private const double DesignCaptionHeight = 40;
        private const double DesignResizeBorder = 15;
        private const uint MonitorDefaultToNearest = 2;

        private static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached(
                "IsUiScaleAttached",
                typeof(bool),
                typeof(UiScaleHelper),
                new PropertyMetadata(false));

        private static readonly DependencyProperty LastMonitorProperty =
            DependencyProperty.RegisterAttached(
                "LastUiScaleMonitor",
                typeof(IntPtr),
                typeof(UiScaleHelper),
                new PropertyMetadata(IntPtr.Zero));

        private static readonly DependencyProperty LastScaleProperty =
            DependencyProperty.RegisterAttached(
                "LastUiScale",
                typeof(double),
                typeof(UiScaleHelper),
                new PropertyMetadata(1.0));

        private static readonly List<WeakReference<Window>> AttachedWindows = new();

        public static UiScaleMode GetMode()
        {
            return ParseMode(ConfigHelper.GetConfig(ConfigKey));
        }

        public static void SetMode(UiScaleMode mode)
        {
            ConfigHelper.SetConfig(ConfigKey, ModeToConfigValue(mode));
            RefreshAllAttachedWindows();
            WeakReferenceMessenger.Default.Send(new UiScaleModeChangedMessage(mode));
        }

        public static UiScaleMode ParseMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return UiScaleMode.Auto;

            if (value.Equals("Auto", StringComparison.OrdinalIgnoreCase) || value == "0")
                return UiScaleMode.Auto;

            if (int.TryParse(value, out int percent))
            {
                return percent switch
                {
                    100 => UiScaleMode.Percent100,
                    125 => UiScaleMode.Percent125,
                    150 => UiScaleMode.Percent150,
                    200 => UiScaleMode.Percent200,
                    _ => UiScaleMode.Auto
                };
            }

            return value.ToLowerInvariant() switch
            {
                "100" or "p100" or "percent100" => UiScaleMode.Percent100,
                "125" or "p125" or "percent125" => UiScaleMode.Percent125,
                "150" or "p150" or "percent150" => UiScaleMode.Percent150,
                "200" or "p200" or "percent200" => UiScaleMode.Percent200,
                _ => UiScaleMode.Auto
            };
        }

        public static string ModeToConfigValue(UiScaleMode mode) => mode switch
        {
            UiScaleMode.Percent100 => "100",
            UiScaleMode.Percent125 => "125",
            UiScaleMode.Percent150 => "150",
            UiScaleMode.Percent200 => "200",
            _ => "Auto"
        };

        public static double ModeToFixedScale(UiScaleMode mode) => mode switch
        {
            UiScaleMode.Percent100 => 1.0,
            UiScaleMode.Percent125 => 1.25,
            UiScaleMode.Percent150 => 1.5,
            UiScaleMode.Percent200 => 2.0,
            _ => 1.0
        };

        /// <summary>
        /// 计算窗口当前应使用的 UI 缩放倍率。
        /// </summary>
        public static double ComputeScale(Window window)
        {
            var mode = GetMode();
            if (mode != UiScaleMode.Auto)
                return ModeToFixedScale(mode);

            return ComputeAutoScale(window);
        }

        /// <summary>
        /// 将窗口接入 UI 缩放（内容 LayoutTransform + WindowChrome 同步）。
        /// </summary>
        public static void Attach(Window window)
        {
            if (window == null || (bool)window.GetValue(IsAttachedProperty))
                return;

            window.SetValue(IsAttachedProperty, true);
            AttachedWindows.Add(new WeakReference<Window>(window));

            window.SourceInitialized += (_, _) => Apply(window);
            window.Loaded += (_, _) => Apply(window);
            window.LocationChanged += (_, _) => ApplyIfMonitorChanged(window);
            window.DpiChanged += (_, _) => Apply(window);
            window.StateChanged += (_, _) => Apply(window);

            if (window.IsLoaded || PresentationSource.FromVisual(window) != null)
                Apply(window);
        }

        public static void RefreshAllAttachedWindows()
        {
            for (int i = AttachedWindows.Count - 1; i >= 0; i--)
            {
                if (!AttachedWindows[i].TryGetTarget(out var window) || window == null)
                {
                    AttachedWindows.RemoveAt(i);
                    continue;
                }

                Apply(window);
            }
        }

        public static void Apply(Window window)
        {
            if (window?.Content is not FrameworkElement content)
                return;

            var scale = ComputeScale(window);
            var lastScale = (double)window.GetValue(LastScaleProperty);
            if (Math.Abs(lastScale - scale) < 0.001 && content.LayoutTransform is ScaleTransform)
            {
                SyncWindowChrome(window, scale);
                return;
            }

            if (content.LayoutTransform is ScaleTransform existing)
            {
                existing.ScaleX = scale;
                existing.ScaleY = scale;
            }
            else
            {
                content.LayoutTransform = new ScaleTransform(scale, scale);
            }

            window.SetValue(LastScaleProperty, scale);
            window.SetValue(LastMonitorProperty, GetMonitorHandle(window));
            SyncWindowChrome(window, scale);
        }

        private static void ApplyIfMonitorChanged(Window window)
        {
            var monitor = GetMonitorHandle(window);
            var last = (IntPtr)window.GetValue(LastMonitorProperty);
            if (monitor != IntPtr.Zero && monitor != last)
                Apply(window);
        }

        private static void SyncWindowChrome(Window window, double scale)
        {
            var chrome = WindowChrome.GetWindowChrome(window);
            if (chrome == null)
                return;

            chrome.CaptionHeight = DesignCaptionHeight * scale;
            chrome.ResizeBorderThickness = new Thickness(DesignResizeBorder * scale);
        }

        private static double ComputeAutoScale(Window window)
        {
            if (!TryGetMonitorMetrics(window, out int pixelHeight, out double systemScale))
                return 1.0;

            // 目标：高分屏在系统缩放不足时，补到接近 Windows 推荐观感
            double targetTotalScale = 1.0;
            if (pixelHeight >= 2160)
                targetTotalScale = 1.5; // 4K 类，目标约 150%
            else if (pixelHeight >= 1440)
                targetTotalScale = 1.25; // 1440p 类

            var appScale = targetTotalScale / Math.Max(systemScale, 0.5);
            appScale = Math.Clamp(appScale, 1.0, 2.0);

            // 收敛到常见档位，避免 1.333 这类难看倍率
            return SnapScale(appScale);
        }

        private static double SnapScale(double scale)
        {
            if (scale < 1.12) return 1.0;
            if (scale < 1.37) return 1.25;
            if (scale < 1.75) return 1.5;
            return 2.0;
        }

        private static bool TryGetMonitorMetrics(Window window, out int pixelHeight, out double systemScale)
        {
            pixelHeight = 0;
            systemScale = 1.0;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                // 句柄未创建时，用系统主屏工作区高度与当前视觉 DPI 兜底
                pixelHeight = (int)Math.Round(SystemParameters.PrimaryScreenHeight * GetVisualScale(window));
                systemScale = GetVisualScale(window);
                return pixelHeight > 0;
            }

            var monitor = GetMonitorHandle(window);
            if (monitor == IntPtr.Zero)
                return false;

            var info = new MonitorInfo { cbSize = (uint)Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return false;

            pixelHeight = info.rcMonitor.Bottom - info.rcMonitor.Top;
            systemScale = GetVisualScale(window);
            return pixelHeight > 0;
        }

        private static double GetVisualScale(Window window)
        {
            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget != null)
                return source.CompositionTarget.TransformToDevice.M11;

            try
            {
                return VisualTreeHelper.GetDpi(window).DpiScaleX;
            }
            catch
            {
                return 1.0;
            }
        }

        private static IntPtr GetMonitorHandle(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                return IntPtr.Zero;

            if (GetWindowRect(hwnd, out var rect))
            {
                var center = new PointApi
                {
                    X = rect.Left + (rect.Right - rect.Left) / 2,
                    Y = rect.Top + (rect.Bottom - rect.Top) / 2
                };
                var fromPoint = MonitorFromPoint(center, MonitorDefaultToNearest);
                if (fromPoint != IntPtr.Zero)
                    return fromPoint;
            }

            return MonitorFromWindow(hwnd, MonitorDefaultToNearest);
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
