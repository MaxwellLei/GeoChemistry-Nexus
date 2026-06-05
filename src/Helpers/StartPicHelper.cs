using System;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    internal static class StartPicHelper
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png" };

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
                .ToArray();
        }
    }
}
