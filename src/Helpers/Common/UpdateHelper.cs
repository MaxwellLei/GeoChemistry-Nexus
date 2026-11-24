using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
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
        /// <summary>服务器公布的安装包 SHA256；为空表示服务器未提供。</summary>
        public string InstallerSha256 { get; set; } = string.Empty;

        /// <summary>当前选用的是否为不内置 .NET 运行时的精简版安装包。</summary>
        public bool IsLiteInstaller { get; set; }

        /// <summary>选用精简版时保留的完整版安装包信息，精简版下载失败时用于回退。</summary>
        public AppUpdateInfo? FullInstallerFallback { get; set; }

        /// <summary>
        /// 精简版不可用时切换为完整版安装包。成功切换返回 true；已是完整版或没有回退信息时返回 false。
        /// </summary>
        public bool TrySwitchToFullInstaller()
        {
            var full = FullInstallerFallback;
            if (!IsLiteInstaller || full == null)
                return false;

            InstallerDownloadUrl = full.InstallerDownloadUrl;
            FallbackInstallerDownloadUrl = full.FallbackInstallerDownloadUrl;
            InstallerFileName = full.InstallerFileName;
            InstallerSha256 = full.InstallerSha256;
            IsLiteInstaller = false;
            FullInstallerFallback = null;
            return true;
        }
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
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return await OfficialContentMirrorClient.SharedHttpClient.GetStringAsync(url, timeoutCts.Token);
        }

        public static string GetCurrentAppVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        }

        public static async Task<ServerInfo?> GetServerInfoAsync()
        {
            string json = await OfficialContentMirrorClient.GetStringAsync(OfficialContentEndpoints.ServerInfoFileName);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonHelper.Deserialize<ServerInfo>(json);
        }

        /// <summary>
        /// 发布合并用：优先读本地输出目录中的 server_info.json，否则拉取远端。
        /// </summary>
        public static ServerInfo LoadMergedServerInfo(string outputDir)
        {
            string localPath = Path.Combine(outputDir, OfficialContentEndpoints.ServerInfoFileName);
            if (File.Exists(localPath))
            {
                try
                {
                    var local = JsonHelper.Deserialize<ServerInfo>(File.ReadAllText(localPath));
                    if (local != null)
                        return local;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UpdateHelper] Load local server_info failed: {ex.Message}");
                }
            }

            try
            {
                string json = OfficialContentMirrorClient.GetStringAsync(OfficialContentEndpoints.ServerInfoFileName)
                    .GetAwaiter()
                    .GetResult();
                var remote = JsonHelper.Deserialize<ServerInfo>(json);
                if (remote != null)
                    return remote;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateHelper] Load remote server_info failed: {ex.Message}");
            }

            return new ServerInfo();
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
                    result.InstallerSha256 = NormalizeSha256(serverInfo?.LatestAppInstallerSha256);

                    TryApplyLiteInstaller(result, serverInfo, normalizedLatestVersion);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateHelper] GetLatestAppUpdateInfoAsync failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 满足条件时把安装包换成不内置 .NET 运行时的精简版，并保留完整版作为回退。
        /// 条件：服务器同时公布了精简版 SHA256 和所需运行时主版本，且本机已安装该主版本的 Desktop 运行时。
        /// 任一条件不满足都保持完整版，因此服务器漏填字段时不会让用户下载到不存在的文件。
        /// </summary>
        private static void TryApplyLiteInstaller(AppUpdateInfo info, ServerInfo? serverInfo, string version)
        {
            if (serverInfo == null)
                return;

            string liteSha256 = NormalizeSha256(serverInfo.LatestAppInstallerLiteSha256);
            if (string.IsNullOrEmpty(liteSha256))
                return;

            if (!DotNetRuntimeHelper.IsRequiredRuntimeSatisfied(serverInfo.RequiredRuntimeMajor))
                return;

            info.FullInstallerFallback = new AppUpdateInfo
            {
                InstallerFileName = info.InstallerFileName,
                InstallerDownloadUrl = info.InstallerDownloadUrl,
                FallbackInstallerDownloadUrl = info.FallbackInstallerDownloadUrl,
                InstallerSha256 = info.InstallerSha256
            };

            info.IsLiteInstaller = true;
            info.InstallerFileName = OfficialContentEndpoints.BuildInstallerFileName(version, lite: true);
            info.InstallerDownloadUrl = OfficialContentEndpoints.BuildGitHubInstallerUrl(version, lite: true);
            info.FallbackInstallerDownloadUrl = OfficialContentEndpoints.BuildCosInstallerUrl(version, lite: true);
            info.InstallerSha256 = liteSha256;
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
            if (fileInfo.Length <= 0)
                return false;

            // 服务器公布了 SHA256 时，缓存文件必须与之一致；不一致说明文件残缺或被替换，直接丢弃。
            if (!string.IsNullOrEmpty(updateInfo.InstallerSha256)
                && !VerifyFileSha256(installerPath, updateInfo.InstallerSha256))
            {
                TryDeleteFile(installerPath);
                installerPath = string.Empty;
                return false;
            }

            return true;
        }

        public static async Task<string> DownloadInstallerAsync(
            string downloadUrl,
            string destinationPath,
            IProgress<double>? progress = null,
            string? fallbackDownloadUrl = null,
            string? expectedSha256 = null)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException("Installer download URL is empty.");

            // 安装包会被直接执行，禁止走明文 HTTP。
            if (!IsHttpsUrl(downloadUrl))
                throw new InvalidOperationException("Installer download URL must use HTTPS.");

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
                string installerName = Path.GetFileName(destinationPath);
                if (string.IsNullOrWhiteSpace(installerName))
                    installerName = Path.GetFileName(new Uri(fallbackDownloadUrl).LocalPath);
                await OfficialContentMirrorClient.DownloadFileAsync(
                    $"{OfficialContentEndpoints.InstallersFolderName}/{installerName}",
                    destinationPath,
                    progress);
            }

            string normalizedHash = NormalizeSha256(expectedSha256);
            if (!string.IsNullOrEmpty(normalizedHash) && !VerifyFileSha256(destinationPath, normalizedHash))
            {
                TryDeleteFile(destinationPath);
                throw new InvalidDataException("Installer SHA256 mismatch. The downloaded file was discarded.");
            }

            return destinationPath;
        }

        private static bool IsHttpsUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeSha256(string? hash)
        {
            return (hash ?? string.Empty).Trim().Replace("-", string.Empty).ToLowerInvariant();
        }

        /// <summary>是否为 64 位十六进制的 SHA256 文本（允许带连字符和大小写）。</summary>
        public static bool IsValidSha256(string? hash)
        {
            string normalized = NormalizeSha256(hash);
            if (normalized.Length != 64)
                return false;

            foreach (char c in normalized)
            {
                if (!Uri.IsHexDigit(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 计算文件的 SHA256（小写十六进制）。在后台线程执行，不阻塞界面；文件不存在返回空串。
        /// </summary>
        public static Task<string> ComputeFileSha256Async(string filePath)
        {
            return Task.Run(() =>
            {
                if (!File.Exists(filePath))
                    return string.Empty;

                using var sha = System.Security.Cryptography.SHA256.Create();
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920);
                return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
            });
        }

        private static bool VerifyFileSha256(string filePath, string expectedSha256)
        {
            try
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                using var stream = File.OpenRead(filePath);
                string actual = Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
                return string.Equals(actual, NormalizeSha256(expectedSha256), StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateHelper] SHA256 verification failed: {ex.Message}");
                return false;
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // 下次下载会覆盖
            }
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

            TryDeleteFile(destinationPath + ".part");

            return Task.CompletedTask;
        }

        public static async Task DownloadFileAsync(string url, string destinationPath, IProgress<double>? progress = null)
        {
            // 先写入 .part 临时文件，下载完整并校验长度后再改名；
            // 中途失败或退出只会留下 .part，不会产生"看起来已下载"的残缺目标文件。
            string partPath = destinationPath + ".part";

            try
            {
                // 共享 HttpClient 无整体超时；沿用原 HttpClient 默认的 100 秒"等待响应头"超时，下载正文不受限。
                using var headerTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(100));
                using var response = await OfficialContentMirrorClient.SharedHttpClient.GetAsync(
                    url, HttpCompletionOption.ResponseHeadersRead, headerTimeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1 && progress != null;
                long totalRead = 0;

                await using (var contentStream = await response.Content.ReadAsStreamAsync())
                await using (var fileStream = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory())) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;

                        if (canReportProgress)
                            progress!.Report((double)totalRead / totalBytes * 100);
                    }
                }

                if (totalRead == 0)
                    throw new IOException("Downloaded content is empty.");

                if (totalBytes > 0 && totalRead != totalBytes)
                    throw new IOException($"Download incomplete: expected {totalBytes} bytes, received {totalRead}.");

                File.Move(partPath, destinationPath, overwrite: true);
            }
            catch
            {
                TryDeleteFile(partPath);
                throw;
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
