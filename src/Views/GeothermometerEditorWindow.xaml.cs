using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

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
            UiScaleHelper.Attach(this);
        }

        public GeothermometerEditorWindow(GeothermometerEditorViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.OnCloseRequested = () => Close();
            viewModel.ShowErrorMessage = ShowErrorMessage;
            viewModel.ShowSuccessMessage = ShowSuccessMessage;
            viewModel.OnTagSuggestionsNeedRefresh = () =>
                Dispatcher.Invoke(() => EditorControl.BasicInfoPanel.RefreshTagSuggestions(viewModel));

            // 构造时显式传入 VM：此时子控件 DataContext 绑定可能尚未生效
            EditorControl.BasicInfoPanel.RefreshTagSuggestions(viewModel);
            Loaded += (_, _) => EditorControl.BasicInfoPanel.RefreshTagSuggestions(viewModel);
            LanguageService.Instance.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    Dispatcher.Invoke(() => EditorControl.BasicInfoPanel.RefreshTagSuggestions(viewModel));
            };

            var helpEditor = EditorControl.HelpDocPanel.HelpDocEditorControl;
            viewModel.GetCurrentRtfContent = () =>
            {
                try
                {
                    return RtfHelper.GetRtfString(helpEditor);
                }
                catch
                {
                    return null;
                }
            };

            viewModel.SetCurrentRtfContent = rtf =>
            {
                try
                {
                    if (string.IsNullOrEmpty(rtf))
                    {
                        helpEditor.Document.Blocks.Clear();
                    }
                    else
                    {
                        RtfHelper.LoadRtfString(helpEditor, rtf);
                    }
                }
                catch
                {
                    helpEditor.Document.Blocks.Clear();
                }
            };
        }

        private void ShowSuccessMessage(string message)
        {
            ShowLocalNotification("Success", message, NotificationType.Success, MessageHelper.WaitTime);
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
    }
}
