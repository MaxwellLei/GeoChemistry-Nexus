using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    public class ChemicalHelper
    {
        // 方法：根据氧化物的wt%含量和元素的摩尔质量，计算最终元素的ppm
        public static double ConvertOxideToElementPpm(double oxideWtPercentage, double elementMolarMass, double oxideMolarMass, int elementCount)
        {
            // 计算氧化物中的元素质量百分比
            double elementMassFraction = (elementMolarMass * elementCount) / oxideMolarMass;

            // 计算对应元素的质量
            double elementMass = oxideWtPercentage * elementMassFraction;

            // 将g转换为ppm（假设总质量为1,000,000g）
            double elementPpm = elementMass * 1e6;

            return elementPpm;
        }

        // 标准化
        public static double NormalizeValue(double sampleValue, double referenceValue)
        {
            if (referenceValue == 0) // 避免除以零
            {
                throw new DivideByZeroException("参考值为零，无法进行标准化。");
            }

            return sampleValue / referenceValue;
        }


    }
}
