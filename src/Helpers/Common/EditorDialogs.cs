using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

    public sealed class CategoryStructureSelectDialog : Window
    {
        public CategoryStructureSelectNode? SelectedNode { get; private set; }

        public CategoryStructureSelectDialog(
            IReadOnlyList<CategoryStructureSelectNode> roots,
            Window? owner = null)
        {
            Title = LanguageService.Instance["select_existing_category_structure_title"]
                    ?? "Select Category Structure";
            MinWidth = 420;
            MinHeight = 360;
            Width = 480;
            Height = 440;
            WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            ShowInTaskbar = false;
            Owner = owner;

            var root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            root.Children.Add(new TextBlock
            {
                Text = LanguageService.Instance["select_existing_category_structure_prompt"]
                       ?? "Select a category path from existing diagram templates. You can continue adding custom levels afterward.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var treeView = new TreeView
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xEA)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4)
            };
            Grid.SetRow(treeView, 1);
            root.Children.Add(treeView);

            var selectedPathText = new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x86, 0x86, 0x8B)),
                FontSize = 12
            };
            Grid.SetRow(selectedPathText, 2);
            root.Children.Add(selectedPathText);

            var buttons = EditorDialogUi.CreateButtonPanel((_, _) =>
            {
                if (TryGetSelectedNode(treeView, out var node))
                {
                    SelectedNode = node;
                    DialogResult = true;
                }
            });
            Grid.SetRow(buttons, 3);
            root.Children.Add(buttons);

            var okButton = buttons.Children.OfType<Button>().LastOrDefault(b => b.IsDefault);
            if (okButton != null)
                okButton.IsEnabled = false;

            if (roots == null || roots.Count == 0)
            {
                treeView.Items.Add(new TextBlock
                {
                    Text = LanguageService.Instance["select_existing_category_structure_empty"]
                           ?? "No existing category structures found.",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x86, 0x86, 0x8B)),
                    Margin = new Thickness(8)
                });
            }
            else
            {
                foreach (var item in roots)
                    treeView.Items.Add(CreateTreeItem(item));

                if (treeView.Items.Count == 1 && treeView.Items[0] is TreeViewItem firstItem)
                    firstItem.IsExpanded = true;
            }

            treeView.SelectedItemChanged += (_, _) =>
            {
                if (TryGetSelectedNode(treeView, out var node))
                {
                    selectedPathText.Text = PlotCategoryStructureHelper.BuildDisplayPath(node);
                    if (okButton != null)
                        okButton.IsEnabled = true;
                }
                else
                {
                    selectedPathText.Text = string.Empty;
                    if (okButton != null)
                        okButton.IsEnabled = false;
                }
            };

            Content = root;
        }

        private static bool TryGetSelectedNode(TreeView treeView, out CategoryStructureSelectNode node)
        {
            node = null!;
            if (treeView.SelectedItem is TreeViewItem item &&
                item.Tag is CategoryStructureSelectNode selectNode &&
                selectNode.IsSelectable)
            {
                node = selectNode;
                return true;
            }

            return false;
        }

        private static TreeViewItem CreateTreeItem(CategoryStructureSelectNode node)
        {
            var item = new TreeViewItem
            {
                Header = node.Name,
                Tag = node,
                IsExpanded = node.Children.Count > 0 && node.PrefixLength <= 1
            };

            foreach (var child in node.Children)
                item.Items.Add(CreateTreeItem(child));

            return item;
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
