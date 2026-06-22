using GeoChemistryNexus.Models;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 订阅全局异常事件
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (TryHandleHeadlessPublish(e.Args))
                return;

            // 0. 初始化语言
            LanguageService.InitializeLanguage();

            // 1. 显示启动窗体
            var startViewModel = new StartViewModel();
            var startWindow = new StartWindow
            {
                DataContext = startViewModel
            };
            startWindow.Show();

            // 2. 后台执行初始化任务
            await Task.Run(async () =>
            {
                try
                {
                    // 阶段 1: 语言服务
                    startViewModel.UpdateProgress(10, LanguageService.Instance["initializing_language_service_ellipsis"]);
                    // 语言初始化已提前完成
                    await Task.Delay(200); // 稍微停顿以便用户看清提示

                    // 阶段 2: 字体资源
                    startViewModel.UpdateProgress(30, LanguageService.Instance["loading_font_resources_ellipsis"]);
                    await FontService.GetFontNamesAsync();
                    await Task.Delay(200);

                    // 阶段 3: 配置检查
                    startViewModel.UpdateProgress(50, LanguageService.Instance["reading_user_configuration_ellipsis"]);
                    await Task.Delay(200);

                    // 阶段 4: 预加载绘图模块（提前实例化 MainPlotPage 单例，避免首次进入卡顿）
                    startViewModel.UpdateProgress(70, LanguageService.Instance["loading_drawing_module_ellipsis"]);
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        MainPlotPage.GetPage();
                    });

                    // 阶段 4.1: 预加载温压计模块（提前实例化 GeothermometerPageView 单例，避免首次进入卡顿）
                    startViewModel.UpdateProgress(75, LanguageService.Instance["loading_drawing_module_ellipsis"]);
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        GeothermometerPageView.GetPage();
                    });

                    startViewModel.UpdateProgress(80, LanguageService.Instance["loading_main_interface_components_ellipsis"]);
                    await Task.Delay(300);

                    // 切换回 UI 线程创建主窗口
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        startViewModel.UpdateProgress(90, LanguageService.Instance["entering_soon_ellipsis"]);

                        // 确保当前 UI 线程使用正确的语言配置
                        LanguageService.RefreshCurrentCulture();

                        var mainWindow = new MainWindow();
                        
                        // 设置为主窗体
                        this.MainWindow = mainWindow;

                        startViewModel.UpdateProgress(100, LanguageService.Instance["completed"]);
                        
                        mainWindow.Show();
                        startWindow.Close();
                    });
                }
                catch (Exception ex)
                {
                    // 记录启动错误到日志
                    LogException(ex, "OnStartup");
                    
                    // 处理启动错误
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(LanguageService.Instance["error_occurred_during_startup"] + ex.Message, LanguageService.Instance["error"], MessageBoxButton.OK, MessageBoxImage.Error);
                        Current.Shutdown();
                    });
                }
            });
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
            string stagingArg = args.FirstOrDefault(a => a.StartsWith("--staging-dir=", StringComparison.OrdinalIgnoreCase));
            string stagingDir = null;
            if (!string.IsNullOrEmpty(stagingArg))
                stagingDir = stagingArg.Substring("--staging-dir=".Length).Trim('"');

            LanguageService.InitializeLanguage();

            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
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
                try
                {
                    string json = UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl).GetAwaiter().GetResult();
                    var info = System.Text.Json.JsonSerializer.Deserialize<ServerInfo>(json);
                    announcement = info?.Announcement ?? string.Empty;
                }
                catch
                {
                    // keep empty announcement
                }

                var publishResult = GraphMapTemplatePublishService.ExportToDirectory(stagingDir, new PublishOptions
                {
                    PreserveAnnouncement = announcement
                });
                Log(publishResult.Summary);

                var geoResult = GeothermometerPublishService.ExportToDirectory(stagingDir);
                Log(geoResult.Summary);

                if (dryRun)
                {
                    Log("Dry run: skipping COS upload.");
                }
                else
                {
                    if (!settings.IsConfigured)
                        throw new InvalidOperationException("COS settings are not configured.");

                    var progress = new Progress<string>(Log);
                    var uploadResult = TencentCosPublishService.UploadCombinedPublishAsync(
                        stagingDir, publishResult, geoResult, null, null, settings, true, true, false, false, progress).GetAwaiter().GetResult();

                    Log(uploadResult.Message);
                    if (!uploadResult.Success)
                        Environment.ExitCode = 1;

                    GraphMapTemplatePublishService.ClearPendingPublishFlags();
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
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "DispatcherUnhandledException");
            
            MessageBox.Show(
                $"{LanguageService.Instance["error_occurred_during_startup"]}{e.Exception.Message}\r\n\r\n{LanguageService.Instance["see_error_log_for_details"]}",
                LanguageService.Instance["error"],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true;
            Current.Shutdown();
        }

        /// <summary>
        /// 非UI线程未处理异常
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex, "UnhandledException");
            }
        }

        /// <summary>
        /// Task未观察异常
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        }

        /// <summary>
        /// 记录异常到日志文件
        /// </summary>
        private void LogException(Exception ex, string source)
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                
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
