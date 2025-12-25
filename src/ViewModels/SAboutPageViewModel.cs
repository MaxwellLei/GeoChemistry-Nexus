using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SAboutPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string version;

        public RelayCommand<string> OpenUrlCommand { get; private set; }

        public SAboutPageViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            OpenUrlCommand = new RelayCommand<string>(ExecuteOpenUrlCommand);
        }

        private void ExecuteOpenUrlCommand(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            catch (Exception ex)
            {
                // 无法打开链接
                MessageBox.Show(LanguageService.Instance["unable_to_open_link"] + ex.Message);
            }
        }
    }
}
