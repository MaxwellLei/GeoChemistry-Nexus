using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 官方图解内容 CDN / COS 端点常量
    /// </summary>
    public static class OfficialContentEndpoints
    {
        public const string CosBaseUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com";

        public const string CosChinaBaseUrl = "https://geochemistrynexus-cn-1303234197.cos.ap-beijing.myqcloud.com";

        /// <summary>
        /// 客户端内置的对象存储根地址。默认顺序是香港、北京。
        /// 简体中文界面会把北京放到前面，见 <see cref="EnumerateBuiltinBaseUrls"/>。
        /// </summary>
        public static readonly string[] BuiltinBaseUrls =
        {
            CosBaseUrl,
            CosChinaBaseUrl
        };

        public const string ServerInfoUrl = CosBaseUrl + "/server_info.json";
        public const string HomeLinksCatalogUrl = CosBaseUrl + "/HomeLinksCatalog.json";
        public const string AnnouncementsUrl = CosBaseUrl + "/Announcements.json";
        public const string GraphMapListUrl = CosBaseUrl + "/GraphMapList.json";
        public const string PlotTemplateCategoriesUrl = CosBaseUrl + "/PlotTemplateCategories.json";

        public const string GraphMapListFileName = "GraphMapList.json";
        public const string PlotTemplateCategoriesFileName = "PlotTemplateCategories.json";
        public const string ServerInfoFileName = "server_info.json";
        public const string HomeLinksCatalogFileName = "HomeLinksCatalog.json";
        public const string AnnouncementsFileName = "Announcements.json";
        public const string PublishManifestFileName = "publish_manifest.json";
        public const string TemplatesFolderName = "Templates";
        public const string InstallersFolderName = "Installers";

        public const string GitHubLatestReleaseUrl = "https://github.com/MaxwellLei/GeoChemistry-Nexus/releases/latest";
        public const string GitHubReleaseDownloadBaseUrl = "https://github.com/MaxwellLei/GeoChemistry-Nexus/releases/download";
        public const string InstallerAssetPrefix = "GeoChemistryNexus-Setup-";
        public const string InstallerAssetSuffix = "-x64.exe";

        public const string DefaultRegion = "ap-hongkong";
        public const string DefaultBucket = "geochemistrynexus-1303234197";

        public const string GeothermometerFolderName = "Geothermometer";
        public const string GeothermometerBaseUrl = CosBaseUrl + "/" + GeothermometerFolderName;
        public const string GeoTListUrl = GeothermometerBaseUrl + "/GeoT-List.json";
        public const string GeoTIndexUrl = GeothermometerBaseUrl + "/GeoT-index.json";
        public const string GeoTMineralCategoriesUrl = GeothermometerBaseUrl + "/GeoTMineralCategories.json";
        public const string GeoTListFileName = "GeoT-List.json";
        public const string GeoTIndexFileName = "GeoT-index.json";
        public const string GeoTMineralCategoriesFileName = "GeoTMineralCategories.json";
        public const string GeothermometerPublishManifestFileName = "geothermometer_publish_manifest.json";

        public static string BuildInstallerFileName(string version)
        {
            return $"{InstallerAssetPrefix}{version}{InstallerAssetSuffix}";
        }

        public static string BuildGitHubInstallerUrl(string version)
        {
            return $"{GitHubReleaseDownloadBaseUrl}/v{version}/{BuildInstallerFileName(version)}";
        }

        public static string BuildCosInstallerUrl(string version)
        {
            return $"{CosBaseUrl}/{InstallersFolderName}/{BuildInstallerFileName(version)}";
        }

        /// <summary>
        /// 简体中文先试北京，其余界面先试香港。另一台始终作为后备。
        /// </summary>
        public static IEnumerable<string> EnumerateBuiltinBaseUrls(bool preferChinaMainland)
        {
            if (preferChinaMainland)
            {
                yield return CosChinaBaseUrl;
                yield return CosBaseUrl;
                yield break;
            }

            yield return CosBaseUrl;
            yield return CosChinaBaseUrl;
        }

        public static string NormalizeBaseUrl(string? url)
        {
            return (url ?? string.Empty).Trim().TrimEnd('/');
        }

        public static string Combine(string? baseUrl, string? relativePath)
        {
            string root = NormalizeBaseUrl(baseUrl);
            string relative = (relativePath ?? string.Empty).Trim().TrimStart('/');
            if (string.IsNullOrEmpty(root))
                return relative;
            if (string.IsNullOrEmpty(relative))
                return root;
            return root + "/" + relative;
        }

        public static string BuildDefaultPublicBaseUrl(string? bucket, string? region)
        {
            string bucketName = (bucket ?? string.Empty).Trim();
            string regionName = (region ?? string.Empty).Trim();
            if (bucketName.Length == 0 || regionName.Length == 0)
                return string.Empty;

            return $"https://{bucketName}.cos.{regionName}.myqcloud.com";
        }
    }
}
