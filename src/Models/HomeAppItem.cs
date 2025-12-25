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

        // 用于小部件
        public string WidgetKey { get; set; }

        [JsonIgnore]
        public bool IsWebLink => Type == HomeAppType.WebLink;

        //  显示属性
        [JsonIgnore]
        public object WidgetContent { get; set; } // 小组件对象
    }
}
