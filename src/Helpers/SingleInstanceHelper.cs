using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GeoChemistryNexus.Views;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 保证 GUI 单实例：第二进程通过命名管道把关联文件路径交给首个进程后退出。
    /// 无头发布（--publish-official-templates）不走此逻辑。
    /// </summary>
    public static class SingleInstanceHelper
    {
        // 与 installer AppId 对齐，避免与其他程序冲突
        private const string MutexName = @"Local\GeoChemistryNexus-{A3F8C2E1-9B4D-4F6A-8C1E-2D5E7F9A0B3C}";
        private const string PipeName = "GeoChemistryNexus-{A3F8C2E1-9B4D-4F6A-8C1E-2D5E7F9A0B3C}";
        private const string ActivateToken = "__ACTIVATE__";

        private static Mutex? _mutex;
        private static CancellationTokenSource? _serverCts;
        private static readonly ConcurrentQueue<string> PendingPackagePaths = new();

        /// <summary>
        /// 尝试成为主实例。失败表示已有实例在运行。
        /// </summary>
        public static bool TryAcquirePrimaryInstance()
        {
            _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool createdNew);
            if (createdNew)
                return true;

            try
            {
                _mutex.Dispose();
            }
            catch
            {
                // ignore
            }

            _mutex = null;
            return false;
        }

        /// <summary>
        /// 启动命名管道服务，接收第二实例转发的路径 / 激活请求。
        /// </summary>
        public static void StartIpcServer()
        {
            if (_serverCts != null)
                return;

            _serverCts = new CancellationTokenSource();
            CancellationToken token = _serverCts.Token;
            _ = Task.Run(() => RunPipeServerLoopAsync(token), token);
        }

        /// <summary>
        /// 通知已运行的主实例：激活窗口，并可选导入关联包。
        /// </summary>
        public static bool TryNotifyRunningInstance(string? packagePath)
        {
            string payload = string.IsNullOrWhiteSpace(packagePath)
                ? ActivateToken
                : packagePath.Trim().Trim('"');

            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    using var client = new NamedPipeClientStream(
                        ".",
                        PipeName,
                        PipeDirection.Out,
                        PipeOptions.None);

                    client.Connect(500);
                    using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
                    writer.WriteLine(payload);
                    return true;
                }
                catch (TimeoutException)
                {
                    Thread.Sleep(150);
                }
                catch (IOException)
                {
                    Thread.Sleep(150);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(150);
                }
            }

            return false;
        }

        /// <summary>
        /// 将待导入路径入队（首启参数或 IPC 收到）。
        /// </summary>
        public static void EnqueuePackagePath(string? packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
                return;

            string path = packagePath.Trim().Trim('"');
            if (!File.Exists(path) || !TemplatePackageFileExtensions.IsAssociatedPackagePath(path))
                return;

            PendingPackagePaths.Enqueue(path);
        }

        /// <summary>
        /// 在主窗口上激活并消费待导入队列（须在 UI 线程调用）。
        /// </summary>
        public static async Task DrainPendingPackagesAsync(MainWindow mainWindow)
        {
            ActivateMainWindow(mainWindow);

            while (PendingPackagePaths.TryDequeue(out string? path))
            {
                await mainWindow.TryOpenAssociatedPackageAsync(path);
            }
        }

        public static void Release()
        {
            try
            {
                _serverCts?.Cancel();
                _serverCts?.Dispose();
                _serverCts = null;
            }
            catch
            {
                // ignore
            }

            try
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                    _mutex = null;
                }
            }
            catch
            {
                // ignore
            }
        }

        private static async Task RunPipeServerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                    using var reader = new StreamReader(server, Encoding.UTF8);
                    string? line = await reader.ReadLineAsync().ConfigureAwait(false);
                    HandleIpcPayload(line);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    // 客户端断开或管道重建，继续监听
                }
                catch (Exception)
                {
                    try
                    {
                        await Task.Delay(200, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                finally
                {
                    try
                    {
                        server?.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        private static void HandleIpcPayload(string? payload)
        {
            Application? app = Application.Current;
            if (app?.Dispatcher == null)
                return;

            string? text = payload?.Trim();
            bool activateOnly = string.IsNullOrEmpty(text) ||
                                string.Equals(text, ActivateToken, StringComparison.Ordinal);

            if (!activateOnly && !string.IsNullOrEmpty(text))
                EnqueuePackagePath(text);

            app.Dispatcher.BeginInvoke(new Action(async () =>
            {
                if (app.MainWindow is not MainWindow mainWindow)
                    return;

                if (activateOnly)
                {
                    ActivateMainWindow(mainWindow);
                    return;
                }

                await DrainPendingPackagesAsync(mainWindow);
            }));
        }

        private static void ActivateMainWindow(Window window)
        {
            if (!window.IsVisible)
                window.Show();

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;

            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();
        }
    }
}
