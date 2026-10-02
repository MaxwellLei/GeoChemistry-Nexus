using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public class HomeUserConfig
    {
        [JsonPropertyName("personalLinks")]
        public List<HomeAppItem> PersonalLinks { get; set; } = new();

        [JsonPropertyName("widgets")]
        public List<HomeAppItem> Widgets { get; set; } = new();
    }
}
