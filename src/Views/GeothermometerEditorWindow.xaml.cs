using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

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
            EditingCommands.ToggleBold.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleItalic.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleUnderline.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
    }
}
