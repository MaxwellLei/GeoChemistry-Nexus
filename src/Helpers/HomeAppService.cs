using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    public class HomeAppService
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Config", "home_app.json");

        public static List<HomeAppItem> LoadApps()
        {
            try
            {
                string json = JsonHelper.ReadJsonFile(ConfigPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<HomeAppItem>();
                }
                
                var apps = JsonHelper.Deserialize<List<HomeAppItem>>(json);
                return apps ?? new List<HomeAppItem>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading home apps: {ex.Message}");
                return new List<HomeAppItem>();
            }
        }

        public static void SaveApps(IEnumerable<HomeAppItem> apps)
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonHelper.Serialize(apps);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving home apps: {ex.Message}");
            }
        }

        // Available widgets catalog
        public static List<HomeAppItem> GetAvailableWidgets()
        {
            return new List<HomeAppItem>
            {
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = "模板翻译器",
                    Description = "编辑绘图模板的多语言翻译",
                    WidgetKey = "TemplateTranslatorWidget",
                    Icon = "\ue8c1"
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = "模板修复工具",
                    Description = "修复丢失或损坏的绘图模板及列表",
                    WidgetKey = "TemplateRepairWidget",
                    Icon = "\ue82f" 
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = "服务器公告",
                    Description = "查看服务器发布的最新公告信息",
                    WidgetKey = "AnnouncementWidget",
                    Icon = "\ue789" // Info or Announcement icon
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = "开发者工具",
                    Description = "维护服务器配置与模板文件",
                    WidgetKey = "DeveloperToolWidget",
                    Icon = "\ue90f" // Tool/Wrench icon
                }
            };
        }
    }
}
