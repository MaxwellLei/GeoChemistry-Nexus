using System;
using System.IO;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解 / 温压计用户包文件后缀约定。
    /// 用户导出使用主后缀；导入兼容遗留 .zip；服务端 CDN/发布仍使用 .zip。
    /// </summary>
    public static class TemplatePackageFileExtensions
    {
        public const string DiagramPrimary = ".gndiag";
        public const string GeothermometerPrimary = ".gngtm";
        public const string LegacyZip = ".zip";
        public const string Json = ".json";

        /// <summary>图解拖放允许的后缀列表（逗号分隔，供 FileDropBehavior 使用）。</summary>
        public const string DiagramDropAllowed = ".json,.gndiag,.zip";

        /// <summary>温压计拖放允许的后缀列表。</summary>
        public const string GeothermometerDropAllowed = ".gngtm,.zip";

        public static string Normalize(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;

            string ext = extension.Trim();
            if (!ext.StartsWith('.'))
                ext = "." + ext;
            return ext.ToLowerInvariant();
        }

        public static string FromPath(string? path) =>
            Normalize(Path.GetExtension(path));

        /// <summary>图解压缩包（.gndiag 或遗留 .zip）。</summary>
        public static bool IsDiagramPackage(string? extension) =>
            IsDiagramPackageExt(Normalize(extension));

        public static bool IsDiagramPackagePath(string? path) =>
            IsDiagramPackageExt(FromPath(path));

        /// <summary>可打开的图解文件（包或裸 JSON）。</summary>
        public static bool IsDiagramOpenable(string? extension) =>
            IsDiagramOpenableExt(Normalize(extension));

        public static bool IsDiagramOpenablePath(string? path) =>
            IsDiagramOpenableExt(FromPath(path));

        /// <summary>温压计压缩包（.gngtm 或遗留 .zip）。</summary>
        public static bool IsGeothermometerPackage(string? extension) =>
            IsGeothermometerPackageExt(Normalize(extension));

        public static bool IsGeothermometerPackagePath(string? path) =>
            IsGeothermometerPackageExt(FromPath(path));

        /// <summary>是否为 zip 字节流包（任一主后缀或 .zip）。</summary>
        public static bool IsZipLikePackage(string? extension)
        {
            string ext = Normalize(extension);
            return ext == DiagramPrimary || ext == GeothermometerPrimary || ext == LegacyZip;
        }

        /// <summary>启动关联仅处理新后缀，避免裸 .zip 歧义。</summary>
        public static bool IsAssociatedPackagePath(string? path)
        {
            string ext = FromPath(path);
            return ext == DiagramPrimary || ext == GeothermometerPrimary;
        }

        public static bool IsDiagramAssociatedPath(string? path) =>
            FromPath(path) == DiagramPrimary;

        public static bool IsGeothermometerAssociatedPath(string? path) =>
            FromPath(path) == GeothermometerPrimary;

        private static bool IsDiagramPackageExt(string ext) =>
            ext == DiagramPrimary || ext == LegacyZip;

        private static bool IsDiagramOpenableExt(string ext) =>
            ext == DiagramPrimary || ext == LegacyZip || ext == Json;

        private static bool IsGeothermometerPackageExt(string ext) =>
            ext == GeothermometerPrimary || ext == LegacyZip;
    }
}
