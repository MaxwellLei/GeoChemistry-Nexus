using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
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
        private int defaultPlotLanguage; // 0: zh-CN, 1: en-US

        // 第三方应用列表
        public ObservableCollection<string> ThirdPartyApps { get; } = new() { "CorelDRAW", "Inkscape", "Adobe Illustrator" };

        [ObservableProperty]
        private string selectedThirdPartyApp;

        [ObservableProperty]
        private bool autoCheckTemplateUpdate;

        [ObservableProperty]
        private bool autoCheckClassStructUpdate;

        public RelayCommand SelectCorelDRAWPathCommand { get; }
        public RelayCommand SelectInkscapePathCommand { get; }
        public RelayCommand SelectAdobeIllustratorPathCommand { get; }
        public RelayCommand DefaultPlotLanguageChangedCommand { get; }

        private bool isLoading = true;

        public SPlotPageViewModel()
        {
            SelectCorelDRAWPathCommand = new RelayCommand(ExecuteSelectCorelDRAWPath);
            SelectInkscapePathCommand = new RelayCommand(ExecuteSelectInkscapePath);
            SelectAdobeIllustratorPathCommand = new RelayCommand(ExecuteSelectAdobeIllustratorPath);
            DefaultPlotLanguageChangedCommand = new RelayCommand(ExecuteDefaultPlotLanguageChanged);

            LoadConfig();
        }

        private void LoadConfig()
        {
            CorelDRAWPath = ConfigHelper.GetConfig("coreldraw_path");
            InkscapePath = ConfigHelper.GetConfig("inkscape_path");
            AdobeIllustratorPath = ConfigHelper.GetConfig("adobe_illustrator_path");
            
            if (int.TryParse(ConfigHelper.GetConfig("default_plot_language"), out int lang))
            {
                DefaultPlotLanguage = lang;
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
            
            if (bool.TryParse(ConfigHelper.GetConfig("auto_check_class_struct_update"), out bool checkClass))
            {
                AutoCheckClassStructUpdate = checkClass;
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

        private void ExecuteDefaultPlotLanguageChanged()
        {
            if (isLoading) return;
            ConfigHelper.SetConfig("default_plot_language", DefaultPlotLanguage.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
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

        partial void OnAutoCheckClassStructUpdateChanged(bool value)
        {
             if (isLoading) return;
             ConfigHelper.SetConfig("auto_check_class_struct_update", value.ToString());
             MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }
    }
}
