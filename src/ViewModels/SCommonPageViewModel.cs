using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using HandyControl.Controls;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using unvell.ReoGrid;

namespace GeoChemistryNexus.ViewModels
{
    partial class SCommonPageViewModel : ObservableObject
    {
        // 定义语言代码映射数组(索引对应 ComboBox)
        // 0 -> zh-CN; 1 -> en-US; 2 -> de-DE; 3 -> es-ES; 4 -> ja-JP; 5 -> ko-KR; 6 -> ru-RU
        private readonly string[] _languageCodes = { "zh-CN", "en-US", "de-DE", "es-ES", "ja-JP", "ko-KR", "ru-RU" };



        //封面流
        [ObservableProperty]
        private CoverFlow? coverFlowMain;

        [ObservableProperty]
        private int language;     // 语言设置(0是中文;1是英文)

        [ObservableProperty]
        private int font;     // 字体设置(0是微软雅黑;1是添加字体)

        [ObservableProperty]
        private int autoOffTime;    // 通知自动关闭时间(0是5秒;1是4秒;2是3秒;3是2秒)

        [ObservableProperty]
        private bool boot;  // 开机自动启动

        [ObservableProperty]
        private bool autoCheck;  // 自动检查更新

        [ObservableProperty]
        private int exitMode;    // 退出方式(0是最小化到托盘;1是退出程序)

        [ObservableProperty]
        private string version;     // 软件版本

        [ObservableProperty]
        private bool _developerMode; // 开发者模式

        public RelayCommand AutoOffTimeChangedCommand { get; private set; }   // 修改通知自动关闭时间命令
        public RelayCommand LanguageChangedCommand { get; private set; }   // 修改语言命令
        public RelayCommand CheckUpdateCommand { get; private set; }   // 检查更新命令
        public RelayCommand ExitProgrmModeCommand { get; private set; }   // 退出程序方式命令
        public RelayCommand AddStartImgCommand { get; private set; }    // 添加启动图

        // 初始化
        public SCommonPageViewModel()
        {
            LanguageChangedCommand = new RelayCommand(ExecuteLanguageChangedCommand);
            AutoOffTimeChangedCommand = new RelayCommand(ExecuteAutoOffTimeChangedCommand);
            CheckUpdateCommand = new RelayCommand(ExecuteCheckUpdateCommandAsync);
            ExitProgrmModeCommand = new RelayCommand(ExecuteExitProgrmModeCommand);
            AddStartImgCommand = new RelayCommand(ExecuteAddStartImgCommand);
            ReadConfig();   // 读取配置文件

            // 获取版本信息
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                
        }

