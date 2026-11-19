using GeoChemistryNexus.Controls.Startup;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Views
{
    public partial class StartWindow : Window
    {
        public Task FirstFrame => Animation.FirstFrame;

        public StartWindow()
        {
            InitializeComponent();
            SourceInitialized += (_, _) =>
            {
                ApplyWindowAppearance();
                // 先挂钩：移动到不同缩放的显示器时，Windows 会发 WM_DPICHANGED 并按比例缩放窗口，
                // 在这里把系统建议矩形改成正中，窗口显示出来就已在正确位置
                HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
                WindowSizeHelper.CenterInWorkAreaPixels(this, preferCursorMonitor: true);
            };
            // 兜底：首帧后仍有偏差时再校正（正常情况下已居中，不会产生移动）
            ContentRendered += (_, _) =>
            {
                _startupCentered = true;
                WindowSizeHelper.CenterInWorkAreaPixels(this, preferCursorMonitor: true);
            };
            Closed += (_, _) => Animation.Stop();
        }

        private bool _startupCentered;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WindowSizeHelper.WmDpiChanged && !_startupCentered)
                WindowSizeHelper.CenterDpiSuggestedRect(lParam, preferCursorMonitor: true);
            return IntPtr.Zero;
        }

        public void ReportProgress(double percent, string? status = null) => Animation.ReportProgress(percent, status);
        public void ReportProgress(double percent, StartupStatus status) => Animation.ReportProgress(percent, status);
        public Task FinishAsync() => Animation.FinishAsync();

        private void ApplyWindowAppearance()
        {
            var workArea = SystemParameters.WorkArea;
            double scale = Math.Min(1, Math.Min(workArea.Width * .94 / Width, workArea.Height * .94 / Height));
            Width *= scale;
            Height *= scale;
            int preference = 2; // DWMWCP_ROUND; older Windows versions ignore this preference.
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, ref preference, sizeof(int));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
