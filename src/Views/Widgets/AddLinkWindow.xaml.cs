using System.Collections.Generic;
using System.Windows;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class AddLinkWindow : HandyControl.Controls.Window
    {
        public HomeAppItem Result { get; private set; }

        public class IconItem
        {
            public string Name { get; set; }
            public string Code { get; set; }
        }

        public AddLinkWindow()
        {
            InitializeComponent();
            
            var icons = new List<IconItem>
            {
                new IconItem { Name = "网页 (Globe)", Code = "\uE774" },
                new IconItem { Name = "链接 (Link)", Code = "\uE71B" },
                new IconItem { Name = "收藏 (Star)", Code = "\uE734" },
                new IconItem { Name = "主页 (Home)", Code = "\uE80F" },
                new IconItem { Name = "文档 (Document)", Code = "\uE8A5" },
                new IconItem { Name = "邮件 (Mail)", Code = "\uE715" },
                new IconItem { Name = "云端 (Cloud)", Code = "\uE753" },
                new IconItem { Name = "设置 (Settings)", Code = "\uE713" },
                new IconItem { Name = "搜索 (Search)", Code = "\uE721" }
            };
            
            IconBox.ItemsSource = icons;
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

        private void Ok_Click(object sender, RoutedEventArgs e)
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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
