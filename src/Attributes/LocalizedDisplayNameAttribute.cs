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
    /// <summary>
    /// 自定义 DisplayNameAttribute，
    /// 通过 LanguageService 动态获取本地化的字符串
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly string _resourceKey;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceKey">在 .resx 资源文件中定义的资源键</param>
        public LocalizedDisplayNameAttribute(string resourceKey)
        {
            _resourceKey = resourceKey;
        }

        /// <summary>
        /// 重写属性，实现动态查找
        /// </summary>
        public override string DisplayName
        {
            get
            {
                string displayName = LanguageService.Instance[_resourceKey];

                return string.IsNullOrEmpty(displayName) ? $"[[{_resourceKey}]]" : displayName;
            }
        }
    }
}
