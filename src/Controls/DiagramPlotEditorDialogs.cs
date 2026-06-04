using GeoChemistryNexus.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    internal sealed class SimpleInputDialog : Window
    {
        public string InputText { get; private set; } = string.Empty;
        private readonly TextBox _textBox;

        public SimpleInputDialog(string prompt, string title, Window? owner)
        {
            Title = title;
            MinWidth = 380;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Owner = owner;

            var root = new StackPanel { Margin = new Thickness(24) };
            root.Children.Add(new TextBlock { Text = prompt, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
            _textBox = new TextBox();
            root.Children.Add(_textBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            var cancel = new Button
            {
                Content = LanguageService.Instance["Cancel"] ?? "Cancel",
                Width = 88,
                IsCancel = true
            };
            var ok = new Button
            {
                Content = LanguageService.Instance["Confirm"] ?? "OK",
                Width = 88,
                Margin = new Thickness(10, 0, 0, 0),
                IsDefault = true
            };
            ok.Click += (_, _) => { InputText = _textBox.Text; DialogResult = true; };
            buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            root.Children.Add(buttons);
            Content = root;
        }
    }

    internal sealed class LanguageSwapDialog : Window
    {
        public string? Lang1 { get; private set; }
        public string? Lang2 { get; private set; }
        private readonly ComboBox _combo1;
        private readonly ComboBox _combo2;

        public LanguageSwapDialog(IEnumerable<string> languages, Window? owner)
        {
            Title = LanguageService.Instance["swap_language_columns"] ?? "Swap Languages";
            MinWidth = 360;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Owner = owner;

            var root = new StackPanel { Margin = new Thickness(24) };
            root.Children.Add(new TextBlock { Text = LanguageService.Instance["swap_language_columns"] ?? "Swap", Margin = new Thickness(0, 0, 0, 8) });
            _combo1 = new ComboBox { ItemsSource = languages };
            _combo1.SelectedIndex = 0;
            root.Children.Add(_combo1);
            root.Children.Add(new TextBlock { Text = "↔", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 8) });
            _combo2 = new ComboBox { ItemsSource = languages };
            _combo2.SelectedIndex = 1;
            root.Children.Add(_combo2);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            buttons.Children.Add(new Button { Content = LanguageService.Instance["Cancel"] ?? "Cancel", Width = 88, IsCancel = true });
            var ok = new Button { Content = LanguageService.Instance["Confirm"] ?? "OK", Width = 88, Margin = new Thickness(10, 0, 0, 0), IsDefault = true };
            ok.Click += (_, _) =>
            {
                Lang1 = _combo1.SelectedItem as string;
                Lang2 = _combo2.SelectedItem as string;
                DialogResult = true;
            };
            buttons.Children.Add(ok);
            root.Children.Add(buttons);
            Content = root;
        }
    }

    internal sealed class LanguageSelectDialog : Window
    {
        public string? SelectedLanguage { get; private set; }
        private readonly ComboBox _combo;

        public LanguageSelectDialog(IEnumerable<string> languages, string prompt, Window? owner)
        {
            Title = LanguageService.Instance["delete_language_column"] ?? "Delete Language";
            MinWidth = 340;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Owner = owner;

            var root = new StackPanel { Margin = new Thickness(24) };
            root.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) });
            _combo = new ComboBox { ItemsSource = languages };
            _combo.SelectedIndex = 0;
            root.Children.Add(_combo);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            buttons.Children.Add(new Button { Content = LanguageService.Instance["Cancel"] ?? "Cancel", Width = 88, IsCancel = true });
            var ok = new Button { Content = LanguageService.Instance["Confirm"] ?? "OK", Width = 88, Margin = new Thickness(10, 0, 0, 0), IsDefault = true };
            ok.Click += (_, _) => { SelectedLanguage = _combo.SelectedItem as string; DialogResult = true; };
            buttons.Children.Add(ok);
            root.Children.Add(buttons);
            Content = root;
        }
    }
}
