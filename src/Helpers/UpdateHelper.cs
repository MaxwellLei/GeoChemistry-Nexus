using HandyControl.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
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

        // 使用静态 HttpClient 实例
        private static readonly HttpClient httpClient = new HttpClient();

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
                    MessageHelper.Warning("无法获取最新发布版本信息。");
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
                    // 比较版本号
                    // 如果最新版本号大于当前版本号，则有更新
                    if(latestVersion == currentVersion)
                    {
                        MessageHelper.Info("当前已是最新版本");
                        return false;
                    }
                    return latestVersion > currentVersion;
                }
                else
                {
                    MessageHelper.Warning("无法解析版本号。请前往发布地址查看");
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                // 网络请求错误
                MessageHelper.Error($"网络请求错误: {ex.Message}");
                return false;
            }
            catch (JsonException ex)
            {
                // JSON 解析错误
                MessageHelper.Error($"JSON 解析错误: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // 其他未知错误
                MessageHelper.Error($"发生未知错误: {ex.Message}");
                return false;
            }
        }
    }
}
