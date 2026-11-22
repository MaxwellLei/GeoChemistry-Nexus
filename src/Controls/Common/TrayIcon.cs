using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 系统托盘图标（通知区域图标）。
    /// 在 XAML 中放置于任意可视容器内即可；右键弹出 <see cref="FrameworkElement.ContextMenu"/>，
    /// 菜单外观沿用应用级 ContextMenu / MenuItem 样式。控件本身不可见，仅负责托盘图标的生命周期。
    /// 仅依赖 Win32 Shell_NotifyIcon，不引入 WinForms / System.Drawing。
    /// </summary>
    public sealed class TrayIcon : FrameworkElement, IDisposable
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(TrayIcon),
            new PropertyMetadata(null, OnIconChanged));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TrayIcon),
            new PropertyMetadata(string.Empty, OnTextChanged));

        /// <summary>托盘图标图片。</summary>
        public ImageSource? Icon
        {
            get => (ImageSource?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>鼠标悬停时显示的提示文字（系统限制最多 127 个字符）。</summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>左键单击托盘图标。</summary>
        public event EventHandler? Click;

        /// <summary>左键双击托盘图标。</summary>
        public event EventHandler? DoubleClick;

        private const int CallbackMessage = WmUser + 1;
        private const int WmUser = 0x0400;
        private const int WmLButtonUp = 0x0202;
        private const int WmLButtonDblClk = 0x0203;
        private const int WmRButtonUp = 0x0205;

        private const uint NimAdd = 0x0;
        private const uint NimModify = 0x1;
        private const uint NimDelete = 0x2;

        private const uint NifMessage = 0x1;
        private const uint NifIcon = 0x2;
        private const uint NifTip = 0x4;

        private const int SmCxSmIcon = 49;
        private const int SmCySmIcon = 50;

        private readonly uint _taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
        private readonly uint _iconId = 1;

        private HwndSource? _messageWindow;
        private IntPtr _iconHandle = IntPtr.Zero;
        private bool _isAdded;
        private bool _isDisposed;

        public TrayIcon()
        {
            Loaded += OnLoaded;
            Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
        }

        #region 生命周期

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
                return;

            EnsureMessageWindow();
            AddOrUpdateIcon();
        }

        private void OnDispatcherShutdownStarted(object? sender, EventArgs e) => Dispose();

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Loaded -= OnLoaded;
            Dispatcher.ShutdownStarted -= OnDispatcherShutdownStarted;

            RemoveIcon();

            if (_messageWindow != null)
            {
                _messageWindow.RemoveHook(WndProc);
                _messageWindow.Dispose();
                _messageWindow = null;
            }

            ReleaseIconHandle();
        }

        private void EnsureMessageWindow()
        {
            if (_messageWindow != null)
                return;

            // 纯消息窗口：不显示，只用来接收托盘回调消息
            var parameters = new HwndSourceParameters("GeoChemistryNexus.TrayIcon")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE
            };
            _messageWindow = new HwndSource(parameters);
            _messageWindow.AddHook(WndProc);
        }

        #endregion

        #region 属性变化

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tray = (TrayIcon)d;
            tray.ReleaseIconHandle();
            tray.AddOrUpdateIcon();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TrayIcon)d).AddOrUpdateIcon();
        }

        #endregion

        #region 托盘图标

        private void AddOrUpdateIcon()
        {
            if (_isDisposed || _messageWindow == null)
                return;

            if (_iconHandle == IntPtr.Zero && Icon is BitmapSource bitmap)
                _iconHandle = CreateIconHandle(bitmap);

            var data = CreateNotifyIconData();
            if (_isAdded)
            {
                Shell_NotifyIcon(NimModify, ref data);
            }
            else if (Shell_NotifyIcon(NimAdd, ref data))
            {
                _isAdded = true;
            }
        }

        private void RemoveIcon()
        {
            if (!_isAdded || _messageWindow == null)
                return;

            var data = CreateNotifyIconData();
            Shell_NotifyIcon(NimDelete, ref data);
            _isAdded = false;
        }

        private NotifyIconData CreateNotifyIconData()
        {
            string tip = Text ?? string.Empty;
            if (tip.Length > 127)
                tip = tip.Substring(0, 127);

            return new NotifyIconData
            {
                cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
                hWnd = _messageWindow!.Handle,
                uID = _iconId,
                uFlags = NifMessage | NifIcon | NifTip,
                uCallbackMessage = CallbackMessage,
                hIcon = _iconHandle,
                szTip = tip
            };
        }

        /// <summary>
        /// 把 WPF 的 BitmapSource 转为系统托盘使用的 HICON（缩放到系统小图标尺寸，保留透明通道）。
        /// </summary>
        private static IntPtr CreateIconHandle(BitmapSource source)
        {
            int width = Math.Max(16, GetSystemMetrics(SmCxSmIcon));
            int height = Math.Max(16, GetSystemMetrics(SmCySmIcon));

            source = SelectBestFrame(source, width, height);

            BitmapSource scaled = source;
            if (source.PixelWidth != width || source.PixelHeight != height)
            {
                scaled = new TransformedBitmap(
                    source,
                    new ScaleTransform((double)width / source.PixelWidth, (double)height / source.PixelHeight));
            }

            var converted = new FormatConvertedBitmap(scaled, PixelFormats.Bgra32, null, 0);
            int stride = converted.PixelWidth * 4;
            var pixels = new byte[stride * converted.PixelHeight];
            converted.CopyPixels(pixels, stride, 0);

            IntPtr colorBitmap = CreateBitmap(converted.PixelWidth, converted.PixelHeight, 1, 32, pixels);
            // 单色掩码位图（每行按 16 位对齐）；全 0 表示完全由彩色位图的 Alpha 通道决定透明度
            var maskBits = new byte[((converted.PixelWidth + 15) / 16 * 2) * converted.PixelHeight];
            IntPtr maskBitmap = CreateBitmap(converted.PixelWidth, converted.PixelHeight, 1, 1, maskBits);
            if (colorBitmap == IntPtr.Zero || maskBitmap == IntPtr.Zero)
            {
                if (colorBitmap != IntPtr.Zero) DeleteObject(colorBitmap);
                if (maskBitmap != IntPtr.Zero) DeleteObject(maskBitmap);
                return IntPtr.Zero;
            }

            try
            {
                var iconInfo = new IconInfo
                {
                    fIcon = true,
                    hbmColor = colorBitmap,
                    hbmMask = maskBitmap
                };
                return CreateIconIndirect(ref iconInfo);
            }
            finally
            {
                DeleteObject(colorBitmap);
                DeleteObject(maskBitmap);
            }
        }

        /// <summary>
        /// 多尺寸 .ico 会解码出多个帧：优先选不小于目标尺寸的最小一帧（缩小比放大清晰），
        /// 没有足够大的帧时退而选最大的一帧。单帧图片原样返回。
        /// </summary>
        private static BitmapSource SelectBestFrame(BitmapSource source, int width, int height)
        {
            var frames = (source as BitmapFrame)?.Decoder?.Frames;
            if (frames == null || frames.Count <= 1)
                return source;

            BitmapFrame? best = null;
            foreach (var frame in frames)
            {
                bool frameFits = frame.PixelWidth >= width && frame.PixelHeight >= height;
                if (best == null)
                {
                    best = frame;
                    continue;
                }

                bool bestFits = best.PixelWidth >= width && best.PixelHeight >= height;
                if (frameFits && !bestFits)
                    best = frame;
                else if (frameFits && bestFits && frame.PixelWidth < best.PixelWidth)
                    best = frame;
                else if (!frameFits && !bestFits && frame.PixelWidth > best.PixelWidth)
                    best = frame;
            }

            return best ?? source;
        }

        private void ReleaseIconHandle()
        {
            if (_iconHandle != IntPtr.Zero)
            {
                DestroyIcon(_iconHandle);
                _iconHandle = IntPtr.Zero;
            }
        }

        #endregion

        #region 消息处理

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == CallbackMessage)
            {
                switch (lParam.ToInt64() & 0xFFFF)
                {
                    case WmLButtonUp:
                        Click?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                    case WmLButtonDblClk:
                        DoubleClick?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                    case WmRButtonUp:
                        ShowContextMenu();
                        handled = true;
                        break;
                }
            }
            else if (msg == (int)_taskbarCreatedMessage && _taskbarCreatedMessage != 0)
            {
                // 资源管理器重启后任务栏重建，需要重新添加托盘图标
                _isAdded = false;
                AddOrUpdateIcon();
            }

            return IntPtr.Zero;
        }

        private void ShowContextMenu()
        {
            var menu = ContextMenu;
            if (menu == null)
                return;

            menu.PlacementTarget = this;
            menu.Placement = PlacementMode.MousePoint;
            menu.IsOpen = true;

            // 托盘菜单需要先把菜单窗口设为前台窗口，否则点击菜单外部时菜单不会自动关闭
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (PresentationSource.FromVisual(menu) is HwndSource menuSource)
                    SetForegroundWindow(menuSource.Handle);
            }));
        }

        #endregion

        #region Win32

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NotifyIconData
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;

            public uint dwState;
            public uint dwStateMask;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;

            public uint uTimeoutOrVersion;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;

            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IconInfo
        {
            [MarshalAs(UnmanagedType.Bool)]
            public bool fIcon;
            public uint xHotspot;
            public uint yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string message);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconIndirect(ref IconInfo iconInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr icon);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, byte[]? bits);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr obj);

        #endregion
    }
}
