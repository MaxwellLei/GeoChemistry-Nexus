using System.Collections.Generic;

namespace GeoChemistryNexus.Services.Epma;

public enum EpmaTemplateAction { Edit, Delete, Export, PromoteOfficial, RemoveOfficial, ForceUpdate, Download }

public static class EpmaTemplateMenuPolicy
{
    /// <summary>Not-downloaded official templates have no local package, so the only action offered is Download (local actions are hidden, not disabled).</summary>
    public static IReadOnlyList<EpmaTemplateAction> Actions(bool isCustom, bool developerMode, bool isInstalled = true)
    {
        if (!isCustom && !isInstalled) return new List<EpmaTemplateAction> { EpmaTemplateAction.Download };
        var actions = new List<EpmaTemplateAction> { EpmaTemplateAction.Edit };
        if (isCustom) actions.Add(EpmaTemplateAction.Delete);
        if (developerMode)
        {
            actions.Add(EpmaTemplateAction.Export);
            if (isCustom) actions.Add(EpmaTemplateAction.PromoteOfficial);
            else { actions.Add(EpmaTemplateAction.ForceUpdate); actions.Add(EpmaTemplateAction.RemoveOfficial); }
        }
        return actions;
    }
}
