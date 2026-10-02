using System;
using System.Globalization;
using System.Windows.Data;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Converter
{
    /// <summary>
    /// 将 TemplateCardSizePreset 转为本地化显示文本。
    /// </summary>
    public class TemplateCardSizePresetToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TemplateCardSizePreset preset)
                return string.Empty;

            string key = preset switch
            {
                TemplateCardSizePreset.Compact => "template_card_size_compact",
                _ => "template_card_size_standard"
            };

            return LanguageService.Instance[key]
                   ?? LanguageService.GetString(key, preset.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
