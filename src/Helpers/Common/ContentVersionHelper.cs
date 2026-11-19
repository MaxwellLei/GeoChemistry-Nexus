using System;
using System.Linq;
using System.Reflection;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 统一的内容版本号工具：存储格式为 string "x.y.z"。
    /// x.y 为格式兼容基线，均由 csproj 属性经 AssemblyMetadata 写入程序集：
    /// 图解来自 DiagramFormatVersion，温压计来自 GeothermometerFormatVersion，EPMA 来自 EpmaFormatVersion；
    /// z 为同格式下的内容修订号。
    /// 软件自身版本（AssemblyVersion / FileVersion）与内容格式版本互相独立。
    /// </summary>
    public static class ContentVersionHelper
    {
        public const string DefaultVersion = "1.0.0";

        private const string DiagramFormatMetadataKey = "DiagramFormatVersion";
        private const string GeothermometerFormatMetadataKey = "GeothermometerFormatVersion";
        private const string EpmaFormatMetadataKey = "EpmaFormatVersion";

        /// <summary>图解格式基线版本（来自 csproj DiagramFormatVersion）。</summary>
        public static string GetDiagramFormatVersion() => Normalize(ReadMetadataVersion(DiagramFormatMetadataKey));

        /// <summary>取版本号的格式基线 x.y。</summary>
        public static string FormatBaseline(string? version)
        {
            var parts = Normalize(version).Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : "1.0";
        }

        /// <summary>地质温压计格式基线版本（来自 csproj GeothermometerFormatVersion）。</summary>
        public static string GetGeothermometerFormatVersion() => Normalize(ReadMetadataVersion(GeothermometerFormatMetadataKey));

        /// <summary>EPMA 模块版本（来自 csproj EpmaFormatVersion，从 1.0 起）。</summary>
        public static string GetEpmaFormatVersion() => Normalize(ReadMetadataVersion(EpmaFormatMetadataKey));

        /// <summary>将任意输入规范化为 x.y.z 字符串。</summary>
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return DefaultVersion;

            input = input.Trim();

            if (TryParseVersion(input, out Version parsed))
                return ToString(parsed);

            if (float.TryParse(input, out float floatValue))
                return FloatToVersionString(floatValue);

            return DefaultVersion;
        }

        /// <summary>完整比较 x.y.z。</summary>
        public static int Compare(string? left, string? right)
        {
            if (!TryParseVersion(Normalize(left), out Version v1))
                v1 = new Version(0, 0, 0);
            if (!TryParseVersion(Normalize(right), out Version v2))
                v2 = new Version(0, 0, 0);

            return v1.CompareTo(v2);
        }

        /// <summary>仅比较 x.y（忽略 z）。</summary>
        public static int CompareFormat(string? contentVersion, string? appFormatVersion)
        {
            if (!TryParseVersion(Normalize(contentVersion), out Version content))
                content = new Version(0, 0, 0);
            if (!TryParseVersion(Normalize(appFormatVersion), out Version app))
                app = new Version(0, 0, 0);

            int majorCompare = content.Major.CompareTo(app.Major);
            if (majorCompare != 0)
                return majorCompare;

            return content.Minor.CompareTo(app.Minor);
        }

        /// <summary>图解内容 x.y 是否不高于当前程序支持的格式。</summary>
        public static bool IsDiagramFormatCompatible(string? contentVersion)
            => CompareFormat(contentVersion, GetDiagramFormatVersion()) <= 0;

        /// <summary>温压计内容 x.y 是否不高于当前程序支持的格式。</summary>
        public static bool IsGeothermometerFormatCompatible(string? contentVersion)
            => CompareFormat(contentVersion, GetGeothermometerFormatVersion()) <= 0;

        /// <summary>同 x.y 下远程 z 是否高于本地（内容有更新）。</summary>
        public static bool HasContentUpdate(string? localVersion, string? remoteVersion)
        {
            localVersion = Normalize(localVersion);
            remoteVersion = Normalize(remoteVersion);

            if (CompareFormat(localVersion, remoteVersion) != 0)
                return CompareFormat(remoteVersion, localVersion) > 0;

            if (!TryParseVersion(localVersion, out Version local))
                return false;
            if (!TryParseVersion(remoteVersion, out Version remote))
                return false;

            return remote.Build > local.Build;
        }

        /// <summary>是否需要升级软件才能打开（内容 x.y 高于程序格式基线）。</summary>
        public static bool RequiresAppUpgrade(string? contentVersion, string appFormatVersion)
            => CompareFormat(contentVersion, appFormatVersion) > 0;

        public static bool TryGetPatch(string? version, out int patch)
        {
            patch = 0;
            if (!TryParseVersion(Normalize(version), out Version parsed))
                return false;

            patch = parsed.Build;
            return true;
        }

        public static string CombineWithPatch(string formatVersion, int patch)
        {
            if (!TryParseVersion(Normalize(formatVersion), out Version format))
                format = new Version(1, 0, 0);

            patch = Math.Max(0, patch);
            return ToString(new Version(format.Major, format.Minor, patch));
        }

        public static string WithAppDiagramFormat(int patch)
            => CombineWithPatch(GetDiagramFormatVersion(), patch);

        /// <summary>EPMA 模板版本：x.y 跟随 EPMA 模块版本，z 为模板修订号。</summary>
        public static string WithAppEpmaFormat(int patch)
            => CombineWithPatch(GetEpmaFormatVersion(), patch);

        public static string WithAppGeothermometerFormat(int patch)
            => CombineWithPatch(GetGeothermometerFormatVersion(), patch);

        /// <summary>
        /// 保存后版本的展示。已有模板版本与即将写入的版本不同时显示「原版本 -> 保存后版本」，否则只显示保存后版本。
        /// </summary>
        public static string FormatSavedVersion(string? originalVersion, string savedVersion)
        {
            if (string.IsNullOrWhiteSpace(originalVersion))
                return savedVersion;

            string original = Normalize(originalVersion);
            if (Compare(original, savedVersion) == 0)
                return savedVersion;

            return $"{original} -> {savedVersion}";
        }

        public static string IncrementPatch(string? version)
        {
            if (!TryParseVersion(Normalize(version), out Version parsed))
                return DefaultVersion;

            return ToString(new Version(parsed.Major, parsed.Minor, parsed.Build + 1));
        }

        /// <summary>保存时将内容版本对齐到程序格式：x.y 升级则重置 z，否则 z+1。</summary>
        public static string ResolveVersionOnSave(string? currentVersion, string appFormatVersion)
        {
            currentVersion = Normalize(currentVersion);
            appFormatVersion = Normalize(appFormatVersion);

            if (CompareFormat(appFormatVersion, currentVersion) > 0)
                return appFormatVersion;

            if (CompareFormat(currentVersion, appFormatVersion) > 0)
                return currentVersion;

            return IncrementPatch(currentVersion);
        }

        private static string ReadMetadataVersion(string key)
        {
            try
            {
                var metadata = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(a => a.Key == key);

                if (!string.IsNullOrWhiteSpace(metadata?.Value))
                    return metadata.Value;
            }
            catch
            {
                // ignore
            }

            return DefaultVersion;
        }

        private static bool TryParseVersion(string input, out Version version)
        {
            version = new Version(0, 0, 0);
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim().TrimStart('v', 'V');

            if (Version.TryParse(input, out Version? parsed) && parsed != null)
            {
                version = parsed;
                return true;
            }

            var parts = input.Split('.');
            if (parts.Length == 2
                && int.TryParse(parts[0], out int major)
                && int.TryParse(parts[1], out int minor))
            {
                version = new Version(major, minor, 0);
                return true;
            }

            return false;
        }

        private static string FloatToVersionString(float value)
        {
            if (value <= 0)
                return DefaultVersion;

            int major = (int)value;
            int minor = (int)Math.Round((value - major) * 10, MidpointRounding.AwayFromZero);
            if (minor < 0)
                minor = 0;

            return ToString(new Version(major, minor, 0));
        }

        private static string ToString(Version version)
            => $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
    }
}
