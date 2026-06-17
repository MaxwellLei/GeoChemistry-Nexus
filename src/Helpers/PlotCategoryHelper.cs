using GeoChemistryNexus.Models;

using System.Collections.Generic;

using System.IO;

using System.Text.Json;

using System.Linq;

using GeoChemistryNexus.Services;



namespace GeoChemistryNexus.Helpers

{

    public static class PlotCategoryHelper

    {

        private static readonly JsonSerializerOptions JsonOptions = new()

        {

            PropertyNameCaseInsensitive = true,

            WriteIndented = true

        };



        public static string LocalConfigPath =>

            Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", OfficialContentEndpoints.PlotTemplateCategoriesFileName);



        public static PlotTemplateCategoryConfig LoadConfig()

        {

            return LoadConfigFromPath(LocalConfigPath);

        }



        public static PlotTemplateCategoryConfig LoadConfigFromPath(string path)

        {

            if (!File.Exists(path))

                return new PlotTemplateCategoryConfig();



            try

            {

                string json = File.ReadAllText(path);

                return JsonSerializer.Deserialize<PlotTemplateCategoryConfig>(json, JsonOptions)

                       ?? new PlotTemplateCategoryConfig();

            }

            catch

            {

                return new PlotTemplateCategoryConfig();

            }

        }



        public static void SaveConfig(PlotTemplateCategoryConfig config)

        {

            SaveConfig(config, LocalConfigPath);

        }



        public static void SaveConfig(PlotTemplateCategoryConfig config, string path)

        {

            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))

                Directory.CreateDirectory(directory);



            File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions));

        }

        

        public static string GetName(Dictionary<string, string> names)

        {

             if (names == null) return "";



             return AppCultureRegistry.GetLocalizedValue(

                 names,

                 LanguageService.CurrentLanguage,

                 AppCultureRegistry.DefaultAppLanguage);

        }

    }

}


