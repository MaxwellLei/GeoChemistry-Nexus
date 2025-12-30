using GeoChemistryNexus.Services;
using HandyControl.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 用于反序列化 GitHub Release API 响应的辅助类
    /// </summary>
    public class GitHubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string TagName { get; set; }
    }

    /// <summary>
    /// 检查 GitHub Releases 更新
    /// </summary>
    public static class UpdateHelper
    {
        // 仓库的 API URL
        private const string GitHubApiUrl = "https://api.github.com/repos/MaxwellLei/GeoChemistry-Nexus/releases/latest";

        // 服务器信息 URL 常量
        private const string ServerInfoUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/server_info.json";

        // 使用静态 HttpClient 实例
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// 获取当前应用程序的版本号（Float格式，取前两位，例如 1.2）
        /// 使用 FileVersion 而不是 AssemblyVersion
        /// </summary>
        public static float GetCurrentVersionFloat()
        {
            try
            {
                var attr = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<System.Reflection.AssemblyFileVersionAttribute>();
                
                if (attr != null && !string.IsNullOrEmpty(attr.Version))
                {
                    if (Version.TryParse(attr.Version, out Version v))
                    {
                        string versionStr = $"{v.Major}.{v.Minor}";
                        if (float.TryParse(versionStr, out float result))
                        {
                            return result;
                        }
                    }
                    // 备用：尝试直接解析
                    if (float.TryParse(attr.Version, out float directResult))
                    {
                        return directResult;
                    }
                }
            }
            catch
            {
                // ignore
            }

            // Fallback: 如果获取 FileVersion 失败，尝试获取 AssemblyVersion
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null) return 1.0f;
            
            string fallbackStr = $"{version.Major}.{version.Minor}";
            if (float.TryParse(fallbackStr, out float fallbackResult))
            {
                return fallbackResult;
            }
            return 1.0f;
        }

        // 计算文件的 MD5 哈希值 (小写 hex 字符串)
        public static string ComputeFileMd5(string filePath)
        {
            if (!File.Exists(filePath)) return string.Empty;

            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        // 简单的 GET 请求获取 JSON 字符串
        public static async Task<string> GetUrlContentAsync(string url = ServerInfoUrl)
        {
            using (var client = new HttpClient())
            {
                // 可以设置超时时间
                client.Timeout = TimeSpan.FromSeconds(10);
                return await client.GetStringAsync(url);
            }
        }

        /// <summary>
        /// 异步检查是否有新版本发布。
        /// </summary>
        /// <param name="currentVersionString">应用程序当前的有好版本字符串，例如 "1.0.0"。</param>
        /// <returns>如果存在新版本，则返回 true；否则返回 false。</returns>
        public static async Task<bool> CheckForUpdateAsync(string currentVersionString)
        {
            // GitHub API 要求设置 User-Agent
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GeoChemistry-Nexus-Update-Checker", "1.0"));

            try
            {
                // 发起 GET 请求
                HttpResponseMessage response = await httpClient.GetAsync(GitHubApiUrl);

                // 确保请求成功
                response.EnsureSuccessStatusCode();

                // 读取响应内容
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // 直接反序列化 JSON 对象，而不是列表
                var latestRelease = JsonSerializer.Deserialize<GitHubRelease>(jsonResponse);

                if (latestRelease == null)
                {
                    MessageHelper.Warning(LanguageService.Instance["unable_to_get_latest_release_info"]);
                    return false;
                }

                string latestVersionTag = latestRelease.TagName;

                // 清理版本号字符串
                string cleanedLatestVersionString = latestVersionTag.TrimStart('v', 'V');
                string cleanedCurrentVersionString = currentVersionString.TrimStart('v', 'V');

                // 解析版本号
                if (Version.TryParse(cleanedLatestVersionString, out Version latestVersion) &&
                    Version.TryParse(cleanedCurrentVersionString, out Version currentVersion))
                {
                    // 强制转换为3位版本号 (忽略 Revision)
                    // 如果只有两位版本号，则默认为 0
                    latestVersion = new Version(latestVersion.Major, latestVersion.Minor, latestVersion.Build >= 0 ? latestVersion.Build : 0);
                    currentVersion = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build >= 0 ? currentVersion.Build : 0);

                    // 比较版本号
                    // 如果最新版本号大于当前版本号，则有更新
                    if(latestVersion == currentVersion)
                    {
                        return false;
                    }
                    return latestVersion > currentVersion;
                }
                else
                {
                    MessageHelper.Warning(LanguageService.Instance["cannot_parse_version_number_check_release_address"]);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                // 网络请求错误
                MessageHelper.Error(LanguageService.Instance["network_request_error"] + $": {ex.Message}");
                return false;
            }
            catch (JsonException ex)
            {
                // JSON 解析错误
                MessageHelper.Error(LanguageService.Instance["json_parse_error"] + $": {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // 其他未知错误
                MessageHelper.Error(LanguageService.Instance["unknown_error_occurred"] + $": {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查并更新 PlotTemplateCategories.json
        /// </summary>
        public static async Task CheckAndUpdatePlotCategoriesAsync()
        {
            try
            {
                // 1. 获取 server_info.json
                string serverInfoJson = await GetUrlContentAsync(ServerInfoUrl);
                var serverInfo = JsonSerializer.Deserialize<GeoChemistryNexus.Models.ServerInfo>(serverInfoJson);

                if (serverInfo == null || string.IsNullOrEmpty(serverInfo.ListPlotCategoriesHash))
                {
                    return;
                }

                // 2. 计算本地文件 Hash
                string localPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotTemplateCategories.json");
                string localHash = string.Empty;

                if (File.Exists(localPath))
                {
                    localHash = ComputeFileMd5(localPath);
                }

                // 3. 比较 Hash
                if (!string.Equals(localHash, serverInfo.ListPlotCategoriesHash, StringComparison.OrdinalIgnoreCase))
                {
                    // 4. 下载更新
                    string downloadUrl = ServerInfoUrl.Replace("server_info.json", "PlotTemplateCategories.json");

                    // 确保目录存在
                    string directory = Path.GetDirectoryName(localPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await DownloadFileAsync(downloadUrl, localPath);
                }
            }
            catch (Exception ex)
            {
                // 更新失败不影响主流程
                Debug.WriteLine($"Failed to update PlotTemplateCategories.json: {ex.Message}");
            }
        }

        /// <summary>
        /// 带进度的文件下载
        /// </summary>
        public static async Task DownloadFileAsync(string url, string destinationPath, IProgress<double> progress = null)
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1 && progress != null;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    progress.Report((double)totalRead / totalBytes * 100);
                }
            }
        }
    }
}
