using System.Globalization;
using System.Linq;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>A template icon is plain text, limited by displayed characters rather than UTF-16 units.</summary>
public static class EpmaTemplateIcon
{
    public static string Normalize(string? value)
    {
        string text = string.Concat((value ?? "").Where(c => !char.IsControl(c))).Trim();
        int[] starts = StringInfo.ParseCombiningCharacters(text);
        return starts.Length <= 3 ? text : text[..starts[3]];
    }
}
