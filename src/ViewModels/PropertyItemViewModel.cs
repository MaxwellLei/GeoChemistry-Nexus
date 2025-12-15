using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GeoChemistryNexus.ViewModels
{
    public partial class PropertyItemViewModel : ObservableObject
    {
        private readonly object _target;
        public PropertyInfo PropertyInfo { get; }

        [ObservableProperty]
        private string _displayName;

        [ObservableProperty]
        private string _category;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private int _order;

        [ObservableProperty]
        private PropertyLayout _layout = PropertyLayout.Horizontal;

        public Type PropertyType => PropertyInfo.PropertyType;

        // Custom Editor Type Name if specified
        public string? EditorTypeName { get; private set; }

        public PropertyItemViewModel(object target, PropertyInfo propertyInfo)
        {
            _target = target;
            PropertyInfo = propertyInfo;

            // Initialize DisplayName
            var displayNameAttr = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
            DisplayName = displayNameAttr?.DisplayName ?? propertyInfo.Name;

            // Initialize Category
            var categoryAttr = propertyInfo.GetCustomAttribute<CategoryAttribute>();
            Category = categoryAttr?.Category ?? "Misc";

            // Initialize Description
            var descriptionAttr = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
            Description = descriptionAttr?.Description ?? "";

            // Initialize Order
            var orderAttr = propertyInfo.GetCustomAttribute<PropertyOrderAttribute>();
            Order = orderAttr?.Order ?? int.MaxValue;

            // Initialize Layout
            var layoutAttr = propertyInfo.GetCustomAttribute<PropertyLayoutAttribute>();
            if (layoutAttr != null)
            {
                Layout = layoutAttr.Layout;
            }
            
            // Initialize Editor
            var editorAttr = propertyInfo.GetCustomAttribute<EditorAttribute>();
            if (editorAttr != null)
            {
                EditorTypeName = editorAttr.EditorTypeName;
            }

            // Initial Enable Check
            CheckEnableConditions();

            // Listen for changes
            if (_target is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged += OnTargetPropertyChanged;
            }
        }

        private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyInfo.Name)
            {
                OnPropertyChanged(nameof(Value));
            }

            // Check if the changed property affects our enabled state
            CheckEnableConditions();
        }

        private void CheckEnableConditions()
        {
            var enableAttrs = PropertyInfo.GetCustomAttributes<EnableIfAttribute>();
            if (!enableAttrs.Any())
            {
                IsEnabled = true;
                return;
            }

            bool enabled = true;
            foreach (var attr in enableAttrs)
            {
                var prop = _target.GetType().GetProperty(attr.PropertyName);
                if (prop != null)
                {
                    var val = prop.GetValue(_target);
                    
                    // Simple equality check
                    bool matches = false;
                    if (val == null && attr.Value == null) matches = true;
                    else if (val != null && attr.Value != null) matches = val.Equals(attr.Value);
                    
                    if (attr.Inverse) matches = !matches;

                    if (!matches)
                    {
                        enabled = false;
                        break;
                    }
                }
            }
            IsEnabled = enabled;
        }

        public object Value
        {
            get => PropertyInfo.GetValue(_target);
            set
            {
                object newValue = value;
                try
                {
                    // Handle numeric conversions from double (common in UI controls like NumericUpDown)
                    if (value is double d)
                    {
                        if (PropertyType == typeof(int)) newValue = Convert.ToInt32(d);
                        else if (PropertyType == typeof(float)) newValue = Convert.ToSingle(d);
                        else if (PropertyType == typeof(short)) newValue = Convert.ToInt16(d);
                        else if (PropertyType == typeof(long)) newValue = Convert.ToInt64(d);
                        else if (PropertyType == typeof(byte)) newValue = Convert.ToByte(d);
                    }
                }
                catch
                {
                    // If conversion fails, keep original value
                }

                if (!Equals(PropertyInfo.GetValue(_target), newValue))
                {
                    PropertyInfo.SetValue(_target, newValue);
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
    }
}
