using System;
using System.Globalization;
using System.Linq;

namespace GeoChemistryNexus.Services.Epma;

public static class EpmaLocalizer
{
    public static string Format(string key, params object?[] args)
    {
        string value = LanguageService.Instance[key];
        if (string.IsNullOrEmpty(value)) value = key;
        if (args.Length == 0) return value;
        try { return string.Format(CultureInfo.CurrentCulture, value, args); }
        catch (FormatException) { return value; }
    }
}
