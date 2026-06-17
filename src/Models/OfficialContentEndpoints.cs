namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 官方图解内容 CDN / COS 端点常量
    /// </summary>
    public static class OfficialContentEndpoints
    {
        public const string CosBaseUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com";

        public const string ServerInfoUrl = CosBaseUrl + "/server_info.json";
        public const string HomeLinksCatalogUrl = CosBaseUrl + "/HomeLinksCatalog.json";
        public const string GraphMapListUrl = CosBaseUrl + "/GraphMapList.json";
        public const string PlotTemplateCategoriesUrl = CosBaseUrl + "/PlotTemplateCategories.json";

        public const string GraphMapListFileName = "GraphMapList.json";
        public const string PlotTemplateCategoriesFileName = "PlotTemplateCategories.json";
        public const string ServerInfoFileName = "server_info.json";
        public const string HomeLinksCatalogFileName = "HomeLinksCatalog.json";
        public const string PublishManifestFileName = "publish_manifest.json";
        public const string TemplatesFolderName = "Templates";

        public const string DefaultRegion = "ap-hongkong";
        public const string DefaultBucket = "geochemistrynexus-1303234197";

        public const string GeothermometerFolderName = "Geothermometer";
        public const string GeothermometerBaseUrl = CosBaseUrl + "/" + GeothermometerFolderName;
        public const string GeoTListUrl = GeothermometerBaseUrl + "/GeoT-List.json";
        public const string GeoTIndexUrl = GeothermometerBaseUrl + "/GeoT-index.json";
        public const string GeoTMineralCategoriesUrl = GeothermometerBaseUrl + "/GeoTMineralCategories.json";
        public const string GeoTListFileName = "GeoT-List.json";
        public const string GeoTIndexFileName = "GeoT-index.json";
        public const string GeoTMineralCategoriesFileName = "GeoTMineralCategories.json";
        public const string GeothermometerPublishManifestFileName = "geothermometer_publish_manifest.json";
    }
}
