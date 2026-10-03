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
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["periodic_table_widget"],
                    Description = LanguageService.Instance["periodic_table_widget_desc"],
                    WidgetKey = "PeriodicTableWidget",
                    Icon = "\ue81e"
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["chronostrat_navigator_widget"],
                    Description = LanguageService.Instance["chronostrat_navigator_widget_desc"],
                    WidgetKey = "ChronostratNavigatorWidget",
                    Icon = "\ue929"
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["oxide_element_converter"],
                    Description = LanguageService.Instance["oxide_element_converter_desc"],
                    WidgetKey = "OxideElementConverterWidget",
                    Icon = "\ue9d5"
                },
                new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = LanguageService.Instance["geoscience_unit_converter"],
                    Description = LanguageService.Instance["geoscience_unit_converter_desc"],
                    WidgetKey = "GeoscienceUnitConverterWidget",
                    Icon = "\ue94e"
                }
            };

            if (isDeveloperMode)
            {
                widgets.Insert(0, new HomeAppItem
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
