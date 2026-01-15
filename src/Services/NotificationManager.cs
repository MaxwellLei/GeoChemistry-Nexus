using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Services
{
    public partial class NotificationManager : ObservableObject
    {
        private static NotificationManager? _instance;
        public static NotificationManager Instance => _instance ??= new NotificationManager();

        public ObservableCollection<NotificationViewModel> Notifications { get; } = new ObservableCollection<NotificationViewModel>();

        [ObservableProperty]
        private bool isModal;

        private NotificationManager() 
        {
            Notifications.CollectionChanged += Notifications_CollectionChanged;
        }

        private void Notifications_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateModalStatus();
        }

        private void UpdateModalStatus()
        {
            IsModal = Notifications.Any(n => n.IsInteractive);
        }

        public void Show(string title, string message, NotificationType type, int durationSeconds = 3)
        {
            // 警告和错误通知不自动关闭
            if (type == NotificationType.Warning || type == NotificationType.Error)
            {
                durationSeconds = 0;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(title, message, type, RemoveNotification);
                Notifications.Add(vm);

                if (durationSeconds > 0)
                {
                    Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
                    {
                        Application.Current.Dispatcher.Invoke(async () => await vm.Close());
                    });
                }
            });
        }

        public Task<bool> ShowDialogAsync(string title, string message, string confirmText, string cancelText)
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(title, message, NotificationType.Info, RemoveNotification)
                {
                    IsInteractive = true,
                    ConfirmText = confirmText,
                    CancelText = cancelText,
                    DialogResultAction = (result) => tcs.SetResult(result)
                };
                Notifications.Add(vm);
            });

            return tcs.Task;
        }

        /// <summary>
        /// 显示三按钮对话框（保存/不保存/取消）
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">对话框消息</param>
        /// <param name="confirmText">确认按钮文本（保存）</param>
        /// <param name="thirdButtonText">第三个按钮文本（不保存）</param>
        /// <param name="cancelText">取消按钮文本</param>
        /// <returns>0=确认/保存, 1=第三个按钮/不保存, 2=取消</returns>
        public Task<int> ShowThreeButtonDialogAsync(string title, string message, string confirmText, string thirdButtonText, string cancelText)
        {
            var tcs = new TaskCompletionSource<int>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(title, message, NotificationType.Info, RemoveNotification)
                {
                    IsInteractive = true,
                    IsThreeButtonDialog = true,
                    ConfirmText = confirmText,
                    ThirdButtonText = thirdButtonText,
                    CancelText = cancelText,
                    ThreeButtonDialogResultAction = (result) => tcs.SetResult(result)
                };
                Notifications.Add(vm);
            });

            return tcs.Task;
        }

        public Task<string?> ShowLanguageSelectionAsync(IEnumerable<string> languages, string currentLanguage)
        {
            var tcs = new TaskCompletionSource<string?>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(
                    LanguageService.Instance["select_language"] ?? "Select Language", 
                    LanguageService.Instance["please_select_language"] ?? "Please select a language", 
                    NotificationType.Info, 
                    RemoveNotification)
                {
                    IsInteractive = true,
                    IsLanguageSelection = true,
                    Languages = new ObservableCollection<string>(languages),
                    SelectedLanguage = currentLanguage,
                    LanguageSelectionAction = (result) => tcs.SetResult(result),
                    DialogResultAction = (result) => { if (!result) tcs.SetResult(null); } // Handle cancel
                };
                Notifications.Add(vm);
            });

            return tcs.Task;
        }

        private void RemoveNotification(NotificationViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Notifications.Contains(vm))
                {
                    Notifications.Remove(vm);
                }
            });
        }
    }
}
