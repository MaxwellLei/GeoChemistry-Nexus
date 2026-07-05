using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Helpers
{
    public class AppUpdateInfo
    {
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public string InstallerDownloadUrl { get; set; } = string.Empty;
        public string FallbackInstallerDownloadUrl { get; set; } = string.Empty;
        public string InstallerFileName { get; set; } = string.Empty;
    }

    public class AppMinimumVersionCheckResult
    {
        public bool IsVersionUnsupported { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string MinimumSupportedVersion { get; set; } = string.Empty;
    }

    /// <summary>
    /// 检查服务器版本信息并下载安装包
    /// </summary>
    public static class UpdateHelper
    {
        private const string ServerInfoUrl = OfficialContentEndpoints.ServerInfoUrl;
        private const int PrimaryInstallerDownloadRetryCount = 2;

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

        public static async Task<ServerInfo?> GetServerInfoAsync()
        {
            string json = await GetUrlContentAsync(ServerInfoUrl);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<ServerInfo>(json);
        }

        public static async Task<AppMinimumVersionCheckResult> CheckMinimumSupportedVersionAsync(string? currentVersionString = null)
        {
            currentVersionString ??= GetCurrentAppVersion();
            var result = new AppMinimumVersionCheckResult
            {
                CurrentVersion = NormalizeVersionText(currentVersionString)
            };

            try
            {
                var serverInfo = await GetServerInfoAsync();
                string minimumVersionText = NormalizeVersionText(serverInfo?.MinimumSupportedVersion);
                result.MinimumSupportedVersion = minimumVersionText;

                if (string.IsNullOrWhiteSpace(minimumVersionText))
                    return result;

                result.IsVersionUnsupported = IsVersionLowerThan(result.CurrentVersion, minimumVersionText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateHelper] CheckMinimumSupportedVersionAsync failed: {ex.Message}");
            }

            return result;
        }

        public static bool IsVersionLowerThan(string currentVersionString, string minimumVersionString)
        {
            if (!TryNormalizeVersion(currentVersionString, out Version? currentVersion)
                || !TryNormalizeVersion(minimumVersionString, out Version? minimumVersion)
                || currentVersion == null
                || minimumVersion == null)
            {
                return false;
            }

            return currentVersion < minimumVersion;
        }

        public static bool TryNormalizeVersion(string? versionString, out Version? version)
        {
            version = null;
            string cleaned = NormalizeVersionText(versionString);
            if (string.IsNullOrWhiteSpace(cleaned))
                return false;

            if (!Version.TryParse(cleaned, out Version? parsed) || parsed == null)
                return false;

            version = NormalizeVersion(parsed);
            return true;
        }

        public static async Task<AppUpdateInfo> GetLatestAppUpdateInfoAsync(string? currentVersionString = null, bool forceDownload = false)
        {
            currentVersionString ??= GetCurrentAppVersion();
            var result = new AppUpdateInfo();

            try
            {
                var serverInfo = await GetServerInfoAsync();
                string latestVersionText = NormalizeVersionText(serverInfo?.LatestAppVersion);
                if (string.IsNullOrWhiteSpace(latestVersionText))
                    return result;

                if (!TryNormalizeVersion(latestVersionText, out Version? latestVersion) || latestVersion == null)
                    return result;

                string normalizedLatestVersion = latestVersion.ToString(3);
                result.LatestVersion = normalizedLatestVersion;

                bool shouldDownload = forceDownload;
                if (!forceDownload)
                    shouldDownload = IsVersionLowerThan(currentVersionString, normalizedLatestVersion);

                if (shouldDownload)
                {
                    result.HasUpdate = true;
                    result.InstallerFileName = OfficialContentEndpoints.BuildInstallerFileName(normalizedLatestVersion);
                    result.InstallerDownloadUrl = OfficialContentEndpoints.BuildGitHubInstallerUrl(normalizedLatestVersion);
                    result.FallbackInstallerDownloadUrl = OfficialContentEndpoints.BuildCosInstallerUrl(normalizedLatestVersion);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateHelper] GetLatestAppUpdateInfoAsync failed: {ex.Message}");
            }

            return result;
        }

        public static async Task<bool> CheckForUpdateAsync(string currentVersionString)
        {
            try
            {
                var info = await GetLatestAppUpdateInfoAsync(currentVersionString);
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

        public static bool TryGetCachedInstallerPath(AppUpdateInfo? updateInfo, out string installerPath)
        {
            installerPath = string.Empty;

            if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.InstallerFileName))
                return false;

            installerPath = GetInstallerDownloadPath(updateInfo.InstallerFileName);
            if (!File.Exists(installerPath))
                return false;

            var fileInfo = new FileInfo(installerPath);
            return fileInfo.Length > 0;
        }

        public static async Task<string> DownloadInstallerAsync(
            string downloadUrl,
            string destinationPath,
            IProgress<double>? progress = null,
            string? fallbackDownloadUrl = null)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException("Installer download URL is empty.");

            string? directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                await DownloadFileWithRetriesAsync(downloadUrl, destinationPath, PrimaryInstallerDownloadRetryCount, progress);
            }
            catch (Exception primaryEx) when (!string.IsNullOrWhiteSpace(fallbackDownloadUrl))
            {
                Debug.WriteLine($"[UpdateHelper] GitHub installer download failed, switching to COS: {primaryEx.Message}");
                await DeletePartialDownloadAsync(destinationPath);
                await DownloadFileAsync(fallbackDownloadUrl!, destinationPath, progress);
            }

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
            Process.Start(new ProcessStartInfo(OfficialContentEndpoints.GitHubLatestReleaseUrl)
            {
                UseShellExecute = true
            });
        }

        private static async Task DownloadFileWithRetriesAsync(
            string url,
            string destinationPath,
            int retryCount,
            IProgress<double>? progress = null)
        {
            Exception? lastException = null;
            int attempts = Math.Max(1, retryCount);

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    await DeletePartialDownloadAsync(destinationPath);
                    await DownloadFileAsync(url, destinationPath, progress);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Debug.WriteLine($"[UpdateHelper] Download attempt {attempt}/{attempts} failed: {ex.Message}");

                    if (attempt < attempts)
                        await Task.Delay(1000);
                }
            }

            throw lastException ?? new InvalidOperationException("Download failed.");
        }

        private static Task DeletePartialDownloadAsync(string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            return Task.CompletedTask;
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

        private static string NormalizeVersionText(string? versionString)
        {
            return (versionString ?? string.Empty).Trim().TrimStart('v', 'V');
        }
    }
}
