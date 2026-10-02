using System.Threading.Tasks;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Helpers
{
    public class MessageHelper
    {
        //消息自动关闭通知时间
        public static int WaitTime { get; set; } = 5;

        private static string L(string key, string fallback) =>
            LanguageService.Instance[key] ?? fallback;

        //普通消息通知
        public static void Info(string message)
        {
            NotificationManager.Instance.Show(
                L("information", "Information"),
                message,
                NotificationType.Info,
                WaitTime);
        }

        //成功消息通知
        public static void Success(string message)
        {
            NotificationManager.Instance.Show(
                L("notification_success", "Success"),
                message,
                NotificationType.Success,
                WaitTime);
        }

        //警告消息通知
        public static void Warning(string message)
        {
            NotificationManager.Instance.Show(
                L("notification_warning", "Warning"),
                message,
                NotificationType.Warning,
                WaitTime);
        }

        //错误消息通知
        public static void Error(string message)
        {
            NotificationManager.Instance.Show(
                L("error", "Error"),
                message,
                NotificationType.Error,
                WaitTime);
        }

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
