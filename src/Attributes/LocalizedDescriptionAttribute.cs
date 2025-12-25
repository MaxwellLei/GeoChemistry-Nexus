using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly string _resourceKey;

        public LocalizedDescriptionAttribute(string resourceKey)
        {
            _resourceKey = resourceKey;
        }

        // 重写 Description 属性，动态获取值
        public override string Description
        {
            get
            {
                return LanguageService.Instance[_resourceKey];
            }
        }
    }
}
