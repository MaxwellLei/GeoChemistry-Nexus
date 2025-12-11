using GeoChemistryNexus.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    public static class PlotCategoryHelper
    {
        public static PlotTemplateCategoryConfig LoadConfig()
        {
            string path = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotTemplateCategories.json");
            if (!File.Exists(path)) return new PlotTemplateCategoryConfig();

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<PlotTemplateCategoryConfig>(json, options) ?? new PlotTemplateCategoryConfig();
            }
            catch
            {
                return new PlotTemplateCategoryConfig();
            }
        }
        
        public static string GetName(Dictionary<string, string> names)
        {
             if (names == null) return "";
             
             string currentLang = LanguageService.CurrentLanguage;
             
             if (names.TryGetValue(currentLang, out string name)) return name;
             if (names.TryGetValue("en-US", out name)) return name;
             return names.Values.FirstOrDefault() ?? "";
        }
    }
}
