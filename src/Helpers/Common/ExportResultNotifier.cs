using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System.Windows;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 文件或目录导出成功后的统一提示。
    /// 在现有成功通知上增加「打开文件位置」，并显示在发起导出的窗口里。
    /// 新的导出完成后调用 <see cref="MessageHelper.ExportSuccess"/> 即可，不必再各自拼按钮。
    /// </summary>
    public static class ExportResultNotifier
    {
        public static void Success(string message, string? path, Window? owner = null) =>
            Show(message, path, ResolveCenter(owner));

        public static void Success(string message, string? path, NotificationCenter? center) =>
            Show(message, path, center ?? ResolveCenter(null));

        private static void Show(string message, string? path, NotificationCenter center)
        {
            string? target = string.IsNullOrWhiteSpace(path) ? null : path;
            NotificationAction[]? actions = null;
            if (target != null)
            {
                string captured = target;
                actions = new[]
                {
                    new NotificationAction(
                        LanguageService.GetString("open_file_location", "Open file location"),
                        () =>
                        {
                            if (!FileHelper.RevealInExplorer(captured))
                            {
                                center.Error(LanguageService.GetString(
                                    "open_file_location_failed",
                                    "Could not open the file location."));
                            }
                        },
                        isPrimary: true)
                };
            }

            center.Show(NotificationType.Success, message, actions: actions);
        }

        private static NotificationCenter ResolveCenter(Window? owner)
        {
            if (TryGetCenter(owner, out var fromOwner))
                return fromOwner;

            var app = Application.Current;
            if (app != null)
            {
                foreach (Window window in app.Windows)
                {
                    if (window.IsActive && TryGetCenter(window, out var fromActive))
                        return fromActive;
                }
            }

            return NotificationManager.Instance;
        }

        /// <summary>
        /// 沿 Owner 链查找通知中心。多语言编辑窗这类没有自己通知区的对话框，会显示在它的父窗口上。
        /// </summary>
        private static bool TryGetCenter(Window? window, out NotificationCenter center)
        {
            int depth = 0;
            for (Window? current = window; current != null && depth < 8; current = current.Owner, depth++)
            {
                if (current is INotificationCenterHost host)
                {
                    center = host.Notifications;
                    return true;
                }
            }

            center = NotificationManager.Instance;
            return false;
        }
    }
}
