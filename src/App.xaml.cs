using GeoChemistryNexus.Controls.Startup;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GeoChemistryNexus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static bool _uiThemesLoaded;

        // 未处理 UI 异常的熔断：窗口时间内超过上限视为错误循环，退出程序。
        private const int UnhandledExceptionLimit = 5;
        private static readonly TimeSpan UnhandledExceptionWindow = TimeSpan.FromSeconds(10);
        private readonly Queue<DateTime> _recentUnhandledExceptionTimes = new();
        private bool _isShowingUnhandledDialog;

        public App()
        {
            // 订阅全局异常事件
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            this.Exit += App_Exit;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0 && e.Args[0] == "--validate-epma")
            {
                await ValidateEpmaPackagesAsync(e.Args.Skip(1).ToArray());
                return;
            }

            if (e.Args.Any(a => string.Equals(a, "--publish-official-templates", StringComparison.OrdinalIgnoreCase))
                && TryHandleHeadlessPublish(e.Args))
                return;

            // GUI 单实例：已有进程时转发关联文件路径并退出
            if (!SingleInstanceHelper.TryAcquirePrimaryInstance())
            {
                await SingleInstanceHelper.TryNotifyRunningInstanceAsync(TryFindAssociatedPackageArg(e.Args));
                Shutdown();
                return;
            }

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            StartupSplashHost? splash = null;
            try
            {
                // 启动动画文案与主界面共用 LanguageService，需先初始化语言（仅读配置，开销很小）。
                LanguageService.InitializeLanguage();

                // The sample animation has its own STA dispatcher. Render it before merging themes,
                // loading fonts, or constructing business views on this thread.
                splash = await StartupSplashHost.ShowAsync(() =>
                    Dispatcher.BeginInvoke(new Action(() => Shutdown())));

                await InitializeGuiAsync(splash, e.Args);
            }
            catch (Exception ex)
            {
                LogException(ex, "OnStartup");
                if (!Dispatcher.HasShutdownStarted)
                {
                    MessageBox.Show(LanguageService.Instance["error_occurred_during_startup"] + ex.Message,
                        LanguageService.Instance["error"], MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                }
            }
            finally
            {
                if (splash != null) await splash.CloseAsync();
            }
        }

        // Invoked only after the splash has rendered; JIT and construction of business views
        // cannot delay its first frame.
        private async Task InitializeGuiAsync(StartupSplashHost splash, string[] args)
        {
            SingleInstanceHelper.StartIpcServer();

            string? associatedPath = TryFindAssociatedPackageArg(args);
            if (!string.IsNullOrEmpty(associatedPath))
                SingleInstanceHelper.EnqueuePackagePath(associatedPath);

            splash.ReportProgress(2, StartupStatus.InitializingConfiguration);
            splash.ReportProgress(5, StartupStatus.LoadingInterface);
            EnsureUiThemesLoaded();

            splash.ReportProgress(10, StartupStatus.InitializingData);
            await Task.Run(AppDataPathHelper.Initialize);

            splash.ReportProgress(30, StartupStatus.LoadingFonts);
            await FontService.GetFontNamesAsync();

            splash.ReportProgress(50, StartupStatus.ReadingConfiguration);
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            splash.ReportProgress(70, StartupStatus.LoadingDiagrams);
            MainPlotPage.GetPage();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            splash.ReportProgress(75, StartupStatus.LoadingGeothermobarometers);
            GeothermometerPageView.GetPage();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            splash.ReportProgress(90, StartupStatus.LoadingInterface);
            LanguageService.RefreshCurrentCulture();
            var mainWindow = new MainWindow();
            splash.ReportProgress(100, StartupStatus.Ready);
            await splash.FinishAsync();
            if (Dispatcher.HasShutdownStarted) return;

            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
            mainWindow.Activate();
        }

        /// <summary>
        /// 在启动窗已显示后合并本地 Styles，避免拖慢首帧。
        /// </summary>
        private static void EnsureUiThemesLoaded()
        {
            if (_uiThemesLoaded)
                return;

            // 先写入主题色资源（Application.Resources 顶层，优先级最高），再合并各样式字典
            AppThemeHelper.Initialize();

            var merged = Current.Resources.MergedDictionaries;
            merged.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/GeoChemistryNexus;component/Themes/Styles.xaml")
            });

            _uiThemesLoaded = true;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            SingleInstanceHelper.Release();
        }

        /// <summary>
        /// 从启动参数中提取模板包路径（.diag.zip / .gtm.zip）。
        /// </summary>
        private static string? TryFindAssociatedPackageArg(string[]? args)
        {
            if (args == null || args.Length == 0)
                return null;

            foreach (string arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg) || arg.StartsWith("--", StringComparison.Ordinal))
                    continue;

                string path = arg.Trim().Trim('"');
                if (!File.Exists(path))
                    continue;

                if (TemplatePackageFileExtensions.IsAssociatedPackagePath(path))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// 无头模式：命令行发布官方图解模板
        /// GeoChemistryNexus.exe --publish-official-templates [--staging-dir=路径] [--dry-run]
        /// </summary>
        private bool TryHandleHeadlessPublish(string[] args)
        {
            if (args == null || !args.Any(a => string.Equals(a, "--publish-official-templates", StringComparison.OrdinalIgnoreCase)))
                return false;

            bool dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase));
            string? stagingArg = args.FirstOrDefault(a => a.StartsWith("--staging-dir=", StringComparison.OrdinalIgnoreCase));
            string? stagingDir = null;
            if (!string.IsNullOrEmpty(stagingArg))
                stagingDir = stagingArg["--staging-dir=".Length..].Trim('"');

            AppDataPathHelper.Initialize();
            LanguageService.InitializeLanguage();

            string logDir = AppDataPathHelper.GetLogsPath();
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string logFile = Path.Combine(logDir, $"publish_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            var logLines = new List<string>();

            void Log(string message)
            {
                string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
                logLines.Add(line);
                Console.WriteLine(line);
            }

            try
            {
                var settings = CosPublishSettingsService.Load();
                if (string.IsNullOrEmpty(stagingDir))
                    stagingDir = settings.StagingDirectory;

                if (string.IsNullOrWhiteSpace(stagingDir))
                    throw new InvalidOperationException("Staging directory is required. Use --staging-dir= or configure cos_publish.json.");

                Log($"Staging directory: {stagingDir}");

                string announcement = string.Empty;
                string minimumSupportedVersion = string.Empty;
                try
                {
                    string json = OfficialContentMirrorClient.GetStringAsync(OfficialContentEndpoints.ServerInfoFileName).GetAwaiter().GetResult();
                    var info = JsonHelper.Deserialize<ServerInfo>(json);
                    announcement = info?.Announcement ?? string.Empty;
                    minimumSupportedVersion = info?.MinimumSupportedVersion ?? string.Empty;
                }
                catch
                {
                    // keep empty server config fields
                }

                var publishResult = GraphMapTemplatePublishService.ExportToDirectory(stagingDir, new PublishOptions
                {
                    PreserveAnnouncement = announcement,
                    PreserveMinimumSupportedVersion = minimumSupportedVersion
                });
                Log(publishResult.Summary);

                var geoResult = GeothermometerPublishService.ExportToDirectory(stagingDir);
                Log(geoResult.Summary);

                // EPMA calculation templates are published with the rest unless explicitly skipped.
                bool publishEpma = !args.Any(a => string.Equals(a, "--skip-epma", StringComparison.OrdinalIgnoreCase));
                EpmaPublishResult? epmaResult = null;
                if (publishEpma)
                {
                    epmaResult = EpmaPublishService.ExportToDirectory(stagingDir);
                    Log(epmaResult.Summary);
                }

                if (dryRun)
                {
                    Log("Dry run: skipping COS upload.");
                }
                else
                {
                    var targets = settings.EnabledTargets.ToList();
                    if (targets.Count == 0)
                        throw new InvalidOperationException("COS settings are not configured.");

                    OfficialContentMirrorClient.StampServerInfoFile(
                        publishResult.ServerInfoPath,
                        targets.Select(target => target.PublicBaseUrl));

                    var progress = new Progress<string>(Log);
                    var failures = new List<string>();
                    foreach (var target in targets)
                    {
                        try
                        {
                            Log($"Uploading to {target.DisplayName}...");
                            var uploadResult = TencentCosPublishService.UploadCombinedPublishAsync(
                                stagingDir, publishResult, geoResult, null, null, target, true, true, false, false, progress, null,
                                epmaResult, publishEpma).GetAwaiter().GetResult();
                            Log($"{target.DisplayName}: {uploadResult.Message}");
                            if (!uploadResult.Success)
                                failures.Add(target.DisplayName);
                        }
                        catch (Exception uploadEx)
                        {
                            failures.Add(target.DisplayName);
                            Log($"ERROR {target.DisplayName}: {uploadEx.Message}");
                        }
                    }

                    if (failures.Count < targets.Count)
                    {
                        GraphMapTemplatePublishService.ClearPendingPublishFlags();
                        GeothermometerPublishService.ApplyPublishedResult(geoResult);
                        if (epmaResult != null)
                            EpmaPublishService.ApplyPublishedResultAsync(epmaResult).GetAwaiter().GetResult();
                    }
                    if (failures.Count > 0)
                        Environment.ExitCode = 1;
                }

                File.WriteAllLines(logFile, logLines);
                Shutdown();
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                File.WriteAllLines(logFile, logLines);
                Environment.ExitCode = 1;
                Shutdown();
                return true;
            }
        }

        /// <summary>
        /// UI线程未处理异常
        /// </summary>
        private void App_DispatcherUnhandledException(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "DispatcherUnhandledException");
            e.Handled = true;

            // 无法安全继续的情况才退出：致命异常、主窗口尚未就绪（启动阶段）、
            // 或短时间内反复报错（说明陷入了错误循环）。其余异常只提示并保留当前工作，避免丢失未保存的编辑。
            if (!CanContinueAfterUnhandledException(e.Exception))
            {
                ShowUnhandledExceptionDialog(e.Exception, willExit: true);
                Current.Shutdown();
                return;
            }

            // 模态对话框的消息循环可能再次触发异常，此时只记录日志，避免对话框叠加。
            if (_isShowingUnhandledDialog)
                return;

            _isShowingUnhandledDialog = true;
            try
            {
                ShowUnhandledExceptionDialog(e.Exception, willExit: false);
            }
            finally
            {
                _isShowingUnhandledDialog = false;
            }
        }

        private bool CanContinueAfterUnhandledException(Exception ex)
        {
            if (ex is OutOfMemoryException or AccessViolationException or InsufficientExecutionStackException)
                return false;

            if (MainWindow is not { IsLoaded: true })
                return false;

            DateTime now = DateTime.UtcNow;
            _recentUnhandledExceptionTimes.Enqueue(now);
            while (_recentUnhandledExceptionTimes.Count > 0
                   && now - _recentUnhandledExceptionTimes.Peek() > UnhandledExceptionWindow)
            {
                _recentUnhandledExceptionTimes.Dequeue();
            }

            return _recentUnhandledExceptionTimes.Count < UnhandledExceptionLimit;
        }

        private static void ShowUnhandledExceptionDialog(Exception ex, bool willExit)
        {
            try
            {
                string errorPrefix = LanguageService.Instance["unhandled_exception_occurred"] ?? "应用程序发生未处理错误：";
                MessageBox.Show(
                    $"{errorPrefix}{ex.Message}\r\n\r\n{LanguageService.Instance["see_error_log_for_details"]}",
                    LanguageService.Instance["error"],
                    MessageBoxButton.OK,
                    willExit ? MessageBoxImage.Stop : MessageBoxImage.Error);
            }
            catch
            {
                // 对话框本身失败不应影响后续处理
            }
        }

        /// <summary>
        /// 非UI线程未处理异常
        /// </summary>
        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex, "UnhandledException");
            }
        }

        /// <summary>
        /// Task未观察异常
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        }

        /// <summary>
        /// 记录异常到日志文件
        /// </summary>
        private static void LogException(Exception ex, string source)
        {
            try
            {
                string logDir = AppDataPathHelper.GetLogsPath();
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log");
                
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== Error Log [{source}] ===");
                sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Source: {source}");
                sb.AppendLine();
                sb.AppendLine("--- Exception Details ---");
                sb.AppendLine(ex.ToString());
                sb.AppendLine();
                
                // 记录 InnerException
                if (ex.InnerException != null)
                {
                    sb.AppendLine("--- Inner Exception ---");
                    sb.AppendLine(ex.InnerException.ToString());
                    sb.AppendLine();
                    
                    // 如果还有更深层的 InnerException
                    if (ex.InnerException.InnerException != null)
                    {
                        sb.AppendLine("--- Inner Inner Exception ---");
                        sb.AppendLine(ex.InnerException.InnerException.ToString());
                        sb.AppendLine();
                    }
                }
                
                sb.AppendLine("--- Environment Info ---");
                sb.AppendLine($"BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                sb.AppendLine($"CurrentDirectory: {Environment.CurrentDirectory}");
                sb.AppendLine($"ExecutablePath: {System.Windows.Forms.Application.ExecutablePath}");
                sb.AppendLine($"ProcessPath: {Environment.ProcessPath}");
                
                File.WriteAllText(logFile, sb.ToString());
            }
            catch
            {
                // 日志写入失败也不要影响程序
            }
        }
    }
}
