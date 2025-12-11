using System;

namespace GeoChemistryNexus.Attributes
{
    public enum PropertyLayout
    {
        Horizontal, // Default: Label left, Editor right
        Vertical    // Label top, Editor bottom (full width)
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
