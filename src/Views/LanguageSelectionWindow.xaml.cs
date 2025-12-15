using System.Collections.Generic;
using System.Windows;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Views
{
    public partial class LanguageSelectionWindow : HandyControl.Controls.Window
    {
        public string SelectedLanguage { get; private set; }

        public LanguageSelectionWindow(IEnumerable<string> languages, string currentLanguage)
        {
            InitializeComponent();
            
            LanguageComboBox.ItemsSource = languages;
            LanguageComboBox.SelectedItem = currentLanguage;
            
            bool isChinese = LanguageService.CurrentLanguage == "zh-CN";
            Title = isChinese ? "选择语言" : "Select Language";
            PromptText.Text = isChinese ? "请选择语言" : "Please select a language";
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is string lang)
            {
                SelectedLanguage = lang;
                DialogResult = true;
                Close();
            }
            else
            {
                bool isChinese = LanguageService.CurrentLanguage == "zh-CN";
                HandyControl.Controls.Growl.Warning(isChinese ? "请选择一种语言" : "Please select a language");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}