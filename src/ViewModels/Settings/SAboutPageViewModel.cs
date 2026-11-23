using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SAboutPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string version = string.Empty;

        /// <summary>
        /// 技术栈标签（.NET 主版本号取自当前运行时）。
        /// </summary>
        public IReadOnlyList<string> TechStackTags { get; } = new[]
        {
            $".NET {Environment.Version.Major}",
            "WPF",
            "MVVM"
        };

        /// <summary>
        /// 使用的开源组件。
        /// </summary>
        public IReadOnlyList<string> OpenSourceLibraries { get; } = new[]
        {
            "VirtualizingWrapPanel",
            "ScottPlot",
            "ReoGrid",
            "Jint",
            "LiteDB",
            "CommunityToolkit.Mvvm",
            "Microsoft.Xaml.Behaviors.Wpf",
            "System.Text.Json"
        };

        public IReadOnlyList<string> SpecialThanksNames { get; } = new[]
        {
            "张天阳",
            "邢琬若",
            "张正阳",
            "崔庆意",
            "刘悦"
        };

        public RelayCommand<string?> OpenUrlCommand { get; private set; } = null!;

        public RelayCommand<string?> CopyTextCommand { get; private set; } = null!;

        /// <summary>
        /// 点击 Gn 块（用于解锁开发者模式入口）。
        /// </summary>
        public RelayCommand UnlockDeveloperModeCommand { get; private set; } = null!;

        private const int UnlockClickCount = 5;
        private static readonly TimeSpan UnlockClickInterval = TimeSpan.FromSeconds(2);
        private int _unlockClicks;
        private DateTime _lastUnlockClickTime = DateTime.MinValue;

        public SAboutPageViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? string.Empty;
            OpenUrlCommand = new RelayCommand<string?>(ExecuteOpenUrlCommand);
            CopyTextCommand = new RelayCommand<string?>(ExecuteCopyTextCommand);
            UnlockDeveloperModeCommand = new RelayCommand(ExecuteUnlockDeveloperModeCommand);
        }

        private void ExecuteUnlockDeveloperModeCommand()
        {
            if (DeveloperModeEntryUnlockedMessage.IsUnlocked) return;

            var now = DateTime.Now;
            _unlockClicks = now - _lastUnlockClickTime <= UnlockClickInterval ? _unlockClicks + 1 : 1;
            _lastUnlockClickTime = now;

            if (_unlockClicks < UnlockClickCount) return;

            _unlockClicks = 0;
            DeveloperModeEntryUnlockedMessage.IsUnlocked = true;
            WeakReferenceMessenger.Default.Send(new DeveloperModeEntryUnlockedMessage());
            MessageHelper.Info(LanguageService.GetString("developer_mode_entry_unlocked",
                "Developer mode option is now available in General Settings."));
        }

        private void ExecuteOpenUrlCommand(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // 无法打开链接
                MessageBox.Show(LanguageService.Instance["unable_to_open_link"] + ex.Message);
            }
        }

        private void ExecuteCopyTextCommand(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                Clipboard.SetText(text);
                MessageHelper.Success(LanguageService.GetString("periodic_table_copied", "Copied to clipboard."));
            }
            catch (Exception)
            {
                MessageHelper.Info(LanguageService.GetString("periodic_table_copy_failed", "Copy failed."));
            }
        }
    }
}
