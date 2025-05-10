using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Helpers
{
    class I18n
    {
        //读取资源字典
        public static string GetString(string key)
        {
            return (string)Application.Current.Resources[key];
        }

        // 根据传入的值返回对应的字符
        public static string GetLanguageFolder(int id)
        {
            if (id == 0) return "zh-Hans";
            return "en-US";
        }
    }
}
