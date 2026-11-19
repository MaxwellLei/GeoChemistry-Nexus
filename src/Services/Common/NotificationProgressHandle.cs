using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 进度通知的控制句柄：由 <see cref="NotificationCenter.ShowProgress"/> 返回，
    /// 长任务通过它汇报进度、响应用户取消，并在结束时让通知原地转为成功 / 错误。
    /// 所有方法都可在任意线程调用，内部会切回 UI 线程。
    /// </summary>
    public sealed class NotificationProgressHandle : IDisposable
    {
        private readonly NotificationCenter _center;
        private readonly NotificationViewModel _viewModel;
        private readonly CancellationTokenSource _cts = new();
        private readonly string _cancellingText;
        private bool _isFinished;

        internal NotificationProgressHandle(
            NotificationCenter center,
            NotificationViewModel viewModel,
            bool cancellable,
            string cancelText,
            string cancellingText)
        {
            _center = center;
            _viewModel = viewModel;
            _cancellingText = cancellingText;

            if (cancellable)
            {
                // 点击“取消”后卡片不关闭，由任务观察 CancellationToken 后调用 Fail / Dismiss 收尾
                viewModel.Actions = new[]
                {
                    new NotificationAction(cancelText, RequestCancel, isPrimary: false, closeOnInvoke: false)
                };
            }
        }

        /// <summary>用户点击“取消”后被触发的令牌。</summary>
        public CancellationToken CancellationToken => _cts.Token;

        public bool IsCancellationRequested => _cts.IsCancellationRequested;

        /// <summary>对应的通知视图模型（需要时可进一步定制）。</summary>
        public NotificationViewModel Notification => _viewModel;

        /// <summary>汇报进度。</summary>
        /// <param name="fraction">完成度 0 ~ 1。</param>
        /// <param name="detail">进度条上方的说明文字，为 null 时保持不变。</param>
        public void Report(double fraction, string? detail = null)
        {
            RunOnUi(() =>
            {
                if (_isFinished || _viewModel.IsClosing)
                    return;

                _viewModel.TaskProgress = Math.Clamp(fraction, 0, 1);
                if (detail != null)
                    _viewModel.ProgressDetail = detail;
            });
        }

        /// <summary>任务成功：通知原地转为“成功”卡片并开始自动关闭倒计时。</summary>
        public void Complete(
            string? title = null,
            string? message = null,
            IReadOnlyList<NotificationAction>? actions = null,
            int? durationSeconds = null)
        {
            RunOnUi(() => Finish(
                NotificationKind.Success,
                NotificationType.Success,
                title ?? NotificationCenter.GetDefaultTitle(NotificationType.Success),
                message ?? _viewModel.Message,
                actions,
                durationSeconds ?? MessageHelper.WaitTime,
                codeLabel: string.Empty));
        }

        /// <summary>任务失败：通知原地转为“错误”卡片，保留到用户手动关闭。</summary>
        public void Fail(string message, string? title = null, string? codeLabel = null, string? copyPayload = null)
        {
            RunOnUi(() =>
            {
                _viewModel.CopyPayload = copyPayload ?? string.Empty;
                Finish(
                    NotificationKind.Error,
                    NotificationType.Error,
                    title ?? NotificationCenter.GetDefaultTitle(NotificationType.Error),
                    message,
                    actions: null,
                    durationSeconds: 0,
                    codeLabel: codeLabel ?? "ERROR");
            });
        }

        /// <summary>直接收起通知（例如任务被取消且不需要额外提示）。</summary>
        public void Dismiss()
        {
            RunOnUi(() =>
            {
                _isFinished = true;
                _ = _viewModel.Close();
            });
        }

        public void Dispose() => _cts.Dispose();

        private void RequestCancel()
        {
            if (_cts.IsCancellationRequested)
                return;

            _cts.Cancel();
            _viewModel.Actions = Array.Empty<NotificationAction>();
            _viewModel.ProgressDetail = _cancellingText;
        }

        private void Finish(
            NotificationKind kind,
            NotificationType type,
            string title,
            string message,
            IReadOnlyList<NotificationAction>? actions,
            int durationSeconds,
            string codeLabel)
        {
            if (_isFinished || _viewModel.IsClosing)
                return;

            _isFinished = true;

            _viewModel.Title = title;
            _viewModel.Message = message;
            _viewModel.CodeLabel = codeLabel;
            _viewModel.Actions = actions ?? Array.Empty<NotificationAction>();
            _viewModel.TimeText = DateTime.Now.ToString("HH:mm");
            _viewModel.ProgressDetail = string.Empty;
            _viewModel.Type = type;
            _viewModel.Kind = kind;

            // 通知已在历史里，状态变化后重新标为未读，提醒用户任务有了结果
            _center.MarkUnread(_viewModel);

            if (durationSeconds > 0)
                _viewModel.StartAutoClose(durationSeconds);
        }

        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }
    }
}
