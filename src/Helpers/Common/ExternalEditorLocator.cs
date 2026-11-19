using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 第三方矢量编辑程序（CorelDRAW / Inkscape / Adobe Illustrator）的自动检测。
    /// 查找顺序：PATH → “App Paths” 注册表 → 常见安装目录。
    /// 检测到且尚未配置（或原路径已失效）时，可自动把路径写入配置。
    /// </summary>
    public static class ExternalEditorLocator
    {
        public const string CorelDrawId = "CorelDRAW";
        public const string InkscapeId = "Inkscape";
        public const string IllustratorId = "Adobe Illustrator";

        private sealed record EditorSpec(string Id, string ConfigKey, string ExeName, string[] InstallPatterns);

        // InstallPatterns 相对于各安装根目录（Program Files、Program Files (x86)、%LocalAppData%\Programs），
        // 目录段中可含通配符，用于匹配带版本号的文件夹。
        private static readonly EditorSpec[] Specs =
        {
            new(CorelDrawId, ConfigKeys.CorelDrawPath, "CorelDRW.exe", new[]
            {
                @"Corel\CorelDRAW Graphics Suite*\Programs64\CorelDRW.exe",
                @"Corel\CorelDRAW Graphics Suite*\Programs\CorelDRW.exe",
            }),
            new(InkscapeId, ConfigKeys.InkscapePath, "inkscape.exe", new[]
            {
                @"Inkscape\bin\inkscape.exe",
                @"Inkscape\inkscape.exe",
            }),
            new(IllustratorId, ConfigKeys.AdobeIllustratorPath, "Illustrator.exe", new[]
            {
                @"Adobe\Adobe Illustrator*\Support Files\Contents\Windows\Illustrator.exe",
            }),
        };

        private static readonly Regex ExePathRegex =
            new(@"^([a-zA-Z]:\\|\\\\).+\.exe$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>路径是否形如 “盘符:\...\xxx.exe” 或网络路径 “\\server\...\xxx.exe”。</summary>
        public static bool IsValidExePath(string? path)
        {
            return !string.IsNullOrWhiteSpace(path) && ExePathRegex.IsMatch(path.Trim());
        }

        /// <summary>
        /// 自动检测指定程序的可执行文件完整路径；未检测到返回 null。
        /// </summary>
        public static string? FindExecutable(string editorId)
        {
            var spec = Specs.FirstOrDefault(s => s.Id == editorId);
            if (spec == null)
                return null;

            try
            {
                return FindInPath(spec.ExeName)
                    ?? FindInAppPaths(spec.ExeName)
                    ?? FindInInstallDirectories(spec.InstallPatterns);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 程序是否可用：已配置路径存在，或能自动检测到。
        /// </summary>
        public static bool IsAvailable(string? configuredPath, string editorId)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
                return true;

            return FindExecutable(editorId) != null;
        }

        /// <summary>
        /// 对每个可检测的外部程序：配置为空或原路径已不存在时，若自动检测到则写入配置。
        /// 已有且有效的用户配置不会被覆盖。
        /// </summary>
        /// <returns>本次自动填入的程序 Id 列表。</returns>
        public static IReadOnlyList<string> EnsureConfiguredPaths()
        {
            var filled = new List<string>();

            foreach (var spec in Specs)
            {
                string? current = ConfigHelper.GetConfig(spec.ConfigKey);
                if (!string.IsNullOrWhiteSpace(current) && File.Exists(current))
                    continue;

                string? found = FindExecutable(spec.Id);
                if (string.IsNullOrEmpty(found))
                    continue;

                ConfigHelper.SetConfig(spec.ConfigKey, found);
                filled.Add(spec.Id);
            }

            return filled;
        }

        private static string? FindInPath(string exeName)
        {
            string? pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVariable))
                return null;

            foreach (string rawDir in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                string dir = rawDir.Trim().Trim('"');
                if (dir.Length == 0)
                    continue;

                try
                {
                    string candidate = Path.Combine(dir, exeName);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch
                {
                    // 非法 PATH 项忽略
                }
            }

            return null;
        }

        private static string? FindInAppPaths(string exeName)
        {
            string subKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}";

            foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            {
                foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                        using var key = baseKey.OpenSubKey(subKey);
                        string? target = (key?.GetValue(null) as string)?.Trim().Trim('"');
                        if (!string.IsNullOrEmpty(target) && File.Exists(target))
                            return target;
                    }
                    catch
                    {
                        // 注册表不可访问时继续尝试下一个位置
                    }
                }
            }

            return null;
        }

        private static string? FindInInstallDirectories(string[] patterns)
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
            }
            .Where(r => !string.IsNullOrEmpty(r) && Directory.Exists(r))
            .Distinct(StringComparer.OrdinalIgnoreCase);

            var matches = new List<string>();
            foreach (string root in roots)
            {
                foreach (string pattern in patterns)
                    matches.AddRange(ExpandPattern(root, pattern.Split('\\')));
            }

            // 多个版本并存时取最近更新的
            return matches
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(p => SafeLastWriteTime(p))
                .FirstOrDefault();
        }

        private static IEnumerable<string> ExpandPattern(string current, string[] segments)
        {
            if (segments.Length == 0)
                yield break;

            string segment = segments[0];
            bool isLast = segments.Length == 1;

            if (isLast)
            {
                string file = Path.Combine(current, segment);
                if (File.Exists(file))
                    yield return file;
                yield break;
            }

            IEnumerable<string> nextDirs;
            try
            {
                nextDirs = segment.Contains('*') || segment.Contains('?')
                    ? Directory.EnumerateDirectories(current, segment).ToList()
                    : Directory.Exists(Path.Combine(current, segment))
                        ? new[] { Path.Combine(current, segment) }
                        : Array.Empty<string>();
            }
            catch
            {
                yield break;
            }

            foreach (string dir in nextDirs)
            {
                foreach (string hit in ExpandPattern(dir, segments.Skip(1).ToArray()))
                    yield return hit;
            }
        }

        private static DateTime SafeLastWriteTime(string path)
        {
            try { return File.GetLastWriteTimeUtc(path); }
            catch { return DateTime.MinValue; }
        }
    }
}
