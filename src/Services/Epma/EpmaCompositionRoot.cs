using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>All EPMA methods, including General APFU, are replaceable self-contained packages.</summary>
public static class EpmaCompositionRoot
{
    /// <summary>Folder for small per-user EPMA files such as the recent-template list; templates live in the LiteDB store.</summary>
    public static string HistoryDirectory { get; } = AppDataPathHelper.GetDataPath("Epma");

    public static EpmaTemplateCatalog Templates { get; } = new(EpmaTemplateDatabaseService.Instance);
}
