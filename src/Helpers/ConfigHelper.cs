using System;
using System.Collections.Generic;
using System.Configuration;

namespace GeoChemistryNexus.Helpers
{
    class ConfigHelper
    {
        private static Configuration _mappedConfig;

        private static Configuration GetConfiguration()
        {
            if (_mappedConfig != null)
                return _mappedConfig;

            AppDataPathHelper.Initialize();

            string configPath = AppDataPathHelper.GetUserConfigPath();
            var map = new ExeConfigurationFileMap
            {
                ExeConfigFilename = configPath
            };
            _mappedConfig = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            return _mappedConfig;
        }

        public static string GetConfig(string key)
        {
            return GetConfiguration().AppSettings.Settings[key]?.Value;
        }

        public static void SetConfig(string key, string value)
        {
            Configuration config = GetConfiguration();
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static void SetConfigs(Dictionary<string, string> settings)
        {
            if (settings == null || settings.Count == 0)
                return;

            Configuration config = GetConfiguration();
            foreach (var item in settings)
            {
                if (config.AppSettings.Settings[item.Key] == null)
                {
                    config.AppSettings.Settings.Add(item.Key, item.Value);
                }
                else
                {
                    config.AppSettings.Settings[item.Key].Value = item.Value;
                }
            }

            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
