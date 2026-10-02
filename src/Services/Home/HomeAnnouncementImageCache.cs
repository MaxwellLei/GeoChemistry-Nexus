using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 公告背景图的本地缓存。键是图片 URL，断网时仍可显示上次成功下载的图。
    /// </summary>
    public static class HomeAnnouncementImageCache
    {
        private const int MaxCachedBytes = 12 * 1024 * 1024;

        private static readonly object Gate = new();

        /// <summary>
        /// 只接受这一代及之后的写入。主页重建公告后，上一代还在飞的下载不能再改缓存。
        /// </summary>
        private static int _acceptedLoadVersion;

        private static string CacheDirectory => AppDataPathHelper.GetDataPath("Cache", "Announcements");

        public static byte[]? TryRead(string url)
        {
            if (!TryGetCachePath(url, out string path))
                return null;

            lock (Gate)
            {
                try
                {
                    if (!File.Exists(path))
                        return null;

                    byte[] bytes = File.ReadAllBytes(path);
                    return bytes.Length == 0 ? null : bytes;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HomeAnnouncementImageCache] Read failed: {ex.Message}");
                    return null;
                }
            }
        }

        public static void Save(string url, byte[] bytes, int loadVersion)
        {
            if (bytes.Length == 0)
                return;

            if (bytes.Length > MaxCachedBytes)
            {
                Debug.WriteLine($"[HomeAnnouncementImageCache] Skip cache, image is {bytes.Length} bytes.");
                return;
            }

            if (!TryGetCachePath(url, out string path))
                return;

            lock (Gate)
            {
                if (!IsLoadCurrentCore(loadVersion))
                    return;

                try
                {
                    string? dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    string tempPath = path + ".tmp";
                    File.WriteAllBytes(tempPath, bytes);
                    File.Move(tempPath, path, overwrite: true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HomeAnnouncementImageCache] Save failed: {ex.Message}");
                }
            }
        }

        public static void Delete(string url, int loadVersion)
        {
            if (!TryGetCachePath(url, out string path))
                return;

            lock (Gate)
            {
                if (!IsLoadCurrentCore(loadVersion))
                    return;

                TryDelete(path);
                TryDelete(path + ".tmp");
            }
        }

        public static bool IsLoadCurrent(int loadVersion)
        {
            lock (Gate)
                return IsLoadCurrentCore(loadVersion);
        }

        /// <summary>
        /// 删掉当前公告不再引用的背景图，避免换图后旧文件一直留在磁盘上。
        /// </summary>
        public static void PruneExcept(IEnumerable<string> activeUrls, int loadVersion)
        {
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (activeUrls != null)
            {
                foreach (string url in activeUrls)
                {
                    if (TryGetCachePath(url, out string path))
                        keep.Add(path);
                }
            }

            lock (Gate)
            {
                if (loadVersion < _acceptedLoadVersion)
                    return;

                if (loadVersion > _acceptedLoadVersion)
                    _acceptedLoadVersion = loadVersion;

                try
                {
                    if (!Directory.Exists(CacheDirectory))
                        return;

                    foreach (string file in Directory.GetFiles(CacheDirectory))
                    {
                        if (file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) || !keep.Contains(file))
                            TryDelete(file);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HomeAnnouncementImageCache] Prune failed: {ex.Message}");
                }
            }
        }

        private static bool IsLoadCurrentCore(int loadVersion)
            => loadVersion >= _acceptedLoadVersion;

        private static bool TryGetCachePath(string url, out string path)
        {
            path = string.Empty;
            if (!HomeAnnouncementBackground.IsHttpImageUrl(url))
                return false;

            string trimmed = url.Trim();
            string hash = ComputeMd5(trimmed);
            if (string.IsNullOrEmpty(hash))
                return false;

            path = Path.Combine(CacheDirectory, hash + GetExtension(trimmed));
            return true;
        }

        private static string GetExtension(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return ".img";

            string ext = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp"
                ? ext
                : ".img";
        }

        private static string ComputeMd5(string value)
        {
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeAnnouncementImageCache] Delete failed: {ex.Message}");
            }
        }
    }
}
