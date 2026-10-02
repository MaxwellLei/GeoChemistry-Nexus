using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Helpers
{
    public static class FileDialogFilterHelper
    {
        private static string L(string key, string fallback) =>
            LanguageService.Instance[key] ?? fallback;

        private static string FormatFilter(string descriptionKey, string fallback, string extensions, string pattern) =>
            $"{L(descriptionKey, fallback)} ({extensions})|{pattern}";

        public static string AllFiles =>
            FormatFilter("filter_all_files", "All Files", "*.*", "*.*");

        public static string JsonFiles =>
            FormatFilter("filter_json_files", "JSON Files", "*.json", "*.json");

        public static string ZipFiles =>
            FormatFilter("filter_zip_files", "Zip Files", "*.zip", "*.zip");

        public static string CsvFiles =>
            FormatFilter("filter_csv_files", "CSV Files", "*.csv", "*.csv");

        /// <summary>图解用户导出包（.gndiag）。</summary>
        public static string DiagramPackageFiles =>
            FormatFilter(
                "filter_diagram_package",
                "Diagram Template Package",
                $"*{TemplatePackageFileExtensions.DiagramPrimary}",
                $"*{TemplatePackageFileExtensions.DiagramPrimary}");

        /// <summary>温压计用户导出包（.gngtm）。</summary>
        public static string GeothermometerPackageFiles =>
            FormatFilter(
                "filter_geothermometer_package",
                "Geothermometer Package",
                $"*{TemplatePackageFileExtensions.GeothermometerPrimary}",
                $"*{TemplatePackageFileExtensions.GeothermometerPrimary}");

        /// <summary>图解打开/导入可选文件（新后缀 + zip + json）。</summary>
        public static string TemplateFiles =>
            FormatFilter(
                "filter_template_files",
                "Template Files",
                $"*{TemplatePackageFileExtensions.DiagramPrimary};*.zip;*.json",
                $"*{TemplatePackageFileExtensions.DiagramPrimary};*.zip;*.json");

        public static string OpenTemplate =>
            $"{TemplateFiles}|{DiagramPackageFiles}|{ZipFiles}|{JsonFiles}|{AllFiles}";

        public static string ImportTemplates =>
            $"{TemplateFiles}|{DiagramPackageFiles}|{ZipFiles}|{JsonFiles}|{AllFiles}";

        /// <summary>温压计导入（.gngtm + 兼容 .zip）。</summary>
        public static string ImportGeothermometerPackages =>
            FormatFilter(
                "filter_geothermometer_import",
                "Geothermometer Packages",
                $"*{TemplatePackageFileExtensions.GeothermometerPrimary};*.zip",
                $"*{TemplatePackageFileExtensions.GeothermometerPrimary};*.zip")
            + $"|{GeothermometerPackageFiles}|{ZipFiles}|{AllFiles}";

        public static string JsonOrAll =>
            $"{JsonFiles}|{AllFiles}";

        public static string JsonOnly =>
            JsonFiles;

        public static string PngSvg =>
            $"{FormatFilter("filter_png_image", "PNG Image", "*.png", "*.png")}|{FormatFilter("filter_svg_vector", "SVG Vector", "*.svg", "*.svg")}";
    }
}
