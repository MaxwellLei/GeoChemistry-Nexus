using System;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 官方内容（图解 / 地质温压计 / EPMA）统一的安装状态与解析规则。
    /// 三个模块的目录同步、卡片/列表展示、批量更新都以这里的状态为准。
    /// </summary>
    public static class ContentInstallStatus
    {
        /// <summary>服务器有、本地尚未下载（只有目录占位）</summary>
        public const string NotInstalled = "NOT_INSTALLED";

        /// <summary>本地已安装且与服务器一致</summary>
        public const string UpToDate = "UP_TO_DATE";

        /// <summary>本地已安装，服务器有当前软件可处理的新版本</summary>
        public const string Outdated = "OUTDATED";

        /// <summary>
        /// 服务器版本高于当前软件支持的格式：无论本地是否已安装，都不能下载/更新；
        /// 本地没有安装时也不能打开，本地已有旧版时仍可打开旧版。
        /// </summary>
        public const string RequiresAppUpgrade = "REQUIRES_APP_UPGRADE";

        public static bool IsRequiresAppUpgrade(string? status)
            => string.Equals(status, RequiresAppUpgrade, StringComparison.Ordinal);

        /// <summary>
        /// 按 x.y.z 版本号判断（图解 / 温压计）：服务器版本的 x.y 高于软件格式基线即需要升级软件。
        /// </summary>
        public static string Resolve(
            bool isInstalled,
            string? localHash,
            string? serverHash,
            string? localVersion,
            string? serverVersion,
            string appFormatVersion)
        {
            bool requiresAppUpgrade = !string.IsNullOrWhiteSpace(serverVersion)
                && ContentVersionHelper.RequiresAppUpgrade(serverVersion, appFormatVersion);
            return Resolve(isInstalled, requiresAppUpgrade, localHash, serverHash, localVersion, serverVersion);
        }

        /// <summary>
        /// 调用方自行判断是否需要升级软件（EPMA 用包版本 x.y 与 requires 判断，见 EpmaScriptPackage.RequiresAppUpgrade）。
        /// </summary>
        public static string Resolve(
            bool isInstalled,
            bool requiresAppUpgrade,
            string? localHash,
            string? serverHash,
            string? localVersion,
            string? serverVersion)
        {
            if (requiresAppUpgrade)
                return RequiresAppUpgrade;

            if (!isInstalled)
                return NotInstalled;

            // 服务器提供了内容哈希时以哈希为准（本地版本号保留内容自带的值，不再被目录覆盖，
            // 目录版本与内容自带版本不一致时不能据此判定有更新）
            if (!string.IsNullOrEmpty(serverHash))
                return string.Equals(localHash, serverHash, StringComparison.OrdinalIgnoreCase) ? UpToDate : Outdated;

            // 没有哈希时才退回版本号比较
            bool versionUpdate = !string.IsNullOrWhiteSpace(serverVersion)
                && (ContentVersionHelper.HasContentUpdate(localVersion, serverVersion)
                    || ContentVersionHelper.Compare(serverVersion, localVersion) > 0);

            return versionUpdate ? Outdated : UpToDate;
        }
    }
}
