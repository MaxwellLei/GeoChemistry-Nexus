using System.Collections.Generic;

namespace GeoChemistryNexus.Services.Epma;

public enum EpmaTemplateAction { Edit, Delete, Export, PromoteOfficial, RemoveOfficial, ForceUpdate }

public static class EpmaTemplateMenuPolicy
{
    /// <summary>Not-downloaded official templates have no local package, so no local action applies to them (the entries are hidden, not disabled).</summary>
    public static IReadOnlyList<EpmaTemplateAction> Actions(bool isCustom, bool developerMode, bool isInstalled = true)
    {
        if (!isCustom && !isInstalled) return new List<EpmaTemplateAction>();
        var actions = new List<EpmaTemplateAction> { EpmaTemplateAction.Edit };
        if (isCustom) actions.Add(EpmaTemplateAction.Delete);
        actions.Add(EpmaTemplateAction.Export);
        if (developerMode)
        {
            if (isCustom) actions.Add(EpmaTemplateAction.PromoteOfficial);
            else { actions.Add(EpmaTemplateAction.ForceUpdate); actions.Add(EpmaTemplateAction.RemoveOfficial); }
        }
        return actions;
    }
}
