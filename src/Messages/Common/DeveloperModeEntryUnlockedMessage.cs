namespace GeoChemistryNexus.Messages
{
    /// <summary>
    /// 在“关于”页连续点击 Gn 块后，通知设置页显示“开发者模式”入口。
    /// </summary>
    public class DeveloperModeEntryUnlockedMessage
    {
        /// <summary>
        /// 本次运行期间是否已解锁开发者模式入口。
        /// </summary>
        public static bool IsUnlocked { get; set; }
    }
}
