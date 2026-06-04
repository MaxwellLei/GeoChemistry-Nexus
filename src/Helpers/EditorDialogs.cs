using GeoChemistryNexus.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Helpers
{
    public sealed class TextInputDialog : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public TextInputDialog(string prompt, string title, Window? owner = null, string defaultValue = "")
        {
            Title = title;
            MinWidth = 380;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Owner = owner;

            var root = new StackPanel { Margin = new Thickness(24) };
            root.Children.Add(new TextBlock
            {
                Text = prompt,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var textBox = new TextBox { Text = defaultValue };
            root.Children.Add(textBox);

            var buttons = EditorDialogUi.CreateButtonPanel((_, _) =>
            {
                InputText = textBox.Text;
                DialogResult = true;
            });
            root.Children.Add(buttons);
            Content = root;
        }
    }

    public sealed class DualComboSelectDialog : Window
    {
        public string? FirstSelection { get; private set; }
        public string? SecondSelection { get; private set; }

        public DualComboSelectDialog(
            IEnumerable<string> items,
            string title,
            string? prompt = null,
            Window? owner = null,
            string? firstLabel = null,
            string? secondLabel = null)
        {
            var itemList = items.ToList();

            Title = title;
            MinWidth = 360;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Owner = owner;

            var root = new StackPanel { Margin = new Thickness(24) };
            var combo1 = new ComboBox { ItemsSource = itemList, SelectedIndex = 0 };
            var combo2 = new ComboBox { ItemsSource = itemList, SelectedIndex = itemList.Count > 1 ? 1 : 0 };

            if (!string.IsNullOrEmpty(firstLabel) || !string.IsNullOrEmpty(secondLabel))
            {
                root.Children.Add(new TextBlock
                {
                    Text = firstLabel ?? string.Empty,
                    Margin = new Thickness(0, 0, 0, 5)
                });
                root.Children.Add(combo1);
                root.Children.Add(new TextBlock
                {
                    Text = secondLabel ?? string.Empty,
                    Margin = new Thickness(0, 15, 0, 5)
                });
                root.Children.Add(combo2);
            }
            else
            {
                if (!string.IsNullOrEmpty(prompt))
                {
                    root.Children.Add(new TextBlock
                    {
                        Text = prompt,
                        Margin = new Thickness(0, 0, 0, 8)
                    });
                }

                root.Children.Add(combo1);
                root.Children.Add(new TextBlock
                {
                    Text = "↔",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 8)
                });
                root.Children.Add(combo2);
            }

            var buttons = EditorDialogUi.CreateButtonPanel((_, _) =>
            {
                FirstSelection = combo1.SelectedItem as string;
                SecondSelection = combo2.SelectedItem as string;
                DialogResult = true;
            });
            root.Children.Add(buttons);
            Content = root;
        }
    }

    public sealed class SingleComboSelectDialog : Window
    {
        public string? SelectedItem { get; private set; }

        public SingleComboSelectDialog(
            IEnumerable<string> items,
            string prompt,
            string? title = null,
            Window? owner = null)
        {
            Title = title ?? LanguageService.Instance["tips"] ?? "Select";
            MinWidth = 340;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Owner = owner;

            var root = new StackPanel { Margin = new Thickness(24) };
            root.Children.Add(new TextBlock
            {
                Text = prompt,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var combo = new ComboBox { ItemsSource = items, SelectedIndex = 0 };
            root.Children.Add(combo);

            var buttons = EditorDialogUi.CreateButtonPanel((_, _) =>
            {
                SelectedItem = combo.SelectedItem as string;
                DialogResult = true;
            });
            root.Children.Add(buttons);
            Content = root;
        }
    }

    internal static class EditorDialogUi
    {
        internal static StackPanel CreateButtonPanel(RoutedEventHandler okClick)
        {
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            buttons.Children.Add(new Button
            {
                Content = LanguageService.Instance["Cancel"] ?? "Cancel",
                Width = 88,
                IsCancel = true
            });

            var ok = new Button
            {
                Content = LanguageService.Instance["Confirm"] ?? "OK",
                Width = 88,
                Margin = new Thickness(10, 0, 0, 0),
                IsDefault = true
            };
            ok.Click += okClick;
            buttons.Children.Add(ok);
            return buttons;
        }
    }
}
