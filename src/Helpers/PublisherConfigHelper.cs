using System;
using System.IO;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 发布器草稿配置的通用读写：优先 publisher 草稿，其次运行时本地文件。
    /// </summary>
    public static class PublisherConfigHelper
    {
        public static T LoadPublisherConfig<T>(
            string publisherPath,
            string localPath,
            Func<T, bool>? useDraftWhen = null) where T : class, new()
        {
            if (File.Exists(publisherPath))
            {
                var draft = JsonHelper.LoadFromFileOrNew<T>(publisherPath);
                if (useDraftWhen == null || useDraftWhen(draft))
                    return draft;
            }

            return JsonHelper.LoadFromFileOrNew<T>(localPath);
        }

        public static void SavePublisherConfig<T>(T config, string publisherPath)
        {
            JsonHelper.SerializeToJsonFile(config, publisherPath);
        }

        public static string ResolveExportConfigPath(string publisherPath, string localPath)
        {
            return File.Exists(publisherPath) ? publisherPath : localPath;
        }

        public static T LoadFromPath<T>(string path) where T : class, new()
        {
            return JsonHelper.LoadFromFileOrNew<T>(path);
        }

        public static void SaveToPath<T>(T config, string path)
        {
            JsonHelper.SerializeToJsonFile(config, path);
        }
    }
}
