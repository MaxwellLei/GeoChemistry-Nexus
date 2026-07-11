using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace GeoChemistryNexus.Helpers
{
    internal static class StartPicHelper
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png" };

        /// <summary>
        /// 官方自带启动图文件名（与 csproj 中 Content 一致），不允许删除。
        /// </summary>
        private static readonly HashSet<string> BuiltInFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "1.jpg",
            "2.jpg",
            "3.jpg",
            "4.jpg"
        };

        public static string FolderPath => Path.Combine(
            Environment.CurrentDirectory, "Data", "Image", "StartPic");

        public static void EnsureFolderExists()
        {
            Directory.CreateDirectory(FolderPath);
        }

        public static string[] GetImageFiles()
        {
            EnsureFolderExists();
            return Directory.GetFiles(FolderPath)
                .Where(file => ImageExtensions.Contains(
                    Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderBy(file => IsBuiltInImage(file) ? 0 : 1)
                .ThenBy(file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static bool IsBuiltInImage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            return BuiltInFileNames.Contains(Path.GetFileName(filePath));
        }

        public static bool IsCustomImage(string filePath) => !IsBuiltInImage(filePath);

        /// <summary>
        /// 将图片完整读入内存后再解码，避免 BitmapImage/BitmapFrame 按 URI 加载时锁定磁盘文件。
        /// </summary>
        public static BitmapImage? LoadBitmapWithoutFileLock(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                var image = new BitmapImage();
                using (var stream = new MemoryStream(bytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                }
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        public static bool TryDeleteCustomImage(string filePath, out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "Invalid path";
                return false;
            }

            if (IsBuiltInImage(filePath))
            {
                errorMessage = "Built-in image cannot be deleted";
                return false;
            }

            if (!File.Exists(filePath))
                return true;

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
