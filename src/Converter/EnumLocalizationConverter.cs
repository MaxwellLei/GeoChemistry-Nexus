using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Converter
{
    public class EnumLocalizationConverter : EnumConverter
    {
        public EnumLocalizationConverter(Type type) : base(type)
        {
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // 将枚举值转换为字符串
            if (destinationType == typeof(string) && value != null)
            {
                FieldInfo fi = value.GetType().GetField(value.ToString());
                if (fi != null)
                {
                    // 获取自定义的 LocalizedDescriptionAttribute
                    var attributes = (LocalizedDescriptionAttribute[])fi.GetCustomAttributes(typeof(LocalizedDescriptionAttribute), false);

                    // 如果找到了该特性，就返回它的 Description
                    // 否则，执行基类的默认行为（即显示枚举名）
                    return ((attributes.Length > 0) && (!string.IsNullOrEmpty(attributes[0].Description)))
                           ? attributes[0].Description
                           : value.ToString();
                }
            }

            // 对于所有其他类型的转换，都使用基类的默认实现
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
