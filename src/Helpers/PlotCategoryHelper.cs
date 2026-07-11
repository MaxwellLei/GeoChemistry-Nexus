using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    public static class PlotCategoryHelper
    {
        public static string LocalConfigPath =>
            FileHelper.GetDataPath("PlotData", OfficialContentEndpoints.PlotTemplateCategoriesFileName);

        public static string PublisherConfigPath =>
            AppDataPathHelper.GetDataPath("Config", "PlotTemplateCategories.publisher.json");

        /// <summary>
        /// 发布器工作副本：优先本地草稿，其次运行时目录文件。
        /// </summary>
        public static PlotTemplateCategoryConfig LoadPublisherConfig()
        {
            return PublisherConfigHelper.LoadPublisherConfig<PlotTemplateCategoryConfig>(
                PublisherConfigPath,
                LocalConfigPath,
                HasContent);
        }

        public static void SavePublisherConfig(PlotTemplateCategoryConfig config)
        {
            PublisherConfigHelper.SavePublisherConfig(config, PublisherConfigPath);
        }

        /// <summary>
        /// 导出/发布时使用的分类文件路径。
        /// </summary>
        public static string ResolveExportConfigPath()
        {
            return PublisherConfigHelper.ResolveExportConfigPath(PublisherConfigPath, LocalConfigPath);
        }

        public static PlotTemplateCategoryConfig LoadConfig()
        {
            return LoadConfigFromPath(LocalConfigPath);
        }

        public static PlotTemplateCategoryConfig LoadConfigFromPath(string path)
        {
            return PublisherConfigHelper.LoadFromPath<PlotTemplateCategoryConfig>(path);
        }

        public static void SaveConfig(PlotTemplateCategoryConfig config)
        {
            SaveConfig(config, LocalConfigPath);
        }

        public static void SaveConfig(PlotTemplateCategoryConfig config, string path)
        {
            PublisherConfigHelper.SaveToPath(config, path);
        }

        public static string GetName(Dictionary<string, string> names)
        {
            if (names == null) return "";

            return AppCultureRegistry.GetLocalizedValue(
                names,
                LanguageService.CurrentLanguage,
                AppCultureRegistry.DefaultAppLanguage);
        }

        private static bool HasContent(PlotTemplateCategoryConfig config)
        {
            return config != null && config.Values.Any(list => list != null && list.Count > 0);
        }
    }
}
