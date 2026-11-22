using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Workbench formatting; scientific values remain unchanged.</summary>
public static class EpmaResultPresentation
{
    public static string Number(double value, int decimals) => value.ToString("F" + Math.Clamp(decimals, 0, 10), CultureInfo.CurrentCulture);
}
