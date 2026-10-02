using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models.PeriodicTable
{
    public sealed class ElementGeochemCatalog
    {
        [JsonPropertyName("source")]
        public ElementGeochemSourceInfo Source { get; set; } = new();

        [JsonPropertyName("elements")]
        public List<ElementGeochemRecord> Elements { get; set; } = new();
    }

    public sealed class ElementGeochemSourceInfo
    {
        [JsonPropertyName("database")]
        public string Database { get; set; } = string.Empty;

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("abundanceCrustCitation")]
        public string AbundanceCrustCitation { get; set; } = string.Empty;

        [JsonPropertyName("abundanceCrustUnit")]
        public string AbundanceCrustUnit { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public sealed class ElementGeochemRecord
    {
        [JsonPropertyName("atomicNumber")]
        public int AtomicNumber { get; set; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("atomicWeight")]
        public double? AtomicWeight { get; set; }

        [JsonPropertyName("abundanceCrustMgPerKg")]
        public double? AbundanceCrustMgPerKg { get; set; }

        [JsonPropertyName("abundanceSeaMgPerL")]
        public double? AbundanceSeaMgPerL { get; set; }

        [JsonPropertyName("goldschmidtClass")]
        public string? GoldschmidtClass { get; set; }

        [JsonPropertyName("geochemicalClass")]
        public string? GeochemicalClass { get; set; }

        [JsonPropertyName("electronicConfiguration")]
        public string? ElectronicConfiguration { get; set; }

        [JsonPropertyName("period")]
        public int Period { get; set; }

        [JsonPropertyName("groupId")]
        public int? GroupId { get; set; }

        [JsonPropertyName("block")]
        public string? Block { get; set; }

        [JsonPropertyName("series")]
        public string? Series { get; set; }

        [JsonPropertyName("oxidationStates")]
        public List<OxidationStateRecord> OxidationStates { get; set; } = new();
    }

    public sealed class OxidationStateRecord
    {
        [JsonPropertyName("state")]
        public int State { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;
    }

    public sealed class OxideFormulaInfo
    {
        public OxideFormulaInfo(string formula, double molecularWeight)
        {
            Formula = formula;
            MolecularWeight = molecularWeight;
        }

        public string Formula { get; }
        public double MolecularWeight { get; }
    }
}
