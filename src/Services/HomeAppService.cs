using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System.Collections.Generic;

namespace GeoChemistryNexus.Services
{
    public static class HomeAppService
    {
        public static List<HomeAppItem> GetAvailableWidgets()
        {
            bool isDeveloperMode = bool.TryParse(ConfigHelper.GetConfig("developer_mode"), out bool devMode) && devMode;

            var widgets = new List<HomeAppItem>
            {
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["template_translator"],
                    Description = LanguageService.Instance["edit_drawing_template_translations"],
                    WidgetKey = "TemplateTranslatorWidget",
                    Icon = "\ue8c1"
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["server_announcements"],
                    Description = LanguageService.Instance["view_latest_server_announcements"],
                    WidgetKey = "AnnouncementWidget",
                    Icon = "\ue789"
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["alkalinity_calculator"],
                    Description = LanguageService.Instance["alkalinity_calculator_desc"],
                    WidgetKey = "AlkalinityCalculatorWidget",
                    Icon = "\ue8ef"
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["hardness_calculator"],
                    Description = LanguageService.Instance["hardness_calculator_desc"],
                    WidgetKey = "HardnessCalculatorWidget",
                    Icon = "\ue9ca"
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["black_body_radiation_calculator"],
                    Description = LanguageService.Instance["black_body_radiation_calculator_desc"],
                    WidgetKey = "BlackBodyRadiationCalculatorWidget",
                    Icon = "\ue706"
                }
            };

            if (isDeveloperMode)
            {
                widgets.Insert(1, new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["official_template_publisher"],
                    Description = LanguageService.Instance["official_template_publisher_desc"],
                    WidgetKey = "OfficialTemplatePublisherWidget",
                    Icon = "\ue898"
                });
            }

            return widgets;
        }
    }
}
