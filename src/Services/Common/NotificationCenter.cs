using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 软件自有通知中心：管理 Toast（状态反馈 / 进度）、Sheet 对话框（交互确认）与通知历史。
    /// 主窗口使用全局单例 <see cref="NotificationManager"/>，
    /// 其他独立窗口各自持有一个实例，并通过 NotificationHost 控件呈现。
    /// </summary>
    public partial class NotificationCenter : ObservableObject, IDisposable
    {
        /// <summary>历史记录最多保留的条数。</summary>
        private const int MaxHistoryCount = 100;

        /// <summary>打开历史面板后，经过多久把全部记录标为已读（让用户先看到未读圆点）。</summary>
        private const int MarkReadDelayMilliseconds = 900;

        /// <summary>“清除全部”动画：条目逐条飞出的总错开时长与相邻间隔范围、单条飞出时长（需与历史面板里的动画时长一致）。</summary>
        private const int ClearStaggerTotalMilliseconds = 700;
        private const int ClearStaggerMinMilliseconds = 18;
        private const int ClearStaggerMaxMilliseconds = 60;
        private const int ClearFlyOutMilliseconds = 340;

        private bool _disposed;
        private bool _isClearingHistory;

        /// <summary>右下角 Toast：状态反馈与进度任务。</summary>
        public ObservableCollection<NotificationViewModel> Notifications { get; } = new();

        /// <summary>居中 Sheet：确认 / 三按钮 / 单选等需要用户决策的对话框。</summary>
        public ObservableCollection<NotificationViewModel> Dialogs { get; } = new();

        /// <summary>通知历史（最新在前，不含对话框）。</summary>
        public ObservableCollection<NotificationViewModel> History { get; } = new();

        /// <summary>存在等待用户决策的通知时为 true（用于显示遮罩）。</summary>
        [ObservableProperty]
        private bool isModal;

        /// <summary>true：最新消息在最上方（向下推挤）；false：最旧消息在最上方（尾部追加）。</summary>
        [ObservableProperty]
        private bool newestOnTop = true;

        /// <summary>
        /// 勿扰模式：信息 / 成功只写入历史、不弹出 Toast；警告、错误、进度与对话框不受影响。
        /// </summary>
        [ObservableProperty]
        private bool isDoNotDisturb;

        /// <summary>历史面板是否展开。</summary>
        [ObservableProperty]
        private bool isHistoryOpen;

        /// <summary>未读通知数量（铃铛角标）。</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasUnread))]
        [NotifyPropertyChangedFor(nameof(UnreadText))]
        private int unreadCount;

        public bool HasUnread => UnreadCount > 0;

        public string UnreadText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();

        partial void OnNewestOnTopChanged(bool value)
        {
            RunOnUi(() =>
            {
                int count = Notifications.Count;
                for (int i = 0; i < count - 1; i++)
                    Notifications.Move(count - 1, i);
            });
        }

        partial void OnIsHistoryOpenChanged(bool value)
        {
            if (value)
                _ = MarkAllReadAsync();
        }

        #region 状态反馈类通知

        /// <summary>
        /// 弹出状态反馈通知。警告和错误通知不会自动关闭，需要用户手动处理。
        /// </summary>
        public NotificationViewModel Show(string title, string message, NotificationType type, int durationSeconds = 3)
            => Show(type, message, title, durationSeconds);

        /// <summary>
        /// 弹出状态反馈通知，标题与时长缺省时使用本地化默认值。
        /// </summary>
        /// <param name="codeLabel">警告 / 错误卡片底部显示的诊断码标签。</param>
        /// <param name="copyPayload">点击复制按钮时写入剪贴板的内容，缺省为消息正文。</param>
        /// <param name="actions">卡片底部的操作按钮（撤销 / 查看 等）。</param>
        public NotificationViewModel Show(
            NotificationType type,
            string message,
            string? title = null,
            int? durationSeconds = null,
            string? codeLabel = null,
            string? copyPayload = null,
            IReadOnlyList<NotificationAction>? actions = null)
        {
            int duration = durationSeconds ?? MessageHelper.WaitTime;
            if (type is NotificationType.Warning or NotificationType.Error)
                duration = 0;

            return RunOnUi(() =>
            {
                var vm = CreateViewModel(ToKind(type), type, title ?? GetDefaultTitle(type), message);
                vm.CodeLabel = codeLabel ?? GetDefaultCodeLabel(type);
                vm.CopyPayload = copyPayload ?? string.Empty;
                if (actions != null)
                    vm.Actions = actions;

                RecordHistory(vm);

                // 勿扰：信息 / 成功只进历史，不弹出
                if (IsDoNotDisturb && type is NotificationType.Info or NotificationType.Success)
                    return vm;

                // 先启动倒计时再加入集合，保证卡片创建时进度条状态已就绪，避免进场瞬间再次触发布局
                vm.StartAutoClose(duration);
                AddNotification(vm);
                return vm;
            });
        }

        public NotificationViewModel Info(string message) => Show(NotificationType.Info, message);

        public NotificationViewModel Success(string message) => Show(NotificationType.Success, message);

        public NotificationViewModel Warning(string message) => Show(NotificationType.Warning, message);

        public NotificationViewModel Error(string message) => Show(NotificationType.Error, message);

        #endregion

        #region 进度通知

        /// <summary>
        /// 弹出进度通知。长任务通过返回的句柄汇报进度、响应取消，并在结束时调用
        /// <see cref="NotificationProgressHandle.Complete"/> / <see cref="NotificationProgressHandle.Fail"/>。
        /// </summary>
        /// <param name="title">标题，例如“正在导入样品数据”。</param>
        /// <param name="message">正文，例如文件名。</param>
        /// <param name="detail">进度条上方的说明文字。</param>
        /// <param name="cancellable">是否提供“取消”按钮。</param>
        public NotificationProgressHandle ShowProgress(
            string title,
            string message,
            string? detail = null,
            bool cancellable = true)
        {
            return RunOnUi(() =>
            {
                var vm = CreateViewModel(NotificationKind.Progress, NotificationType.Info, title, message);
                vm.ProgressDetail = detail ?? string.Empty;

                var handle = new NotificationProgressHandle(
                    this,
                    vm,
                    cancellable,
                    L("Cancel", "Cancel"),
                    L("notification_cancelling", "Cancelling..."));

                RecordHistory(vm);
                AddNotification(vm);
                return handle;
            });
        }

        #endregion

        #region 交互确认类通知

        /// <summary>
        /// 双按钮决策（确认 / 取消）。返回 true 表示用户点击了确认。
        /// </summary>
        public Task<bool> ShowDialogAsync(string title, string message, string confirmText, string cancelText)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            RunOnUi(() =>
            {
                var vm = CreateViewModel(NotificationKind.Confirm, NotificationType.Info, title, message);
                vm.IsInteractive = true;
                vm.ConfirmText = confirmText;
                vm.CancelText = cancelText;
                vm.DialogResultAction = result => tcs.TrySetResult(result);
                AddNotification(vm);
            });

            return tcs.Task;
        }

        /// <summary>
        /// 三按钮对话框（保存 / 不保存 / 取消）。
        /// </summary>
        /// <returns>0 = 主动作，1 = 第三动作，2 = 取消</returns>
        public Task<int> ShowThreeButtonDialogAsync(
            string title,
            string message,
            string confirmText,
            string thirdButtonText,
            string cancelText,
            bool isThirdButtonSubtle = false)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            RunOnUi(() =>
            {
                var vm = CreateViewModel(NotificationKind.ThreeButton, NotificationType.Info, title, message);
                vm.IsInteractive = true;
                vm.ConfirmText = confirmText;
                vm.ThirdButtonText = thirdButtonText;
                vm.CancelText = cancelText;
                vm.IsThirdButtonSubtle = isThirdButtonSubtle;
                vm.ThreeButtonDialogResultAction = result => tcs.TrySetResult(result);
                AddNotification(vm);
            });

            return tcs.Task;
        }

        /// <summary>
        /// 单选对话框：从若干选项中选择一项。取消时返回 null。
        /// </summary>
        public Task<string?> ShowChoiceAsync(
            string title,
            string message,
            IReadOnlyList<string> options,
            string confirmText,
            string cancelText,
            string? selectedOption = null)
        {
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            RunOnUi(() =>
            {
                var vm = CreateViewModel(NotificationKind.Choice, NotificationType.Info, title, message);
                vm.IsInteractive = true;
                vm.ConfirmText = confirmText;
                vm.CancelText = cancelText;
                vm.ChoiceOptions = options;
                vm.SelectedChoice = selectedOption ?? options.FirstOrDefault();
                vm.ChoiceResultAction = selected => tcs.TrySetResult(selected);
                AddNotification(vm);
            });

            return tcs.Task;
        }

        #endregion

        #region 历史与管理

        /// <summary>展开 / 收起历史面板。</summary>
        [RelayCommand]
        private void ToggleHistory() => IsHistoryOpen = !IsHistoryOpen;

        /// <summary>收起历史面板。</summary>
        [RelayCommand]
        private void CloseHistory() => IsHistoryOpen = false;

        /// <summary>
        /// 清空历史记录（不影响正在显示的 Toast）。
        /// 条目按从上到下的顺序依次向右飞出，全部飞完后再从列表中移除；仍在进行中的任务会保留。
        /// </summary>
        [RelayCommand]
        private async Task ClearHistory()
        {
            if (_isClearingHistory || History.Count == 0)
                return;

            _isClearingHistory = true;
            try
            {
                var targets = History.Where(n => n.Kind != NotificationKind.Progress).ToList();

                if (targets.Count > 0)
                {
                    // 总错开时长约 0.7 秒：条目多时缩短相邻间隔，避免等太久
                    int step = Math.Clamp(ClearStaggerTotalMilliseconds / targets.Count, ClearStaggerMinMilliseconds, ClearStaggerMaxMilliseconds);

                    foreach (var vm in targets)
                    {
                        vm.IsClearing = true;
                        await Task.Delay(step);
                    }

                    // 等最后一条也飞出视野
                    await Task.Delay(ClearFlyOutMilliseconds);
                }

                if (_disposed)
                    return;

                foreach (var vm in targets)
                    History.Remove(vm);

                RefreshUnread();
            }
            finally
            {
                _isClearingHistory = false;
            }
        }

        /// <summary>
        /// 清空所有通知；等待决策的对话框按“取消”处理。
        /// </summary>
        public void Clear()
        {
            RunOnUi(() =>
            {
                foreach (var vm in Notifications.Concat(Dialogs).ToList())
                {
                    vm.Abort();
                    _ = vm.Close();
                }
            });
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            RunOnUi(() =>
            {
                foreach (var vm in Notifications.Concat(Dialogs).ToList())
                {
                    vm.PropertyChanged -= OnNotificationPropertyChanged;
                    vm.Abort();
                }

                Notifications.Clear();
                Dialogs.Clear();
                History.Clear();
                IsModal = false;
                UnreadCount = 0;
            });

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 通知状态发生变化（例如进度任务完成）后，重新标为未读，提醒用户去看结果。
        /// 历史面板正展开时用户已经能看到，无需标记。
        /// </summary>
        internal void MarkUnread(NotificationViewModel vm)
        {
            if (IsHistoryOpen)
                return;

            vm.IsUnread = true;
            RefreshUnread();
        }

        #endregion

        #region 内部实现

        private NotificationViewModel CreateViewModel(NotificationKind kind, NotificationType type, string title, string message)
            => new(title, message, type, kind, RemoveNotification);

        private void AddNotification(NotificationViewModel vm)
        {
            if (_disposed)
            {
                vm.Abort();
                return;
            }

            vm.PropertyChanged += OnNotificationPropertyChanged;

            if (vm.IsInteractive)
                Dialogs.Add(vm);
            else if (NewestOnTop)
                Notifications.Insert(0, vm);
            else
                Notifications.Add(vm);

            UpdateModalStatus();
        }

        private void RemoveNotification(NotificationViewModel vm)
        {
            RunOnUi(() =>
            {
                vm.PropertyChanged -= OnNotificationPropertyChanged;
                Notifications.Remove(vm);
                Dialogs.Remove(vm);
                UpdateModalStatus();
            });
        }

        private void RecordHistory(NotificationViewModel vm)
        {
            if (_disposed)
                return;

            vm.IsUnread = !IsHistoryOpen;
            History.Insert(0, vm);

            while (History.Count > MaxHistoryCount)
                History.RemoveAt(History.Count - 1);

            RefreshUnread();
        }

        private void RefreshUnread() => UnreadCount = History.Count(n => n.IsUnread);

        private async Task MarkAllReadAsync()
        {
            await Task.Delay(MarkReadDelayMilliseconds);

            if (!IsHistoryOpen || _disposed)
                return;

            foreach (var vm in History)
                vm.IsUnread = false;

            RefreshUnread();
        }

        private void OnNotificationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NotificationViewModel.IsClosing))
                UpdateModalStatus();
        }

        private void UpdateModalStatus()
        {
            IsModal = Dialogs.Any(n => !n.IsClosing);
        }

        private static NotificationKind ToKind(NotificationType type) => type switch
        {
            NotificationType.Success => NotificationKind.Success,
            NotificationType.Warning => NotificationKind.Warning,
            NotificationType.Error => NotificationKind.Error,
            _ => NotificationKind.Info
        };

        private static string L(string key, string fallback) =>
            LanguageService.GetString(key, fallback);

        /// <summary>
        /// 各类状态通知的本地化默认标题。
        /// </summary>
        public static string GetDefaultTitle(NotificationType type) => type switch
        {
            NotificationType.Success => L("notification_success", "Success"),
            NotificationType.Warning => L("notification_warning", "Warning"),
            NotificationType.Error => L("error", "Error"),
            _ => L("information", "Information")
        };

        private static string GetDefaultCodeLabel(NotificationType type) => type switch
        {
            NotificationType.Warning => "WARNING",
            NotificationType.Error => "ERROR",
            _ => string.Empty
        };

        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }

        private static T RunOnUi<T>(Func<T> func)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                return func();

            return dispatcher.Invoke(func);
        }

        #endregion
    }
}
