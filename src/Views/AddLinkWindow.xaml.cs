using System.Collections.Generic;
using System.Windows;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Views
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

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                HandyControl.Controls.Growl.Warning("请输入名称");
                return;
            }

            if (string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                HandyControl.Controls.Growl.Warning("请输入链接");
                return;
            }

            Result = new HomeAppItem
            {
                Type = HomeAppType.WebLink,
                Title = TitleBox.Text.Trim(),
                Url = UrlBox.Text.Trim(),
                Description = DescBox.Text?.Trim() ?? "",
                Icon = IconBox.SelectedValue?.ToString() ?? "\uE774"
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
