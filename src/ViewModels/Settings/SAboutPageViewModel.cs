using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        public IReadOnlyList<string> SpecialThanksNames { get; } = new[]
        {
            "张天阳",
            "邢琬若",
            "张正阳",
            "崔庆意",
            "刘悦"
        };

        public RelayCommand<string?> OpenUrlCommand { get; private set; } = null!;

        public SAboutPageViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? string.Empty;
            OpenUrlCommand = new RelayCommand<string?>(ExecuteOpenUrlCommand);
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
    }
}