        // 退出程序方式
        private void ExecuteExitProgrmModeCommand()
        {
            ConfigHelper.SetConfig("exit_program_mode", ExitMode.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 检查更新
        private async void ExecuteCheckUpdateCommandAsync()
        {
            //MessageHelper.Info("正在检查更新，请稍候...");
            try
            {
                // 异步等待结果
                bool hasUpdate = await UpdateHelper.CheckForUpdateAsync(Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

                if (hasUpdate)
                {
                    bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                                LanguageService.Instance["new_version_available_github"],
                                LanguageService.Instance["Cancel"],
                                LanguageService.Instance["go_to_download"]);
                    if (isConfirmed)
                    {
                        try
                        {
                            await UpdateHelper.CheckAndUpdatePlotCategoriesAsync();
                        }
                        catch
                        {
                            // 忽略更新 PlotTemplateCategories.json 的错误
                        }

                        string url = "https://github.com/MaxwellLei/GeoChemistry-Nexus/releases/latest";
                        //拉起浏览器
                        try
                        {
                            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                        }
                        catch (Exception ex)
                        {
                            MessageHelper.Warning((string)System.Windows.Application.Current.Resources["OpenBrowserError"] + ex.Message);
                        }
                    }
                }
                else
                {
                    try
                    {
                        await UpdateHelper.CheckAndUpdatePlotCategoriesAsync();
                    }
                    catch
                    {
                        // 忽略更新 PlotTemplateCategories.json 的错误
                    }
                    MessageHelper.Success(LanguageService.Instance["already_latest_version"]);
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["unknown_error_checking_for_updates"] + $": {ex.Message}");
            }
        }

        // 开发者模式
        partial void OnDeveloperModeChanged(bool value)
        {
            ConfigHelper.SetConfig("developer_mode", DeveloperMode.ToString());
            WeakReferenceMessenger.Default.Send(new DeveloperModeChangedMessage(value));

            if (value)
            {
                string warning = LanguageService.Instance["developer_mode_warning"];
                if (string.IsNullOrEmpty(warning))
                {
                    warning = LanguageService.Instance["dev_mode_warning_non_developers"];
                }
                MessageHelper.Warning(warning);
            }
            else
            {
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        // 是否开机启动
        partial void OnBootChanged(bool value)
        {
            BootHelper.SetAutoRun(Boot);
            ConfigHelper.SetConfig("boot", Boot.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 是否检查更新
        partial void OnAutoCheckChanged(bool value)
        {
            ConfigHelper.SetConfig("auto_check_update", AutoCheck.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 修改消息通知时间
        private void ExecuteAutoOffTimeChangedCommand()
        {
            switch (AutoOffTime)
            {
                case 0:
                    MessageHelper.waitTime = 5;
                    break;
                case 1:
                    MessageHelper.waitTime = 4;
                    break;
                case 2:
                    MessageHelper.waitTime = 3;
                    break;
                case 3:
                    MessageHelper.waitTime = 2;
                    break;
            }
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 修改语言
        private void ExecuteLanguageChangedCommand()
        {
            // 确保索引在有效范围内
            if (Language >= 0 && Language < _languageCodes.Length)
            {
                // 获取当前选中的索引对应的语言代码
                string selectedCode = _languageCodes[Language];

                // 将字符串代码保存到配置文件
                ConfigHelper.SetConfig("language", selectedCode);

                // 语言切换
                LanguageService.Instance.ChangeLanguage(new System.Globalization.CultureInfo(selectedCode));
            }
            else
            {
                // 异常处理：索引越界则默认回滚到中文
                ConfigHelper.SetConfig("language", "en-US");
                LanguageService.Instance.ChangeLanguage(new System.Globalization.CultureInfo("en-US"));
            }

            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }



        // 读取配置文件
        void ReadConfig()
        {
            // 读取语言设置
            string langConfig = Helpers.ConfigHelper.GetConfig("language");
            // 在数组中查找该字符串对应的索引
            int langIndex = Array.IndexOf(_languageCodes, langConfig);
            Language = (langIndex >= 0) ? langIndex : 1;

            AutoOffTime = int.Parse(Helpers.ConfigHelper.GetConfig("auto_off_time"));    //读取消息通知时间
            
            if (bool.TryParse(Helpers.ConfigHelper.GetConfig("developer_mode"), out bool devMode))
            {
                DeveloperMode = devMode;
            }

            Boot = bool.Parse(Helpers.ConfigHelper.GetConfig("boot"));     //读取是否自动开机
            AutoCheck = bool.Parse(Helpers.ConfigHelper.GetConfig("auto_check_update"));   //读取是否自动检查更新
            ExitMode = int.Parse(Helpers.ConfigHelper.GetConfig("exit_program_mode"));  //读取退出方式
        }

        // 添加启动图
        private void ExecuteAddStartImgCommand()
        {
            // 源文件路径
            string sourceFilePath = FileHelper.GetFilePath("ImageFile(*.jpg,*.png)|*.jpg;*.png");
            if (sourceFilePath != null)
            {
                string sourceFileName = System.IO.Path.GetFileName(sourceFilePath);
                // 目标文件路径
                string destinationFolderPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Resources", "Image", "StartPic"); ;
                // 创建目标文件路径
                string destinationFilePath = System.IO.Path.Combine(destinationFolderPath, sourceFileName);
                // 尝试复制文件
                try
                {
                    // 如果目标文件已存在，将overwrite参数设置为true以覆盖
                    File.Copy(sourceFilePath, destinationFilePath, true);
                    // 刷新封面流
                    GetFlowPic();
                    MessageHelper.Success((string)Application.Current.Resources["StartImageCopySuccessfully"]);
                }
                catch (Exception ex)
                {
                    MessageHelper.Warning((string)Application.Current.Resources["StartImageCopyError"] + ex.Message);
                }
            }
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 获取启动封面图
        public void GetFlowPic()
        {
            //获取启动封面
            string folderPath = System.IO.Path.Combine(Environment.CurrentDirectory,
                "Data", "Image", "StartPic");     // 指定文件夹路径
            string[] files = Directory.GetFiles(folderPath); // 获取文件夹中的所有文件
            foreach (var file in files)
            {
                CoverFlowMain?.Add(file);
            }
        }
    }
}
