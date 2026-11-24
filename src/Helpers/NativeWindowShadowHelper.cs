using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 无边框窗口（WindowStyle=None + WindowChrome，不使用 AllowsTransparency）交给 DWM 绘制原生阴影，
    /// 与 EPMA 模板编辑窗口、新建图解窗口一致。
    /// </summary>
    public static class NativeWindowShadowHelper
    {
        private const int DwmwaNcRenderingPolicy = 2;
        private const int DwmncrpEnabled = 2;

        /// <summary>在窗口句柄创建后启用 DWM 原生阴影。</summary>
        public static void Attach(Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            window.SourceInitialized += (_, _) =>
            {
                int policy = DwmncrpEnabled;
                DwmSetWindowAttribute(new WindowInteropHelper(window).Handle, DwmwaNcRenderingPolicy, ref policy, sizeof(int));
            };
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
