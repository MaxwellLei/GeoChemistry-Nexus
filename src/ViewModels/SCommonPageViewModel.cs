using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using HandyControl.Controls;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    partial class SCommonPageViewModel : ObservableObject
    {
        private bool changeConfig = false;  // 是否第一次进入设置页面
        private bool isInsideChange = false;    //是否是代码层面改变设置的值

        private string dbLocationPath;  //数据库路径

        //封面流
        [ObservableProperty]
        private CoverFlow coverFlowMain;

        [ObservableProperty]     
        private int dbLocation;      // 数据库位置设置(0是默认位置，即文档；1是自定义位置)

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

        public RelayCommand OpenDbFolderCommand { get; private set; }   // 打开存储文件夹命令
        public RelayCommand DbLocationChangedCommand { get; private set; }   // 修改存储文件位置命令
        public RelayCommand AutoOffTimeChangedCommand { get; private set; }   // 修改通知自动关闭时间命令
        public RelayCommand LanguageChangedCommand { get; private set; }   // 修改语言命令
        public RelayCommand CheckUpdateCommand { get; private set; }   // 检查更新命令
        public RelayCommand ExitProgrmModeCommand { get; private set; }   // 退出程序方式命令
        public RelayCommand AddStartImgCommand { get; private set; }    // 添加启动图

        // 初始化
        public SCommonPageViewModel()
        {
            OpenDbFolderCommand = new RelayCommand(ExecuteOpenDbFolderCommand);
            DbLocationChangedCommand = new RelayCommand(ExecuteDbLocationChangedCommand);
            LanguageChangedCommand = new RelayCommand(ExecuteLanguageChangedCommand);
            AutoOffTimeChangedCommand = new RelayCommand(ExecuteAutoOffTimeChangedCommand);
            CheckUpdateCommand = new RelayCommand(ExecuteCheckUpdateCommand);
            ExitProgrmModeCommand = new RelayCommand(ExecuteExitProgrmModeCommand);
            AddStartImgCommand = new RelayCommand(ExecuteAddStartImgCommand);
            ReadConfig();   // 读取配置文件
            changeConfig = true;
        }

        // 退出程序方式
        private void ExecuteExitProgrmModeCommand()
        {
            ConfigHelper.SetConfig("exit_program_mode", ExitMode.ToString());
            MessageHelper.Success(I18n.GetString("ModifedSuccess"));
        }

        // 检查更新
        private void ExecuteCheckUpdateCommand()
        {
            MessageHelper.Success(I18n.GetString("Info1"));
            //UpdateHelper.CheckForUpdatesAsync();
        }

        // 是否开机启动
        partial void OnBootChanged(bool value)
        {
            BootHelper.SetAutoRun(Boot);
            ConfigHelper.SetConfig("boot", Boot.ToString());
            MessageHelper.Success(I18n.GetString("ModifedSuccess"));
        }

        // 是否检查更新
        partial void OnAutoCheckChanged(bool value)
        {
            ConfigHelper.SetConfig("auto_check_update", AutoCheck.ToString());
            MessageHelper.Success(I18n.GetString("ModifedSuccess"));
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
            MessageHelper.Success(I18n.GetString("ModifedSuccess"));
        }

        // 修改语言
        private void ExecuteLanguageChangedCommand()
        {
            if (Language == 0)
            {
                LanguageHelper.ChangeLanguage("zh-CN");
                ConfigHelper.SetConfig("language", "0");
            }
            else
            {
                LanguageHelper.ChangeLanguage("en-US");
                ConfigHelper.SetConfig("language", "1");
            }

            MessageHelper.Success(I18n.GetString("ModifedSuccess"));
        }

        // 修改数据库位置
        private void ExecuteDbLocationChangedCommand()
        {
            //保存数据库位置设置
            if (DbLocation == 0)
            {
                if (!isInsideChange)
                {
                    Helpers.ConfigHelper.SetConfig("database_location", "0");
                    Helpers.ConfigHelper.SetConfig("database_location_path", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));  //如果是默认设置，则删除配置文件中的数据库位置
                    MessageHelper.Success(I18n.GetString("ModifedSuccess"));
                }
                isInsideChange = false;
            }
            else
            {
                dbLocationPath = FileHelper.GetFolderPath();  //获取文件夹路径
                if (dbLocationPath != null)
                {
                    Helpers.ConfigHelper.SetConfig("database_location", "1");
                    Helpers.ConfigHelper.SetConfig("database_location_path", dbLocationPath);  //如果是自定义设置，则保存配置文件中的数据库位置
                    MessageHelper.Success(I18n.GetString("ModifedSuccess"));
                }
                else
                {
                    isInsideChange = true;
                    DbLocation = 0;
                    MessageHelper.Warning(I18n.GetString("ModifedCanceled"));
                }
            }
        }

        // 打开指定数据库文件夹
        private void ExecuteOpenDbFolderCommand()
        {
            FileHelper.Openxplorer(ConfigHelper.GetConfig("database_location_path"));
            MessageHelper.Success(I18n.GetString("ModifedSuccess"));
        }

        // 读取配置文件
        void ReadConfig()
        {
            if (ConfigHelper.GetConfig("database_location_path") == "")
            {
                Helpers.ConfigHelper.SetConfig("database_location_path", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));  //如果是默认设置，则删除配置文件中的数据库位置
            }

            DbLocation = int.Parse(Helpers.ConfigHelper.GetConfig("database_location"));   //读取数据文件设置
            Language = int.Parse(Helpers.ConfigHelper.GetConfig("language"));  //读取语言
            AutoOffTime = int.Parse(Helpers.ConfigHelper.GetConfig("auto_off_time"));    //读取消息通知时间
            boot = bool.Parse(Helpers.ConfigHelper.GetConfig("boot"));     //读取是否自动开机
            autoCheck = bool.Parse(Helpers.ConfigHelper.GetConfig("auto_check_update"));   //读取是否自动检查更新
            ExitMode = int.Parse(Helpers.ConfigHelper.GetConfig("exit_program_mode"));  //读取退出方式
        }

        // 保存修改后的配置文件
        void SaveConfig()
        {
            Helpers.ConfigHelper.SetConfig("language", Language.ToString());  //保存语言设置
            Helpers.ConfigHelper.SetConfig("auto_off_time", AutoOffTime.ToString());  //保存消息通知时间设置
            Helpers.ConfigHelper.SetConfig("boot", Boot.ToString());  //保存是否自动开机设置
            Helpers.ConfigHelper.SetConfig("auto_check_update", AutoCheck.ToString());  //保存是否自动检查更新设置
            Helpers.ConfigHelper.SetConfig("exit_program_mode", ExitMode.ToString());  //保存退出方式设置
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
            MessageHelper.Success(I18n.GetString("ModifedSuccess"));
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
                CoverFlowMain.Add(file);
            }
        }
    }
}
