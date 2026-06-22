using GeoChemistryNexus.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 统一管理可写数据根目录：便携版 / 开发调试 / 安装版（LocalAppData）。
    /// </summary>
    public static class AppDataPathHelper
    {
        public const string PortableFlagFileName = "portable.flag";
        private const string AppDataFolderName = "GeoChemistryNexus";

        private static string _dataRoot;
        private static bool _initialized;

        public static string GetAppDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static bool IsPortableMode()
        {
            return File.Exists(Path.Combine(GetAppDirectory(), PortableFlagFileName));
        }

        public static bool IsDevMode()
        {
#if DEBUG
            return true;
#else
            string baseDir = GetAppDirectory();
            return baseDir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}Debug", StringComparison.OrdinalIgnoreCase)
                || baseDir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}Release", StringComparison.OrdinalIgnoreCase);
#endif
        }

        public static string GetDataRoot()
        {
            if (!string.IsNullOrEmpty(_dataRoot))
                return _dataRoot;

            if (IsPortableMode() || IsDevMode())
            {
                _dataRoot = Path.Combine(GetAppDirectory(), "Data");
            }
            else
            {
                _dataRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppDataFolderName);
            }

            return _dataRoot;
        }

        public static string GetDataPath(params string[] segments)
        {
            if (segments == null || segments.Length == 0)
                return GetDataRoot();

            return Path.Combine(new[] { GetDataRoot() }.Concat(segments).ToArray());
        }

        public static string GetUserConfigPath()
        {
            return GetDataPath("Config", "App.config");
        }

        public static string GetLogsPath()
        {
            return GetDataPath("Logs");
        }

        public static string GetBundledDataPath(params string[] segments)
        {
            return Path.Combine(new[] { GetAppDirectory(), "Data" }.Concat(segments).ToArray());
        }

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            EnsureDataStructure();
            SeedDefaultFiles();
        }

        public static void EnsureDataStructure()
        {
            foreach (string dir in new[]
            {
                GetDataPath("Config"),
                GetDataPath("PlotData"),
                GetDataPath("Plugins"),
                GetLogsPath()
            })
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
        }

        private static void SeedDefaultFiles()
        {
            SeedFileIfMissing(
                GetBundledDataPath("PlotData", OfficialContentEndpoints.PlotTemplateCategoriesFileName),
                GetDataPath("PlotData", OfficialContentEndpoints.PlotTemplateCategoriesFileName));

            SeedFileIfMissing(
                GetBundledDataPath("Plugins", OfficialContentEndpoints.GeoTMineralCategoriesFileName),
                GetDataPath("Plugins", OfficialContentEndpoints.GeoTMineralCategoriesFileName));

            string defaultDllConfig = Path.Combine(GetAppDirectory(), "GeoChemistryNexus.dll.config");
            SeedFileIfMissing(defaultDllConfig, GetUserConfigPath());
        }

        private static void SeedFileIfMissing(string sourcePath, string targetPath)
        {
            try
            {
                if (File.Exists(targetPath) || !File.Exists(sourcePath))
                    return;

                string targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                File.Copy(sourcePath, targetPath, overwrite: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppDataPathHelper] Seed failed ({targetPath}): {ex.Message}");
            }
        }
    }
}
