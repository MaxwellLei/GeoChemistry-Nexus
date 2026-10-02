using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Views
{
    public partial class OfficialTemplatePublisherWindow : Window
    {
        public ObservableCollection<NotificationViewModel> LocalNotifications { get; } = new();

        public OfficialTemplatePublisherWindow()
        {
            InitializeComponent();
            UiScaleHelper.Attach(this);

            Title = LanguageService.Instance["official_template_publisher"];

            var viewModel = new OfficialTemplatePublisherViewModel();
            PublisherWidget.DataContext = viewModel;
            viewModel.ShowSuccessMessage = ShowSuccessMessage;
            viewModel.ShowWarningMessage = ShowWarningMessage;
            viewModel.ShowErrorMessage = ShowErrorMessage;
            viewModel.ShowConfirmDialogAsync = (message, cancelText, confirmText) =>
                ShowConfirmDialogAsync(
                    LanguageService.Instance["Confirm"] ?? "Confirm",
                    message,
                    confirmText,
                    cancelText);
            viewModel.OwnerWindow = this;
        }

        public void ShowSuccessMessage(string message) =>
            ShowLocalNotification(
                LanguageService.Instance["notification_success"] ?? "Success",
                message,
                NotificationType.Success,
                MessageHelper.WaitTime);

        public void ShowWarningMessage(string message) =>
            ShowLocalNotification(
                LanguageService.Instance["notification_warning"] ?? "Warning",
                message,
                NotificationType.Warning);

        public void ShowErrorMessage(string message) =>
            ShowLocalNotification(
                LanguageService.Instance["error"] ?? "Error",
                message,
                NotificationType.Error);

        public Task<bool> ShowConfirmDialogAsync(string title, string message, string confirmText, string cancelText)
        {
            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(title, message, NotificationType.Info, RemoveNotification)
                {
                    IsInteractive = true,
                    ConfirmText = confirmText,
                    CancelText = cancelText,
                    DialogResultAction = result => tcs.TrySetResult(result)
                };
                LocalNotifications.Add(vm);
            });
            return tcs.Task;
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
                        Dispatcher.BeginInvoke(new Action(async () => await vm.Close())));
                }
            });
        }

        private void RemoveNotification(NotificationViewModel vm)
        {
            Dispatcher.Invoke(() =>
            {
                if (LocalNotifications.Contains(vm))
                    LocalNotifications.Remove(vm);
            });
        }
    }
}
