using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Models;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace GeoChemistryNexus.PropertyEditor
{
    public class PointDefinitionPropertyEditor : PropertyEditorBase
    {
        // 重写此方法，返回我们自定义的编辑器UI实例
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            return new PointDefinitionControl();
        }

        // 重写此方法，告诉PropertyGrid应该将数据绑定到我们自定义控件的哪个依赖属性上
        public override DependencyProperty GetDependencyProperty()
        {
            return PointDefinitionControl.PointValueProperty;
        }
    }
}
