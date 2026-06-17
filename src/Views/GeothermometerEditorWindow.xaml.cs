using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// GeothermometerEditorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GeothermometerEditorWindow : Window
    {
        public ObservableCollection<NotificationViewModel> LocalNotifications { get; } = new();

        public GeothermometerEditorWindow()
        {
            InitializeComponent();
        }

        public GeothermometerEditorWindow(GeothermometerEditorViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.OnCloseRequested = () => this.Close();
            viewModel.ShowErrorMessage = ShowErrorMessage;
            viewModel.ShowSuccessMessage = ShowSuccessMessage;

            RefreshTagSuggestions();
            LanguageService.Instance.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    Dispatcher.Invoke(RefreshTagSuggestions);
            };

            // 连接 ViewModel 与 RichTextBox 的回调
            viewModel.GetCurrentRtfContent = () =>
            {
                try
                {
                    return RtfHelper.GetRtfString(HelpDocEditor);
                }
                catch
                {
                    return null;
                }
            };

            viewModel.SetCurrentRtfContent = (rtf) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(rtf))
                    {
                        HelpDocEditor.Document.Blocks.Clear();
                    }
                    else
                    {
                        RtfHelper.LoadRtfString(HelpDocEditor, rtf);
                    }
                }
                catch
                {
                    HelpDocEditor.Document.Blocks.Clear();
                }
            };
        }

        private void ShowSuccessMessage(string message)
        {
            ShowLocalNotification("Success", message, NotificationType.Success, MessageHelper.waitTime);
        }

        private void ShowErrorMessage(string message)
        {
            ShowLocalNotification("Error", message, NotificationType.Error);
        }

        private void ShowLocalNotification(string title, string message, NotificationType type, int durationSeconds = 0)
        {
            Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(title, message, type, RemoveNotification);
                LocalNotifications.Add(vm);

                if (durationSeconds > 0)
                {
                    _ = Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
                    {
                        Dispatcher.BeginInvoke(new Action(async () => await vm.Close()));
                    });
                }
            });
        }

        private void RemoveNotification(NotificationViewModel vm)
        {
            Dispatcher.Invoke(() =>
            {
                if (LocalNotifications.Contains(vm))
                {
                    LocalNotifications.Remove(vm);
                }
            });
        }

        // ==================== RTF 格式化按钮事件 ====================

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm && vm.IsContentReadOnly) return;
            EditingCommands.ToggleBold.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm && vm.IsContentReadOnly) return;
            EditingCommands.ToggleItalic.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm && vm.IsContentReadOnly) return;
            EditingCommands.ToggleUnderline.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm && vm.IsContentReadOnly) return;
            if (HelpDocEditor?.Selection == null) return;

            string sizeText = null;

            if (FontSizeComboBox.SelectedItem is ComboBoxItem item)
            {
                sizeText = item.Content?.ToString();
            }
            else if (FontSizeComboBox.SelectedItem is string s)
            {
                sizeText = s;
            }

            if (sizeText != null && double.TryParse(sizeText, out double size) && size > 0 && size <= 200)
            {
                HelpDocEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                HelpDocEditor.Focus();
            }
        }

        private void TagCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is GeothermometerEditorViewModel vm &&
                vm.TryAddTag(TagCombo.Text?.Trim() ?? string.Empty))
            {
                TagCombo.Text = string.Empty;
            }
            e.Handled = true;
        }

        private void TagCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not GeothermometerEditorViewModel vm) return;
            if (!TagCombo.IsDropDownOpen)
            {
                TagCombo.SelectedIndex = -1;
                return;
            }

            if (TagCombo.SelectedItem is TagDisplayItem item &&
                vm.TryAddTag(item.DisplayName, item.OriginalObject))
            {
                TagCombo.SelectedIndex = -1;
                TagCombo.Text = string.Empty;
            }

            RefreshTagSuggestions();
        }

        private void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm &&
                (sender as Button)?.Tag is GeoTTagModel item)
            {
                vm.RemoveTag(item);
            }
        }

        private void RefreshTagSuggestions()
        {
            if (DataContext is not GeothermometerEditorViewModel vm) return;

            var list = vm.GetTagSuggestions();
            TagCombo.ItemsSource = list.Select(entry =>
            {
                string displayName = AppCultureRegistry.GetLocalizedValue(
                    entry,
                    LanguageService.CurrentLanguage,
                    AppCultureRegistry.DefaultAppLanguage);
                if (string.IsNullOrWhiteSpace(displayName) &&
                    entry.TryGetValue("zh-CN", out var zh) &&
                    !string.IsNullOrWhiteSpace(zh))
                {
                    displayName = zh;
                }

                return new TagDisplayItem
                {
                    DisplayName = displayName ?? string.Empty,
                    OriginalObject = entry
                };
            }).ToList();
        }

        private class TagDisplayItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public Dictionary<string, string>? OriginalObject { get; set; }
        }
    }
}
