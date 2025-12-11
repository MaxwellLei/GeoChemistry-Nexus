using GeoChemistryNexus.Controls;
using HandyControl.Controls;
using System.Windows;

namespace GeoChemistryNexus.PropertyEditor
{
    public class ScriptDefinitionPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            return new ScriptDefinitionControl();
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return ScriptDefinitionControl.ScriptDefinitionProperty;
        }
    }
}
