using GeoChemistryNexus.Controls;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.PropertyEditor
{
    public class VerticesPropertyEditor : PropertyEditorBase
    {
        // 重写此方法，返回我们为顶点列表编辑器自定义的UI实例
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            return new VerticesEditorControl();
        }

        // 重写此方法，告诉PropertyGrid应该将数据绑定到我们自定义控件的哪个依赖属性上
        public override DependencyProperty GetDependencyProperty()
        {
            return VerticesEditorControl.VerticesValueProperty;
        }
    }
}
