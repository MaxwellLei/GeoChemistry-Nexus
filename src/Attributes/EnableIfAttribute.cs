using System;

namespace GeoChemistryNexus.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class EnableIfAttribute : Attribute
    {
        public string PropertyName { get; }
        public object? Value { get; }
        public bool Inverse { get; set; }

        public EnableIfAttribute(string propertyName, object? value)
        {
            PropertyName = propertyName;
            Value = value;
        }

        public EnableIfAttribute(string propertyName, bool value)
        {
            PropertyName = propertyName;
            Value = value;
        }
    }
}
