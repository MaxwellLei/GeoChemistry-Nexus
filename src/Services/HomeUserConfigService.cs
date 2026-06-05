using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Services
{
    public static class HomeUserConfigService
    {
        private static readonly string UserConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data", "Config", "home_user.json");

        private static readonly string[] DeprecatedWidgetKeys =
        {
            "CalendarWidget",
            "SystemInfoWidget",
            "DeveloperToolWidget"
        };

        public static HomeUserConfig Load()
        {
            try
            {
                string json = JsonHelper.ReadJsonFile(UserConfigPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new HomeUserConfig();

                var config = JsonHelper.Deserialize<HomeUserConfig>(json);
                if (config == null)
                    return new HomeUserConfig();

                config.PersonalLinks ??= new List<HomeAppItem>();
                config.Widgets ??= new List<HomeAppItem>();

                config.Widgets = config.Widgets
                    .Where(w => w.Type == HomeAppType.Widget
                                && !DeprecatedWidgetKeys.Any(k => string.Equals(w.WidgetKey, k, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                return config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeUserConfigService] Load failed: {ex.Message}");
                return new HomeUserConfig();
            }
        }

        public static void Save(HomeUserConfig config)
        {
            if (config == null)
                return;

            try
            {
                string dir = Path.GetDirectoryName(UserConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonHelper.Serialize(config);
                File.WriteAllText(UserConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeUserConfigService] Save failed: {ex.Message}");
            }
        }

        public static void SavePersonalLinks(IEnumerable<HomeAppItem> links)
        {
            var config = Load();
            config.PersonalLinks = links?.ToList() ?? new List<HomeAppItem>();
            Save(config);
        }

        public static void SaveWidgets(IEnumerable<HomeAppItem> widgets)
        {
            var config = Load();
            config.Widgets = widgets?.ToList() ?? new List<HomeAppItem>();
            Save(config);
        }
    }
}
