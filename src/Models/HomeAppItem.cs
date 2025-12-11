using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public enum HomeAppType
    {
        WebLink,
        Widget
    }

    // Base class for polymorphic deserialization if needed, 
    // but for simple JSON, we might just use a single class or custom converter.
    // Given the simple requirements, a single class might suffice if properties are nullable, 
    // but inheritance is cleaner for MVVM.
    // Let's use a single class for JSON storage simplicity to avoid complex polymorphic JSON handling 
    // unless we need very different behaviors.
    
    // However, the user asked for "similar to Android app icon layout".
    
    public partial class HomeAppItem : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public HomeAppType Type { get; set; }

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private string url;

        [ObservableProperty]
        private string icon;

        // For Widget
        public string WidgetKey { get; set; }

        // Display properties (not stored in JSON potentially, or just part of the model)
        [JsonIgnore]
        public object WidgetContent { get; set; } // The actual UI element for the widget
    }
}
