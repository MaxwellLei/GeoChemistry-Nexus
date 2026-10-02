using GeoChemistryNexus.Converter;
using GeoChemistryNexus.Helpers;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public class HomeLinksCatalog
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 2;

        [JsonPropertyName("groups")]
        public List<HomeLinkGroup> Groups { get; set; } = new();
    }

    public class HomeLinkGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        [JsonConverter(typeof(LocalizedStringJsonConverter))]
        public LocalizedString Title { get; set; } = new();

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
        [JsonConverter(typeof(LocalizedStringJsonConverter))]
        public LocalizedString Title { get; set; } = new();

        [JsonPropertyName("description")]
        [JsonConverter(typeof(LocalizedStringJsonConverter))]
        public LocalizedString Description { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "\uE774";
    }
}
