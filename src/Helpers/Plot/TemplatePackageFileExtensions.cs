using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解 / 温压计 / EPMA 用户包的后缀约定。
    /// 界面只展示复合后缀（.diag.zip、.gtm.zip、.epma.zip），内容仍是 zip；
    /// 裸 .zip 作为隐藏的兼容后缀仍可导入（不出现在对话框说明和拖放提示中），但不属于其他模块的复合后缀包。
    /// 旧的 .gndiag、.gngtm 不再支持。
    /// <see cref="Path.GetExtension"/> 对这类文件名只返回 .zip，因此判断路径必须用 <see cref="HasSuffix"/>。
    /// </summary>
    public static class TemplatePackageFileExtensions
    {
        public const string DiagramPrimary = ".diag.zip";
        public const string GeothermometerPrimary = ".gtm.zip";
        public const string EpmaPrimary = ".epma.zip";
        public const string Zip = ".zip";
        public const string Json = ".json";

        /// <summary>图解拖放允许的后缀（逗号分隔）。芯片只展示 <see cref="DiagramPrimary"/>。</summary>
        public const string DiagramDropAllowed = DiagramPrimary + "," + Zip + "," + Json;

        /// <summary>温压计拖放允许的后缀。芯片只展示 <see cref="GeothermometerPrimary"/>。</summary>
        public const string GeothermometerDropAllowed = GeothermometerPrimary + "," + Zip;

        /// <summary>EPMA 拖放允许的后缀。芯片只展示 <see cref="EpmaPrimary"/>。</summary>
        public const string EpmaDropAllowed = EpmaPrimary + "," + Zip;

        public static string Normalize(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;

            string ext = extension.Trim();
            if (!ext.StartsWith('.'))
                ext = "." + ext;
            return ext.ToLowerInvariant();
        }

        /// <summary>最后一段扩展名。复合后缀请改用 <see cref="HasSuffix"/>。</summary>
        public static string FromPath(string? path) =>
            Normalize(Path.GetExtension(path));

        public static bool HasSuffix(string? path, string? suffix)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(suffix))
                return false;

            string name = Path.GetFileName(path.Trim().Trim('"'));
            return name.EndsWith(Normalize(suffix), StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesAnySuffix(string? path, IEnumerable<string>? suffixes)
        {
            if (suffixes == null)
                return false;

            foreach (string suffix in suffixes)
            {
                if (HasSuffix(path, suffix))
                    return true;
            }

            return false;
        }

        /// <summary>把逗号分隔的后缀列表规范成带点、按长度从长到短排列的列表。</summary>
        public static IReadOnlyList<string> SplitSuffixList(string? list)
        {
            if (string.IsNullOrWhiteSpace(list))
                return Array.Empty<string>();

            return list
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalize)
                .Where(ext => ext.Length > 1)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(ext => ext.Length)
                .ToArray();
        }

        /// <summary>
        /// 保存对话框若只补上了 .zip，或用户没写扩展名，则改成模块主后缀。
        /// 已经是主后缀时保持原样。
        /// </summary>
        public static string WithPrimarySuffix(string path, string suffix)
        {
            if (string.IsNullOrWhiteSpace(path) || HasSuffix(path, suffix))
                return path;

            if (HasSuffix(path, Zip))
                path = path.Substring(0, path.Length - Zip.Length);

            return path + suffix;
        }

        /// <summary>图解包：.diag.zip，或不属于其他模块的裸 .zip。</summary>
        public static bool IsDiagramPackagePath(string? path) =>
            IsPackagePath(path, DiagramPrimary);

        /// <summary>可打开的图解文件（图解包或裸 JSON）。</summary>
        public static bool IsDiagramOpenablePath(string? path) =>
            IsDiagramPackagePath(path) || FromPath(path) == Json;

        /// <summary>温压计包：.gtm.zip，或不属于其他模块的裸 .zip。</summary>
        public static bool IsGeothermometerPackagePath(string? path) =>
            IsPackagePath(path, GeothermometerPrimary);

        /// <summary>EPMA 包：.epma.zip，或不属于其他模块的裸 .zip。</summary>
        public static bool IsEpmaPackagePath(string? path) =>
            IsPackagePath(path, EpmaPrimary);

        /// <summary>启动关联（双击打开）只认无歧义的复合后缀，不把裸 .zip 交给某个模块。</summary>
        public static bool IsAssociatedPackagePath(string? path) =>
            IsDiagramAssociatedPath(path) || IsGeothermometerAssociatedPath(path);

        public static bool IsDiagramAssociatedPath(string? path) =>
            HasSuffix(path, DiagramPrimary);

        public static bool IsGeothermometerAssociatedPath(string? path) =>
            HasSuffix(path, GeothermometerPrimary);

        private static bool IsPackagePath(string? path, string ownPrimary)
        {
            if (HasSuffix(path, ownPrimary))
                return true;

            if (IsOtherModulePackage(path, ownPrimary))
                return false;

            return FromPath(path) == Zip;
        }

        private static bool IsOtherModulePackage(string? path, string ownPrimary)
        {
            if (ownPrimary != DiagramPrimary && HasSuffix(path, DiagramPrimary))
                return true;
            if (ownPrimary != GeothermometerPrimary && HasSuffix(path, GeothermometerPrimary))
                return true;
            if (ownPrimary != EpmaPrimary && HasSuffix(path, EpmaPrimary))
                return true;
            return false;
        }
    }
}
