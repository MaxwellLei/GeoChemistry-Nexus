using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Helpers
{
    public class AppUpdateInfo
    {
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public string InstallerDownloadUrl { get; set; } = string.Empty;
        public string InstallerFileName { get; set; } = string.Empty;
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用于反序列化 GitHub Release API 响应的辅助类
    /// </summary>
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
    }

    /// <summary>
    /// 检查 GitHub Releases 更新并下载安装包
    /// </summary>
    public static class UpdateHelper
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/MaxwellLei/GeoChemistry-Nexus/releases/latest";
        private const string ServerInfoUrl = OfficialContentEndpoints.ServerInfoUrl;
        private const string InstallerAssetPattern = "GeoChemistryNexus-Setup-";
        private const string InstallerAssetSuffix = "-x64.exe";

        private static readonly HttpClient HttpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("GeoChemistry-Nexus-Update-Checker", "1.0"));
            return client;
        }

        public static string ComputeFileMd5(string filePath)
        {
            if (!File.Exists(filePath)) return string.Empty;

            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = md5.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public static async Task<string> GetUrlContentAsync(string url = ServerInfoUrl)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            return await client.GetStringAsync(url);
        }

        public static string GetCurrentAppVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        }

        public static async Task<AppUpdateInfo> GetLatestReleaseInfoAsync(string? currentVersionString = null, bool forceDownload = false)
        {
            currentVersionString ??= GetCurrentAppVersion();
            var result = new AppUpdateInfo();

            try
            {
                using var response = await HttpClient.GetAsync(GitHubApiUrl);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var latestRelease = JsonSerializer.Deserialize<GitHubRelease>(jsonResponse);

                if (latestRelease == null || string.IsNullOrWhiteSpace(latestRelease.TagName))
                    return result;

                string cleanedLatest = latestRelease.TagName.TrimStart('v', 'V');
                if (Version.TryParse(cleanedLatest, out Version? latestVersion) && latestVersion != null)
                {
                    latestVersion = NormalizeVersion(latestVersion);
                    result.LatestVersion = latestVersion.ToString(3);
                }
                else
                {
                    result.LatestVersion = cleanedLatest;
                }

                var installerAsset = latestRelease.Assets?
                    .FirstOrDefault(a =>
                        !string.IsNullOrWhiteSpace(a?.Name)
                        && a.Name.StartsWith(InstallerAssetPattern, StringComparison.OrdinalIgnoreCase)
                        && a.Name.EndsWith(InstallerAssetSuffix, StringComparison.OrdinalIgnoreCase));

                bool shouldDownload = forceDownload;
                if (!forceDownload)
                {
                    string cleanedCurrent = currentVersionString.TrimStart('v', 'V');
                    if (Version.TryParse(cleanedLatest, out Version? latest) &&
                        Version.TryParse(cleanedCurrent, out Version? current) &&
                        latest != null && current != null)
                    {
                        shouldDownload = NormalizeVersion(latest) > NormalizeVersion(current);
                    }
                }

                if (shouldDownload && installerAsset != null)
                {
                    result.HasUpdate = true;
                    result.InstallerDownloadUrl = installerAsset.BrowserDownloadUrl;
                    result.InstallerFileName = installerAsset.Name;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateHelper] GetLatestReleaseInfoAsync failed: {ex.Message}");
            }

            return result;
        }

        public static async Task<bool> CheckForUpdateAsync(string currentVersionString)
        {
            try
            {
                var info = await GetLatestReleaseInfoAsync(currentVersionString);
                return info.HasUpdate;
            }
            catch (HttpRequestException ex)
            {
                MessageHelper.Error(LanguageService.Instance["network_request_error"] + $": {ex.Message}");
                return false;
            }
            catch (JsonException ex)
            {
                MessageHelper.Error(LanguageService.Instance["json_parse_error"] + $": {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["unknown_error_occurred"] + $": {ex.Message}");
                return false;
            }
        }

        public static async Task CheckAndUpdatePlotCategoriesAsync()
        {
            try
            {
                string serverInfoJson = await GetUrlContentAsync(ServerInfoUrl);
                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(serverInfoJson);

                if (serverInfo == null || string.IsNullOrEmpty(serverInfo.ListPlotCategoriesHash))
                    return;

                string localPath = FileHelper.GetDataPath("PlotData", "PlotTemplateCategories.json");
                string localHash = File.Exists(localPath) ? ComputeFileMd5(localPath) : string.Empty;

                if (!string.Equals(localHash, serverInfo.ListPlotCategoriesHash, StringComparison.OrdinalIgnoreCase))
                {
                    string downloadUrl = ServerInfoUrl.Replace("server_info.json", "PlotTemplateCategories.json");
                    string? directory = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    await DownloadFileAsync(downloadUrl, localPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update PlotTemplateCategories.json: {ex.Message}");
            }
        }

        public static string GetInstallerDownloadPath(string? fileName = null)
        {
            string dir = Path.Combine(Path.GetTempPath(), "GeoChemistryNexus");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string safeName = string.IsNullOrWhiteSpace(fileName)
                ? "GeoChemistryNexus-Setup.exe"
                : Path.GetFileName(fileName);

            return Path.Combine(dir, safeName);
        }

        public static async Task<string> DownloadInstallerAsync(
            string downloadUrl,
            string destinationPath,
            IProgress<double>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException("Installer download URL is empty.");

            string? directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await DownloadFileAsync(downloadUrl, destinationPath, progress);
            return destinationPath;
        }

        public static void LaunchInstallerAndShutdown(string installerPath)
        {
            if (!File.Exists(installerPath))
                throw new FileNotFoundException("Installer not found.", installerPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/CLOSEAPPLICATIONS",
                UseShellExecute = true
            });

            Application.Current?.Shutdown();
        }

        public static void OpenLatestReleasePage()
        {
            Process.Start(new ProcessStartInfo("https://github.com/MaxwellLei/GeoChemistry-Nexus/releases/latest")
            {
                UseShellExecute = true
            });
        }

        public static async Task DownloadFileAsync(string url, string destinationPath, IProgress<double>? progress = null)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("GeoChemistry-Nexus-Update-Checker", "1.0"));

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1 && progress != null;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory())) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (canReportProgress)
                    progress!.Report((double)totalRead / totalBytes * 100);
            }
        }

        private static Version NormalizeVersion(Version version)
        {
            return new Version(
                version.Major,
                version.Minor,
                version.Build >= 0 ? version.Build : 0);
        }
    }
}
