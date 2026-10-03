using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 提供窗口激活与所有者焦点保留的辅助方法。
    /// 解决在关闭子窗口或非模态小组件窗口时，由于 Windows 操作系统的全局 Z-Order 回退机制，
    /// 导致主窗体失去前台焦点并被底层其他第三方软件遮挡的问题。
    /// </summary>
    public static class WindowActivationHelper
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        /// <summary>
        /// 平稳将焦点和激活状态交还给所属主窗体（或指定的 Owner 窗体）。
        /// </summary>
        /// <param name="owner">目标所有者窗体，为 null 时自动回退到 Application.Current.MainWindow</param>
        public static void RestoreOwnerFocus(Window? owner = null)
        {
            try
            {
                owner ??= Application.Current?.MainWindow;
                if (owner == null)
                    return;

                // 若主窗体已最小化或不可见，不强制拉起，尊重用户的最小化意图
                if (!owner.IsVisible || owner.WindowState == WindowState.Minimized)
                    return;

                var hwnd = new WindowInteropHelper(owner).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    BringWindowToTop(hwnd);
                    SetForegroundWindow(hwnd);
                }

                owner.Activate();
                owner.Focus();
            }
            catch
            {
                // 忽略非致命的 UI 调度异常
            }
        }

        /// <summary>
        /// 为指定的窗体附加“关闭时所有者焦点保留”机制。
        /// 在窗体 Closing（句柄尚未销毁且拥有前台权限）以及 Closed（系统彻底注销句柄后）两道时机，
        /// 确保激活状态无缝平稳交还给 Owner，避免系统焦点被外部应用抢占。
        /// </summary>
        /// <param name="window">子窗体或非模态小组件窗体</param>
        /// <param name="preferredOwner">期望的 Owner 窗体</param>
        public static void AttachOwnerFocusPreservation(Window window, Window? preferredOwner = null)
        {
            if (window == null)
                return;

            var targetOwner = preferredOwner ?? window.Owner ?? Application.Current?.MainWindow;
            if (window.Owner == null && targetOwner != null && targetOwner != window)
            {
                try
                {
                    window.Owner = targetOwner;
                }
                catch
                {
                    // 忽略若 Owner 设置不符合 WPF 限制时的异常
                }
            }

            // 1. 在 Closing 触发时（此时子窗体依然持有当前前台权限，调用 SetForegroundWindow 最可靠）
            window.Closing += (s, e) =>
            {
                if (e.Cancel)
                    return;

                var owner = window.Owner ?? preferredOwner ?? Application.Current?.MainWindow;
                RestoreOwnerFocus(owner);
            };

            // 2. 在 Closed 触发后（窗体已从系统注销，在 UI 线程下一个消息循环进行二次兜底确认）
            window.Closed += (s, e) =>
            {
                var owner = window.Owner ?? preferredOwner ?? Application.Current?.MainWindow;
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        RestoreOwnerFocus(owner);
                    }), DispatcherPriority.Input);
                }
            };
        }
    }
}
