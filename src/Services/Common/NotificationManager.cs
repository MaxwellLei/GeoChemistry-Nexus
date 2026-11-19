namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 主窗口使用的全局通知中心（单例）。
    /// 独立窗口请直接创建各自的 <see cref="NotificationCenter"/> 实例。
    /// </summary>
    public sealed class NotificationManager : NotificationCenter
    {
        private static NotificationManager? _instance;

        public static NotificationManager Instance => _instance ??= new NotificationManager();

        private NotificationManager()
        {
        }
    }
}
