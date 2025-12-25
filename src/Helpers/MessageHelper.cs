using System.Threading.Tasks;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Helpers
{
    public class MessageHelper
    {
        //消息自动关闭通知时间
        public static int waitTime = 5;

        //普通消息通知
        public static void Info(string message)
        {
            NotificationManager.Instance.Show("Info", message, NotificationType.Info, waitTime);
        }

        //成功消息通知
        public static void Success(string message)
        {
            NotificationManager.Instance.Show("Success", message, NotificationType.Success, waitTime);
        }

        //警告消息通知
        public static void Warning(string message)
        {
            NotificationManager.Instance.Show("Warning", message, NotificationType.Warning, waitTime);
        }

        //错误消息通知
        public static void Error(string message)
        {
            NotificationManager.Instance.Show("Error", message, NotificationType.Error, waitTime);
        }

        public static Task<bool> ShowAsyncDialog(string message, string cancelText, string confirmText)
        {
            return NotificationManager.Instance.ShowDialogAsync("Confirm", message, confirmText, cancelText);
        }
    }
}
