using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 从屏幕上取一个像素。蒙层只负责接收鼠标，颜色直接读光标所在的物理像素。
    /// 右键或 Esc 取消。
    /// </summary>
    public class ScreenColorDropperWindow : Window
    {
        private const int SrcCopy = 0x00CC0020;
        private const uint WdaExcludeFromCapture = 0x00000011;
        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        private readonly Border _chip;
        private readonly Canvas _canvas;
        private readonly FrameworkElement _chipFrame;
        private IntPtr _hwnd;
        private IntPtr _screenDc;
        private IntPtr _memoryDc;
        private IntPtr _dib;
        private IntPtr _dibBits;
        private IntPtr _oldObject;
        private bool _finished;

        public Color? Picked { get; private set; }

        public ScreenColorDropperWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            ShowActivated = true;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Cursor = Cursors.Cross;
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = Math.Max(1, SystemParameters.VirtualScreenWidth);
            Height = Math.Max(1, SystemParameters.VirtualScreenHeight);

            _chip = new Border
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(4),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Background = Brushes.White
            };
            _chipFrame = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                IsHitTestVisible = false,
                Child = _chip,
                Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 1, Opacity = 0.35, Color = Colors.Black }
            };
            _canvas = new Canvas { IsHitTestVisible = false };
            _canvas.Children.Add(_chipFrame);
            Content = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255)),
                Children = { _canvas }
            };

            SourceInitialized += (_, _) =>
            {
                _hwnd = new WindowInteropHelper(this).Handle;
                SetWindowDisplayAffinity(_hwnd, WdaExcludeFromCapture);
            };
            MouseMove += (_, _) => FollowCursor();
            MouseLeftButtonDown += (sender, e) =>
            {
                Finish(ReadCursorPixel(out _));
                e.Handled = true;
            };
            MouseRightButtonDown += (_, e) =>
            {
                Finish(null);
                e.Handled = true;
            };
            PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Escape) return;
                Finish(null);
                e.Handled = true;
            };
            Loaded += (_, _) => FollowCursor();
            Closed += (_, _) => ReleasePixelBuffer();
        }

        private void FollowCursor()
        {
            if (_finished) return;
            var color = ReadCursorPixel(out var client);
            if (color == null) return;
            _chip.Background = new SolidColorBrush(color.Value);
            MoveChip(client);
        }

        /// <summary>
        /// 光标和取样都用物理屏幕坐标，软件窗体内外是同一套。
        /// 返回值里的点已换成蒙层客户区的物理像素。
        /// </summary>
        private Color? ReadCursorPixel(out Win32Point client)
        {
            client = default;
            var previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
            try
            {
                if (!GetCursorPos(out var screen)) return null;
                EnsurePixelBuffer();
                if (_memoryDc == IntPtr.Zero || _dibBits == IntPtr.Zero) return null;
                if (!BitBlt(_memoryDc, 0, 0, 1, 1, _screenDc, screen.X, screen.Y, SrcCopy)) return null;
                client = screen;
                if (_hwnd != IntPtr.Zero)
                    ScreenToClient(_hwnd, ref client);
                var bgra = Marshal.ReadInt32(_dibBits);
                return Color.FromRgb((byte)((bgra >> 16) & 0xFF), (byte)((bgra >> 8) & 0xFF), (byte)(bgra & 0xFF));
            }
            finally
            {
                if (previous != IntPtr.Zero)
                    SetThreadDpiAwarenessContext(previous);
            }
        }

        private void MoveChip(Win32Point client)
        {
            var source = PresentationSource.FromVisual(this);
            var local = source?.CompositionTarget == null
                ? new Point(client.X, client.Y)
                : source.CompositionTarget.TransformFromDevice.Transform(new Point(client.X, client.Y));

            const double gap = 16;
            var chipW = _chipFrame.ActualWidth > 1 ? _chipFrame.ActualWidth : 24;
            var chipH = _chipFrame.ActualHeight > 1 ? _chipFrame.ActualHeight : 24;
            var boundsW = _canvas.ActualWidth > 1 ? _canvas.ActualWidth : ActualWidth;
            var boundsH = _canvas.ActualHeight > 1 ? _canvas.ActualHeight : ActualHeight;
            var left = local.X + gap;
            var top = local.Y + gap;
            if (left + chipW > boundsW) left = local.X - chipW - 8;
            if (top + chipH > boundsH) top = local.Y - chipH - 8;
            Canvas.SetLeft(_chipFrame, Math.Max(0, left));
            Canvas.SetTop(_chipFrame, Math.Max(0, top));
        }

        private void EnsurePixelBuffer()
        {
            if (_dibBits != IntPtr.Zero) return;
            _screenDc = GetDC(IntPtr.Zero);
            _memoryDc = CreateCompatibleDC(_screenDc);
            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = 1,
                    Height = -1,
                    Planes = 1,
                    BitCount = 32
                }
            };
            _dib = CreateDIBSection(_screenDc, ref info, 0, out _dibBits, IntPtr.Zero, 0);
            _oldObject = SelectObject(_memoryDc, _dib);
        }

        private void ReleasePixelBuffer()
        {
            if (_memoryDc != IntPtr.Zero && _oldObject != IntPtr.Zero)
                SelectObject(_memoryDc, _oldObject);
            if (_dib != IntPtr.Zero) DeleteObject(_dib);
            if (_memoryDc != IntPtr.Zero) DeleteDC(_memoryDc);
            if (_screenDc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, _screenDc);
            _dib = IntPtr.Zero;
            _dibBits = IntPtr.Zero;
            _memoryDc = IntPtr.Zero;
            _screenDc = IntPtr.Zero;
        }

        private void Finish(Color? color)
        {
            if (_finished) return;
            _finished = true;
            Picked = color;
            DialogResult = color.HasValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfoHeader
        {
            public int Size;
            public int Width;
            public int Height;
            public short Planes;
            public short BitCount;
            public int Compression;
            public int SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public int ClrUsed;
            public int ClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfo
        {
            public BitmapInfoHeader Header;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Win32Point point);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hwnd, ref Win32Point point);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr window);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr window, IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr dest, int x, int y, int width, int height, IntPtr source, int srcX, int srcY, int rop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapInfo info, uint usage, out IntPtr bits, IntPtr section, uint offset);
    }
}
