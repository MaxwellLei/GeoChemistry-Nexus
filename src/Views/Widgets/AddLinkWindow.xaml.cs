using System.Collections.Generic;
using System.Windows;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class AddLinkWindow : HandyControl.Controls.Window
    {
        public HomeAppItem? Result { get; private set; }

        public AddLinkWindow()
        {
            InitializeComponent();
            UiScaleHelper.Attach(this);

            IconBox.ItemsSource = HomeIconHelper.CreatePresetIcons();
            IconBox.SelectedIndex = 0;
        }

        public void LoadIcon(string icon)
        {
            if (HomeIconHelper.IsUrlIcon(icon))
            {
                IconUrlBox.Text = icon.Trim();
                IconBox.SelectedIndex = 0;
                return;
            }

            IconUrlBox.Text = string.Empty;
            if (string.IsNullOrWhiteSpace(icon))
            {
                IconBox.SelectedIndex = 0;
                return;
            }

            foreach (IconItem item in IconBox.Items)
            {
                if (item.Code == icon)
                {
                    IconBox.SelectedItem = item;
                    return;
                }
            }

            IconBox.SelectedIndex = 0;
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                // 请输入名称
                HandyControl.Controls.Growl.Warning(LanguageService.Instance["please_enter_name"]);
                return;
            }

            if (string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                // 请输入链接
                HandyControl.Controls.Growl.Warning(LanguageService.Instance["please_enter_link"]);
                return;
            }

            string icon = !string.IsNullOrWhiteSpace(IconUrlBox.Text)
                ? IconUrlBox.Text.Trim()
                : IconBox.SelectedValue?.ToString() ?? HomeIconHelper.DefaultIcon;

            Result = new HomeAppItem
            {
                Type = HomeAppType.WebLink,
                Title = TitleBox.Text.Trim(),
                Url = UrlBox.Text.Trim(),
                Description = DescBox.Text?.Trim() ?? "",
                Icon = icon
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
