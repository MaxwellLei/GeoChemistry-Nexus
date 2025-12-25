using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Messages;
using System.Collections.ObjectModel;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SPlotPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string corelDRAWPath;

        [ObservableProperty]
        private string inkscapePath;

        [ObservableProperty]
        private string adobeIllustratorPath;

        [ObservableProperty]
        private string customThirdPartyAppPath;

        // 默认图解列表展开层级 (1-4)
        [ObservableProperty]
        private int defaultTreeExpandLevel;

        // 第三方应用列表
        public ObservableCollection<string> ThirdPartyApps { get; } = new() { "CorelDRAW", "Inkscape", "Adobe Illustrator", "Custom" };

        [ObservableProperty]
        private string selectedThirdPartyApp;

        [ObservableProperty]
        private bool autoCheckTemplateUpdate;

        public RelayCommand SelectCorelDRAWPathCommand { get; }
        public RelayCommand SelectInkscapePathCommand { get; }
        public RelayCommand SelectAdobeIllustratorPathCommand { get; }
        public RelayCommand SelectCustomThirdPartyAppPathCommand { get; }

        private bool isLoading = true;

        public SPlotPageViewModel()
        {
            SelectCorelDRAWPathCommand = new RelayCommand(ExecuteSelectCorelDRAWPath);
            SelectInkscapePathCommand = new RelayCommand(ExecuteSelectInkscapePath);
            SelectAdobeIllustratorPathCommand = new RelayCommand(ExecuteSelectAdobeIllustratorPath);
            SelectCustomThirdPartyAppPathCommand = new RelayCommand(ExecuteSelectCustomThirdPartyAppPath);

            LoadConfig();
        }

        private void LoadConfig()
        {
            CorelDRAWPath = ConfigHelper.GetConfig("coreldraw_path");
            InkscapePath = ConfigHelper.GetConfig("inkscape_path");
            AdobeIllustratorPath = ConfigHelper.GetConfig("adobe_illustrator_path");
            CustomThirdPartyAppPath = ConfigHelper.GetConfig("custom_third_party_app_path");
            
            if (int.TryParse(ConfigHelper.GetConfig("default_tree_expand_level"), out int expandLevel))
            {
                DefaultTreeExpandLevel = expandLevel;
            }
            else
            {
                DefaultTreeExpandLevel = 2; // Default to level 2
            }

            SelectedThirdPartyApp = ConfigHelper.GetConfig("default_third_party_app");
            if (string.IsNullOrEmpty(SelectedThirdPartyApp))
            {
                SelectedThirdPartyApp = "CorelDRAW";
            }

            if (bool.TryParse(ConfigHelper.GetConfig("auto_check_template_update"), out bool checkTemp))
            {
                AutoCheckTemplateUpdate = checkTemp;
            }
            
            isLoading = false;
        }

        private void ExecuteSelectCorelDRAWPath()
        {
            string path = FileHelper.GetFilePath("CorelDRAW Executable (*.exe)|*.exe");
            if (!string.IsNullOrEmpty(path))
            {
                CorelDRAWPath = path;
                ConfigHelper.SetConfig("coreldraw_path", path);
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        private void ExecuteSelectInkscapePath()
        {
            string path = FileHelper.GetFilePath("Inkscape Executable (*.exe)|*.exe");
            if (!string.IsNullOrEmpty(path))
            {
                InkscapePath = path;
                ConfigHelper.SetConfig("inkscape_path", path);
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        private void ExecuteSelectAdobeIllustratorPath()
        {
            string path = FileHelper.GetFilePath("Adobe Illustrator Executable (*.exe)|*.exe");
            if (!string.IsNullOrEmpty(path))
            {
                AdobeIllustratorPath = path;
                ConfigHelper.SetConfig("adobe_illustrator_path", path);
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        private void ExecuteSelectCustomThirdPartyAppPath()
        {
            string path = FileHelper.GetFilePath("Executable (*.exe)|*.exe");
            if (!string.IsNullOrEmpty(path))
            {
                CustomThirdPartyAppPath = path;
                ConfigHelper.SetConfig("custom_third_party_app_path", path);
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        partial void OnSelectedThirdPartyAppChanged(string value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig("default_third_party_app", value);
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnAutoCheckTemplateUpdateChanged(bool value)
        {
            if (isLoading) return;
             ConfigHelper.SetConfig("auto_check_template_update", value.ToString());
             MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnDefaultTreeExpandLevelChanged(int value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig("default_tree_expand_level", value.ToString());
            
            // 发送消息通知
            WeakReferenceMessenger.Default.Send(new DefaultTreeExpandLevelChangedMessage(value));

            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }
    }
}
