using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 按对象存储根地址依次尝试官方内容。成功后记住该根地址，供本次进程后续请求优先使用。
    /// </summary>
    public static class OfficialContentMirrorClient
    {
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(8);
        private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(10);
        private static readonly object Gate = new();
        private static string? _preferredBaseUrl;
        private static readonly List<string> LearnedBaseUrls = new();

        public static void RememberPreferred(string? baseUrl)
        {
            string normalized = OfficialContentEndpoints.NormalizeBaseUrl(baseUrl);
            if (!IsHttpUrl(normalized))
                return;

            lock (Gate)
                _preferredBaseUrl = normalized;
        }

        public static void MergeLearnedMirrors(IEnumerable<string>? mirrors)
        {
            if (mirrors == null)
                return;

            lock (Gate)
            {
                foreach (string mirror in mirrors)
                {
                    string normalized = OfficialContentEndpoints.NormalizeBaseUrl(mirror);
                    // 服务器下发的镜像会参与安装包等内容的下载，只接受 HTTPS，避免被降级到明文。
                    if (!normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        || ContainsUrl(LearnedBaseUrls, normalized))
                        continue;

                    LearnedBaseUrls.Add(normalized);
                }
            }
        }

        public static async Task<string> GetStringAsync(string relativePath)
        {
            Exception? lastError = null;
            foreach (string baseUrl in GetAttemptOrder())
            {
                try
                {
                    string json = await GetStringFromAbsoluteAsync(OfficialContentEndpoints.Combine(baseUrl, relativePath)).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(json))
                        throw new IOException("Empty content.");

                    RememberPreferred(baseUrl);
                    TryLearnFromServerInfo(relativePath, json);
                    return json;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Debug.WriteLine($"[OfficialContentMirror] {baseUrl}/{relativePath} failed: {ex.Message}");
                }
            }

            throw lastError ?? new HttpRequestException("No official content mirror is reachable.");
        }

        public static async Task DownloadFileAsync(string relativePath, string destinationPath, IProgress<double>? progress = null)
        {
            Exception? lastError = null;
            foreach (string baseUrl in GetAttemptOrder())
            {
                try
                {
                    await DownloadFromAbsoluteAsync(
                        OfficialContentEndpoints.Combine(baseUrl, relativePath),
                        destinationPath,
                        progress).ConfigureAwait(false);
                    RememberPreferred(baseUrl);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Debug.WriteLine($"[OfficialContentMirror] {baseUrl}/{relativePath} failed: {ex.Message}");
                    // 失败时 .part 已在下载流程内清理；目标文件保持原样，不再误删已有的好文件。
                }
            }

            throw lastError ?? new HttpRequestException("No official content mirror is reachable.");
        }

        /// <summary>
        /// 把已启用目标的公共根地址写入本次导出的 server_info.json。文件不存在时不创建。
        /// </summary>
        public static void StampServerInfoFile(string? serverInfoPath, IEnumerable<string>? mirrors)
        {
            if (string.IsNullOrWhiteSpace(serverInfoPath) || !File.Exists(serverInfoPath))
                return;

            var info = JsonHelper.Deserialize<ServerInfo>(File.ReadAllText(serverInfoPath)) ?? new ServerInfo();
            info.ContentMirrors = NormalizeMirrorList(mirrors);
            var options = JsonHelper.CreateRelaxedIndentedOptions();
            File.WriteAllText(serverInfoPath, JsonSerializer.Serialize(info, options));
        }

        public static string? TryGetRelativePath(string? absoluteOrRelative)
        {
            if (string.IsNullOrWhiteSpace(absoluteOrRelative))
                return null;

            string value = absoluteOrRelative.Trim();
            if (!IsHttpUrl(value))
                return value.TrimStart('/');

            foreach (string baseUrl in GetKnownBaseUrls())
            {
                string root = OfficialContentEndpoints.NormalizeBaseUrl(baseUrl);
                string prefix = root + "/";
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return value.Substring(prefix.Length);
            }

            return null;
        }

        private static async Task<string> GetStringFromAbsoluteAsync(string url)
        {
            using var timeoutCts = new CancellationTokenSource(ConnectTimeout);
            return await SharedHttpClient.GetStringAsync(url, timeoutCts.Token).ConfigureAwait(false);
        }

        private static async Task DownloadFromAbsoluteAsync(string url, string destinationPath, IProgress<double>? progress)
        {
            string? directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using var connectCts = new CancellationTokenSource(ConnectTimeout);
            using var response = await SharedHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, connectCts.Token).ConfigureAwait(false);
            // 响应头已经到达。把 8 秒连接计时改为整个下载的 10 分钟上限（原先由 HttpClient.Timeout 承担），
            // 令牌不能提前释放，且需传给正文读取，才能让上限继续生效。
            connectCts.CancelAfter(DownloadTimeout);
            var downloadToken = connectCts.Token;
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            bool canReportProgress = totalBytes > 0 && progress != null;

            // 先写 .part，下载完整且长度一致后再改名，避免留下残缺的目标文件。
            string partPath = destinationPath + ".part";
            try
            {
                long totalRead = 0;
                await using (var contentStream = await response.Content.ReadAsStreamAsync(downloadToken).ConfigureAwait(false))
                await using (var fileStream = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(), downloadToken).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), downloadToken).ConfigureAwait(false);
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

        private static void TryLearnFromServerInfo(string relativePath, string json)
        {
            string name = relativePath.Trim().TrimStart('/');
            if (!name.Equals(OfficialContentEndpoints.ServerInfoFileName, StringComparison.OrdinalIgnoreCase))
                return;

            var info = JsonHelper.Deserialize<ServerInfo>(json);
            MergeLearnedMirrors(info?.ContentMirrors);
        }

        private static List<string> GetAttemptOrder()
        {
            var order = new List<string>();
            lock (Gate)
            {
                AddUrl(order, _preferredBaseUrl);
                foreach (string url in EnumerateBuiltinBaseUrls())
                    AddUrl(order, url);
                foreach (string url in LearnedBaseUrls)
                    AddUrl(order, url);
            }

            if (order.Count == 0)
                AddUrl(order, OfficialContentEndpoints.CosBaseUrl);

            return order;
        }

        private static IEnumerable<string> EnumerateBuiltinBaseUrls()
        {
            string language = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            bool preferChina = string.Equals(language, "zh-CN", StringComparison.OrdinalIgnoreCase);
            return OfficialContentEndpoints.EnumerateBuiltinBaseUrls(preferChina);
        }

        private static List<string> GetKnownBaseUrls()
        {
            var urls = GetAttemptOrder();
            foreach (string url in OfficialContentEndpoints.BuiltinBaseUrls)
                AddUrl(urls, url);
            return urls;
        }

        private static List<string> NormalizeMirrorList(IEnumerable<string>? mirrors)
        {
            var list = new List<string>();
            if (mirrors == null)
                return list;

            foreach (string mirror in mirrors)
                AddUrl(list, mirror);

            return list;
        }

        private static void AddUrl(List<string> urls, string? url)
        {
            string normalized = OfficialContentEndpoints.NormalizeBaseUrl(url);
            if (!IsHttpUrl(normalized) || ContainsUrl(urls, normalized))
                return;

            urls.Add(normalized);
        }

        private static bool ContainsUrl(List<string> urls, string normalized)
        {
            foreach (string existing in urls)
            {
                if (string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsHttpUrl(string url)
        {
            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 进程内共享的 HttpClient（复用连接池与 DNS 缓存，避免每次请求新建造成端口耗尽）。
        /// 超时由各调用方通过 CancellationToken 控制，因此此处不设置整体 Timeout。
        /// </summary>
        internal static HttpClient SharedHttpClient { get; } = CreateSharedClient();

        private static HttpClient CreateSharedClient()
        {
            var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("GeoChemistry-Nexus-Update-Checker", "1.0"));
            return client;
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
                // 下一次尝试会覆盖或再次删除
            }
        }
    }
}
