using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    class ConfigHelper
    {
        //private static string configPath = FileHelper.GetFilePath();  //配置文件路径


        //获取配置文件中的值
        //传入 key 返回 value
        public static string GetConfig(string key)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            return ConfigurationManager.AppSettings[key];
            //return configuration.AppSettings.Settings[key].Value;
        }

        //设置配置文件中的值
        //传入 key value 写入配置文件并刷新配置文件
        public static void SetConfig(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
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
    }
}
