using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public class HomeLinksCatalog
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("groups")]
        public List<HomeLinkGroup> Groups { get; set; } = new();
    }

    public class HomeLinkGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("links")]
        public List<HomeLinkEntry> Links { get; set; } = new();
    }

    public class HomeLinkEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "\uE774";
    }
}
