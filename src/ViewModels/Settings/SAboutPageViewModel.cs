using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        public SAboutPageViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? string.Empty;
            OpenUrlCommand = new RelayCommand<string?>(ExecuteOpenUrlCommand);
            CopyTextCommand = new RelayCommand<string?>(ExecuteCopyTextCommand);
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
