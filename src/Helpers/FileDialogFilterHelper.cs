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

        public static string TemplateFiles =>
            FormatFilter("filter_template_files", "Template Files", "*.json;*.zip", "*.json;*.zip");

        public static string OpenTemplate =>
            $"{TemplateFiles}|{AllFiles}";

        public static string ImportTemplates =>
            $"{L("filter_template_files", "Template Files")} (*.zip;*.json)|*.zip;*.json|{ZipFiles}|{JsonFiles}|{AllFiles}";

        public static string JsonOrAll =>
            $"{JsonFiles}|{AllFiles}";

        public static string JsonOnly =>
            JsonFiles;

        public static string PngSvg =>
            $"{FormatFilter("filter_png_image", "PNG Image", "*.png", "*.png")}|{FormatFilter("filter_svg_vector", "SVG Vector", "*.svg", "*.svg")}";
    }
}
