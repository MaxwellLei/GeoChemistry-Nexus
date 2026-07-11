using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views.Widgets
{
    public enum HomeLinkLocalizedEditMode
    {
        Group,
        Link
    }

    public partial class HomeLinkLocalizedEditWindow : HandyControl.Controls.Window
    {
        public ContentLanguageContext LanguageContext { get; }

        public LocalizedString ResultTitle { get; private set; } = new();

        public LocalizedString ResultDescription { get; private set; } = new();

        public string ResultUrl { get; private set; } = string.Empty;

        public string ResultIcon { get; private set; } = HomeIconHelper.DefaultIcon;

        private readonly HomeLinkLocalizedEditMode _mode;

        public HomeLinkLocalizedEditWindow(
            HomeLinkLocalizedEditMode mode,
            ContentLanguageContext languageContext,
            string windowTitle,
            LocalizedString? title = null,
            LocalizedString? description = null,
            string? url = null,
            string? icon = null)
        {
            InitializeComponent();
            UiScaleHelper.Attach(this);
            _mode = mode;
            LanguageContext = languageContext;
            Title = windowTitle;

            LanguageBox.ItemsSource = AppCultureRegistry.GetContentOptions();
            LanguageBox.SelectedValue = languageContext.ContentLanguage
                ?? AppCultureRegistry.DefaultContentLanguage;

            TitleControl.Value = HomeLinksLocalization.Clone(title);
            DescriptionControl.Value = HomeLinksLocalization.Clone(description);

            bool isLinkMode = mode == HomeLinkLocalizedEditMode.Link;
            DescriptionLabel.Visibility = isLinkMode ? Visibility.Visible : Visibility.Collapsed;
            DescriptionControl.Visibility = isLinkMode ? Visibility.Visible : Visibility.Collapsed;
            UrlBox.Visibility = isLinkMode ? Visibility.Visible : Visibility.Collapsed;
            IconBox.Visibility = isLinkMode ? Visibility.Visible : Visibility.Collapsed;
            IconUrlBox.Visibility = isLinkMode ? Visibility.Visible : Visibility.Collapsed;

            if (isLinkMode)
            {
                UrlBox.Text = url ?? string.Empty;
                InitializeIcons(icon);
            }
            else
            {
                Height = 280;
            }
        }

        private void InitializeIcons(string? icon)
        {
            IconBox.ItemsSource = HomeIconHelper.CreatePresetIcons();
            LoadIcon(icon);
        }

        private void LoadIcon(string? icon)
        {
            if (HomeIconHelper.IsUrlIcon(icon))
            {
                IconUrlBox.Text = icon?.Trim() ?? string.Empty;
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

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageBox.SelectedValue is string code)
                LanguageContext.ContentLanguage = code;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            TitleControl.CommitPendingText();
            DescriptionControl.CommitPendingText();

            if (!HomeLinksLocalization.HasText(TitleControl.Value, LanguageContext))
            {
                HandyControl.Controls.Growl.Warning(LanguageService.Instance["please_enter_name"]);
                return;
            }

            if (_mode == HomeLinkLocalizedEditMode.Link && string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                HandyControl.Controls.Growl.Warning(LanguageService.Instance["please_enter_link"]);
                return;
            }

            ResultTitle = HomeLinksLocalization.Clone(TitleControl.Value);
            ResultDescription = HomeLinksLocalization.Clone(DescriptionControl.Value);
            ResultUrl = UrlBox.Text?.Trim() ?? string.Empty;
            ResultIcon = !string.IsNullOrWhiteSpace(IconUrlBox.Text)
                ? IconUrlBox.Text.Trim()
                : IconBox.SelectedValue?.ToString() ?? HomeIconHelper.DefaultIcon;

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
