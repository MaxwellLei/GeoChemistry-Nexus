using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 状态类通知的语义类型。
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 通知卡片的完整形态：4 种状态反馈 + 进度任务 + 3 种交互确认。
    /// 状态反馈与进度以右下角 Toast 呈现，交互确认以居中 Sheet 呈现。
    /// </summary>
    public enum NotificationKind
    {
        Info,
        Success,
        Warning,
        Error,
        /// <summary>双按钮决策（确认 / 取消）</summary>
        Confirm,
        /// <summary>三按钮对话框（主动作 / 第三动作 / 取消）</summary>
        ThreeButton,
        /// <summary>单选列表 + 确认 / 取消</summary>
        Choice,
        /// <summary>长任务进度（可更新、可取消，完成后原地转为成功 / 错误）</summary>
        Progress
    }

    /// <summary>
    /// 单条通知卡片的视图模型。
    /// 状态由 <see cref="Kind"/> 决定，卡片外观完全由 XAML 样式驱动。
    /// </summary>
    public partial class NotificationViewModel : ObservableObject
    {
        /// <summary>退出动画时长，需与 NotificationItem.xaml 中 SlideOut 保持一致。</summary>
        private const int ExitAnimationMilliseconds = 500;

        /// <summary>Sheet 对话框退出动画时长，需与 NotificationSheet.xaml 中 SheetOut 保持一致。</summary>
        private const int SheetExitAnimationMilliseconds = 230;

        /// <summary>鼠标移开后至少保留的时间，避免悬停到临界点后立刻消失。</summary>
        private const double MinResumeRemainingMilliseconds = 1500;

        private readonly Action<NotificationViewModel> _closeAction;
        private DispatcherTimer? _timer;
        private double _durationMilliseconds;
        private double _elapsedMilliseconds;
        private long _lastTickMilliseconds;
        private bool _isPaused;
        private bool _isResolved;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string message = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowCopyBar))]
        private NotificationType type;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowCopyBar))]
        [NotifyPropertyChangedFor(nameof(ShowConfirm))]
        [NotifyPropertyChangedFor(nameof(ShowThreeButtons))]
        [NotifyPropertyChangedFor(nameof(ShowChoice))]
        [NotifyPropertyChangedFor(nameof(ShowProgressBar))]
        [NotifyPropertyChangedFor(nameof(ShowCloseButton))]
        [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
        private NotificationKind kind;

        /// <summary>标题旁的小标签，为空时不显示。</summary>
        [ObservableProperty]
        private string tag = string.Empty;

        /// <summary>右上角时间戳（HH:mm）。</summary>
        [ObservableProperty]
        private string timeText = DateTime.Now.ToString("HH:mm");

        /// <summary>警告 / 错误卡片底部的诊断码标签。</summary>
        [ObservableProperty]
        private string codeLabel = string.Empty;

        /// <summary>点击“复制”时写入剪贴板的内容，为空则复制 <see cref="Message"/>。</summary>
        [ObservableProperty]
        private string copyPayload = string.Empty;

        [ObservableProperty]
        private bool isCopied;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
        [NotifyPropertyChangedFor(nameof(ShowCloseButton))]
        private bool isInteractive;

        [ObservableProperty]
        private string confirmText = string.Empty;

        [ObservableProperty]
        private string cancelText = string.Empty;

        [ObservableProperty]
        private string thirdButtonText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsThirdButtonDestructive))]
        private bool isThirdButtonSubtle;

        [ObservableProperty]
        private IReadOnlyList<string> choiceOptions = Array.Empty<string>();

        [ObservableProperty]
        private string? selectedChoice;

        /// <summary>自动关闭倒计时进度，1 → 0。</summary>
        [ObservableProperty]
        private double progress = 1;

        [ObservableProperty]
        private bool hasTimer;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
        private bool isClosing;

        /// <summary>
        /// 尚未在通知中心里查看过（历史面板里显示未读圆点，铃铛上累计未读数）。
        /// </summary>
        [ObservableProperty]
        private bool isUnread;

        /// <summary>
        /// 正在被“清除全部”清掉（历史面板里的条目据此播放向右飞出的动画）。
        /// </summary>
        [ObservableProperty]
        private bool isClearing;

        /// <summary>长任务完成度，0 → 1（仅 <see cref="NotificationKind.Progress"/> 使用）。</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TaskProgressText))]
        private double taskProgress;

        /// <summary>进度条上方的说明文字，例如“已处理 32 / 128 条”。</summary>
        [ObservableProperty]
        private string progressDetail = string.Empty;

        /// <summary>卡片底部的操作按钮（撤销 / 查看 / 取消 等）。</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowActions))]
        private IReadOnlyList<NotificationAction> actions = Array.Empty<NotificationAction>();

        /// <summary>双按钮决策结果：true = 确认，false = 取消。</summary>
        public Action<bool>? DialogResultAction { get; set; }

        /// <summary>三按钮对话框结果：0 = 主动作，1 = 第三动作，2 = 取消。</summary>
        public Action<int>? ThreeButtonDialogResultAction { get; set; }

        /// <summary>单选对话框结果：选中项；取消时为 null。</summary>
        public Action<string?>? ChoiceResultAction { get; set; }

        public bool ShowCopyBar => Kind is NotificationKind.Warning or NotificationKind.Error;
        public bool ShowConfirm => Kind == NotificationKind.Confirm;
        public bool ShowThreeButtons => Kind == NotificationKind.ThreeButton;
        public bool ShowChoice => Kind == NotificationKind.Choice;
        public bool ShowProgressBar => Kind == NotificationKind.Progress;
        public bool ShowActions => Actions.Count > 0;

        /// <summary>交互确认类需用户明确决策、进度任务需通过“取消”结束，二者都不提供关闭按钮。</summary>
        public bool ShowCloseButton => !IsInteractive && Kind != NotificationKind.Progress;
        public bool IsThirdButtonDestructive => !IsThirdButtonSubtle;
        public string TaskProgressText => $"{Math.Round(TaskProgress * 100):0}%";

        public NotificationViewModel(
            string title,
            string message,
            NotificationType type,
            NotificationKind kind,
            Action<NotificationViewModel> closeAction)
        {
            Title = title;
            Message = message;
            Type = type;
            Kind = kind;
            _closeAction = closeAction;
        }

        #region 自动关闭倒计时

        /// <summary>
        /// 开始自动关闭倒计时（鼠标悬停时暂停）。
        /// </summary>
        public void StartAutoClose(int seconds)
        {
            if (seconds <= 0 || IsClosing)
                return;

            StopTimer();

            _durationMilliseconds = seconds * 1000d;
            _elapsedMilliseconds = 0;
            _lastTickMilliseconds = Environment.TickCount64;
            _isPaused = false;
            Progress = 1;
            HasTimer = true;

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        /// <summary>鼠标进入卡片：暂停倒计时。</summary>
        public void PauseAutoClose() => _isPaused = true;

        /// <summary>鼠标离开卡片：继续倒计时。</summary>
        public void ResumeAutoClose()
        {
            if (_timer == null)
                return;

            double latestElapsed = Math.Max(0, _durationMilliseconds - MinResumeRemainingMilliseconds);
            if (_durationMilliseconds > MinResumeRemainingMilliseconds && _elapsedMilliseconds > latestElapsed)
                _elapsedMilliseconds = latestElapsed;

            _lastTickMilliseconds = Environment.TickCount64;
            _isPaused = false;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            long now = Environment.TickCount64;
            if (!_isPaused)
                _elapsedMilliseconds += now - _lastTickMilliseconds;
            _lastTickMilliseconds = now;

            Progress = Math.Clamp(1 - _elapsedMilliseconds / _durationMilliseconds, 0, 1);

            if (_elapsedMilliseconds >= _durationMilliseconds)
            {
                StopTimer();
                _ = Close();
            }
        }

        private void StopTimer()
        {
            if (_timer == null)
                return;

            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
            HasTimer = false;
        }

        #endregion

        #region 命令

        [RelayCommand]
        private async Task Confirm()
        {
            // 选中项在关闭动画开始前取值，避免动画期间被改动
            string? selectedChoice = SelectedChoice;

            Action resolve = Kind switch
            {
                NotificationKind.ThreeButton => () => Resolve(() => ThreeButtonDialogResultAction?.Invoke(0)),
                NotificationKind.Choice => () => Resolve(() => ChoiceResultAction?.Invoke(selectedChoice)),
                _ => () => Resolve(() => DialogResultAction?.Invoke(true))
            };

            await CloseThenResolveAsync(resolve);
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await CloseThenResolveAsync(ResolveAsCancelled);
        }

        [RelayCommand]
        private async Task ThirdButton()
        {
            await CloseThenResolveAsync(() => Resolve(() => ThreeButtonDialogResultAction?.Invoke(1)));
        }

        /// <summary>
        /// 先播放退出动画并移除对话框，再把结果交还给调用方。
        /// 若在点击瞬间就返回结果，调用方后续的界面切换会与退出动画抢占 UI 线程，导致动画卡顿。
        /// </summary>
        private async Task CloseThenResolveAsync(Action resolve)
        {
            if (IsClosing)
                return;

            IsClosing = true;
            StopTimer();

            await Task.Delay(SheetExitAnimationMilliseconds);
            _closeAction.Invoke(this);

            // 让对话框移除后的一帧先渲染出来，再执行调用方的后续逻辑
            await Dispatcher.Yield(DispatcherPriority.Render);

            resolve();
        }

        [RelayCommand]
        private async Task CopyAsync()
        {
            string text = string.IsNullOrEmpty(CopyPayload) ? Message : CopyPayload;
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception)
            {
                // 剪贴板被占用时忽略
                return;
            }

            IsCopied = true;
            await Task.Delay(1800);
            IsCopied = false;
        }

        /// <summary>点击卡片上的操作按钮：先执行回调，再按需关闭。</summary>
        [RelayCommand]
        private async Task InvokeAction(NotificationAction? action)
        {
            if (action == null)
                return;

            action.Invoke();

            if (action.CloseOnInvoke)
                await Close();
        }

        private bool CanDismiss() => ShowCloseButton && !IsClosing;

        [RelayCommand(CanExecute = nameof(CanDismiss))]
        public async Task Close()
        {
            if (IsClosing)
                return;

            IsClosing = true;
            StopTimer();
            await Task.Delay(ExitAnimationMilliseconds);
            _closeAction.Invoke(this);
        }

        #endregion

        #region 结果回调

        /// <summary>
        /// 通知被强制移除（清空 / 窗口关闭）时，让等待中的对话框按“取消”返回，避免调用方永久挂起。
        /// </summary>
        public void ResolveAsCancelled()
        {
            switch (Kind)
            {
                case NotificationKind.ThreeButton:
                    Resolve(() => ThreeButtonDialogResultAction?.Invoke(2));
                    break;
                case NotificationKind.Choice:
                    Resolve(() => ChoiceResultAction?.Invoke(null));
                    break;
                case NotificationKind.Confirm:
                    Resolve(() => DialogResultAction?.Invoke(false));
                    break;
            }
        }

        /// <summary>
        /// 释放资源：停止倒计时并取消未决的对话框。
        /// </summary>
        public void Abort()
        {
            StopTimer();
            ResolveAsCancelled();
        }

        private void Resolve(Action callback)
        {
            if (_isResolved)
                return;

            _isResolved = true;
            callback();
        }

        #endregion
    }
}
