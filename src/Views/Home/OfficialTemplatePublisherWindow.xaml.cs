using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Views
{
    public partial class OfficialTemplatePublisherWindow : Widgets.WidgetWindow, INotificationCenterHost
    {
        /// <summary>
        /// 本窗口独立的通知中心（由 XAML 中的 NotificationHost 呈现）。
        /// </summary>
        public NotificationCenter Notifications { get; } = new();

        private readonly OfficialTemplatePublisherViewModel _viewModel;

        public OfficialTemplatePublisherWindow()
        {
            InitializeComponent();
            UiScaleHelper.Attach(this);
            WindowActivationHelper.AttachOwnerFocusPreservation(this);

            Title = "官方发布器";

            var viewModel = new OfficialTemplatePublisherViewModel();
            _viewModel = viewModel;
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

        public void ShowSuccessMessage(string message) => Notifications.Success(message);

        public void ShowWarningMessage(string message) => Notifications.Warning(message);

        public void ShowErrorMessage(string message) => Notifications.Error(message);

        public Task<bool> ShowConfirmDialogAsync(string title, string message, string confirmText, string cancelText) =>
            Notifications.ShowDialogAsync(title, message, confirmText, cancelText);

        protected override void OnClosed(EventArgs e)
        {
            // 编辑后 0.8 秒内就关窗口时，待保存的草稿也要落盘
            _viewModel.FlushPendingDrafts();
            Notifications.Dispose();
            base.OnClosed(e);
        }
    }
}
