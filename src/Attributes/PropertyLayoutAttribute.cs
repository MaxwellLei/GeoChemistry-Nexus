using System;

namespace GeoChemistryNexus.Attributes
{
    public enum PropertyLayout
    {
        Horizontal, // 默认：标签在左，编辑器在右
        Vertical    // 标签在上，编辑器在下（占满整宽）
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyLayoutAttribute : Attribute
    {
        public PropertyLayout Layout { get; }

        public PropertyLayoutAttribute(PropertyLayout layout)
        {
            Layout = layout;
        }
    }
}
