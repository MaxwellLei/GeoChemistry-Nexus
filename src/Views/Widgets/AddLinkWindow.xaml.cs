using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class AddLinkWindow : Window, INotificationCenterHost
    {
        public HomeAppItem? Result { get; private set; }

        /// <summary>
        /// 本窗口独立的通知中心（由 XAML 中的 NotificationHost 呈现）。
        /// </summary>
        public NotificationCenter Notifications { get; } = new();

        public AddLinkWindow()
        {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => Close()));
            UiScaleHelper.Attach(this);
            ChildWindowPlacementHelper.Attach(this);

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
                Notifications.Warning(LanguageService.Instance["please_enter_name"]);
                return;
            }

            if (string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                // 请输入链接
                Notifications.Warning(LanguageService.Instance["please_enter_link"]);
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
    }
}
