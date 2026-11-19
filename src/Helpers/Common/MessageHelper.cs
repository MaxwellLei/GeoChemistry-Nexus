using System.Threading.Tasks;
using System.Windows;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 主窗口通知的便捷入口，全部委托给全局 <see cref="NotificationManager"/>。
    /// 独立窗口请使用各自的 <see cref="NotificationCenter"/>。
    /// </summary>
    public class MessageHelper
    {
        /// <summary>设置项 auto_off_time（下拉框索引）对应的自动关闭秒数：5 / 4 / 3 / 2。</summary>
        public static int GetWaitSecondsByIndex(int index) => index switch
        {
            1 => 4,
            2 => 3,
            3 => 2,
            _ => 5
        };

        /// <summary>自动关闭秒数（2 ~ 5）对应的设置项 auto_off_time 索引，是 <see cref="GetWaitSecondsByIndex"/> 的反向映射。</summary>
        public static int GetIndexBySeconds(int seconds) => seconds switch
        {
            4 => 1,
            3 => 2,
            2 => 3,
            _ => 0
        };

        //消息自动关闭通知时间（对 Info / Success 通知生效，Warning / Error 需手动关闭）
        public static int WaitTime { get; set; } = LoadWaitTime();

        // 启动时从配置恢复用户在设置页选择的自动关闭时间
        private static int LoadWaitTime()
        {
            try
            {
                if (int.TryParse(ConfigHelper.GetConfig(ConfigKeys.AutoOffTime), out int index))
                    return GetWaitSecondsByIndex(index);
            }
            catch
            {
                // 配置读取失败时使用默认值
            }

            return 5;
        }

        private static string L(string key, string fallback) =>
            LanguageService.Instance[key] ?? fallback;

        //普通消息通知
        public static void Info(string message) => NotificationManager.Instance.Info(message);

        //成功消息通知
        public static void Success(string message) => NotificationManager.Instance.Success(message);

        //警告消息通知（需用户手动关闭）
        public static void Warning(string message) => NotificationManager.Instance.Warning(message);

        //错误消息通知（需用户手动关闭）
        public static void Error(string message) => NotificationManager.Instance.Error(message);

        //成功消息通知（带操作按钮，例如“撤销”“查看”）
        public static void Success(string message, params NotificationAction[] actions) =>
            NotificationManager.Instance.Show(NotificationType.Success, message, actions: actions);

        /// <summary>
        /// 文件或目录导出成功。成功通知上带「打开文件位置」。
        /// <paramref name="owner"/> 是带独立通知中心的窗口时，提示显示在该窗口上。
        /// </summary>
        public static void ExportSuccess(string message, string? path, Window? owner = null) =>
            ExportResultNotifier.Success(message, path, owner);

        /// <summary>
        /// 文件或目录导出成功，并指定通知中心（独立窗口已持有 <see cref="NotificationCenter"/> 时使用）。
        /// </summary>
        public static void ExportSuccess(string message, string? path, NotificationCenter? center) =>
            ExportResultNotifier.Success(message, path, center);

        //进度通知：返回句柄，长任务通过它汇报进度 / 响应取消 / 收尾
        public static NotificationProgressHandle ShowProgress(
            string title,
            string message,
            string? detail = null,
            bool cancellable = true) =>
            NotificationManager.Instance.ShowProgress(title, message, detail, cancellable);

        public static Task<bool> ShowAsyncDialog(string message, string cancelText, string confirmText)
        {
            return NotificationManager.Instance.ShowDialogAsync(
                L("Confirm", "Confirm"),
                message,
                confirmText,
                cancelText);
        }
    }
}
