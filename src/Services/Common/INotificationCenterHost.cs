namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 拥有独立通知中心的窗口。文件导出成功等提示会显示在这个窗口上，而不是主窗口。
    /// </summary>
    public interface INotificationCenterHost
    {
        NotificationCenter Notifications { get; }
    }
}
