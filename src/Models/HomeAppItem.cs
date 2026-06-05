using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
        [NotifyPropertyChangedFor(nameof(IsIconUrl))]
        private string icon;

        [JsonIgnore]
        public bool IsIconUrl => HomeIconHelper.IsUrlIcon(Icon);

        // 小组件 Key
        public string WidgetKey { get; set; }

        [JsonIgnore]
        public bool IsWebLink => Type == HomeAppType.WebLink;

        /// <summary>
        /// 来自服务器官方目录的链接，不可编辑或删除。
        /// </summary>
        [JsonIgnore]
        public bool IsOfficial { get; set; }

        [JsonIgnore]
        public bool IsReadOnly => IsOfficial;

        // 小组件提示
        [JsonIgnore]
        public object WidgetContent { get; set; }
    }
}
