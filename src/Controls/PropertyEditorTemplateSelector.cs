using GeoChemistryNexus.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public class PropertyEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate NumericTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }
        public DataTemplate ColorTemplate { get; set; }
        public DataTemplate LocalizedStringTemplate { get; set; }
        public DataTemplate PointDefinitionTemplate { get; set; }
        public DataTemplate FontFamilyTemplate { get; set; }
        public DataTemplate ScriptDefinitionTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PropertyItemViewModel vm)
            {
                // Check explicit editor type name
                if (!string.IsNullOrEmpty(vm.EditorTypeName))
                {
                    if (vm.EditorTypeName.Contains("ColorPropertyEditor")) return ColorTemplate;
                    if (vm.EditorTypeName.Contains("PointDefinitionPropertyEditor")) return PointDefinitionTemplate;
                    if (vm.EditorTypeName.Contains("LocalizedStringPropertyEditor")) return LocalizedStringTemplate;
                    if (vm.EditorTypeName.Contains("FontFamilyPropertyEditor")) return FontFamilyTemplate;
                    if (vm.EditorTypeName.Contains("ScriptDefinitionPropertyEditor")) return ScriptDefinitionTemplate;
                }

                // Check by type
                if (vm.PropertyType == typeof(bool)) return BoolTemplate;
                if (vm.PropertyType == typeof(int) || vm.PropertyType == typeof(float) || vm.PropertyType == typeof(double)) return NumericTemplate;
                if (vm.PropertyType.IsEnum) return EnumTemplate;
                
                // Default
                return StringTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
