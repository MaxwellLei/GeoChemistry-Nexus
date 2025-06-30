using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedCategoryAttribute : CategoryAttribute
    {
        // 重写属性，动态获取值
        protected override string GetLocalizedString(string value)
        {
            string localizedValue = LanguageService.Instance[value];
            // 如果找不到资源，返回键名本身
            return string.IsNullOrEmpty(localizedValue) ? $"[[{value}]]" : localizedValue;
        }

        public LocalizedCategoryAttribute(string resourceKey) : base(resourceKey)
        {
        }
    }
}
