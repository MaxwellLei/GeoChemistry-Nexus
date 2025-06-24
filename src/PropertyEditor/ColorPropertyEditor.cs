using GeoChemistryNexus.Controls;
using HandyControl.Controls;
using System.Windows;

namespace GeoChemistryNexus.PropertyEditor
{
    public class ColorPropertyEditor : PropertyEditorBase
    {
        // 重写对应的控件构建类，用于返回UI需要显示的控件实例
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var modernColorPicker = new ModernColorPicker();
            return modernColorPicker;
        }

        // 设置对应实体属性与控件关联的依赖属性
        public override DependencyProperty GetDependencyProperty()
        {
            return ModernColorPicker.SelectedColorProperty;
        }
    }
}
