using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 窗口关闭后不再回调自动消失的本地通知。
    /// </summary>
    internal static class LocalNotificationDelay
    {
        public static void Schedule(Dispatcher dispatcher, int durationSeconds, CancellationToken token, Action close)
        {
            if (durationSeconds <= 0 || dispatcher == null || close == null)
                return;

            _ = Task.Delay(TimeSpan.FromSeconds(durationSeconds), token).ContinueWith(task =>
            {
                if (task.IsCanceled || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                    return;

                dispatcher.BeginInvoke(close);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }
    }
}
