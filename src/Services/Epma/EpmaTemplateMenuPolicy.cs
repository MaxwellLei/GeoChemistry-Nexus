using System.Collections.Generic;

namespace GeoChemistryNexus.Services.Epma;

public enum EpmaTemplateAction { Edit, Delete, Export, PromoteOfficial, RemoveOfficial }

public static class EpmaTemplateMenuPolicy
{
    public static IReadOnlyList<EpmaTemplateAction> Actions(bool isCustom, bool developerMode)
    {
        var actions = new List<EpmaTemplateAction> { EpmaTemplateAction.Edit };
        if (isCustom) actions.Add(EpmaTemplateAction.Delete);
        actions.Add(EpmaTemplateAction.Export);
        if (developerMode) actions.Add(isCustom ? EpmaTemplateAction.PromoteOfficial : EpmaTemplateAction.RemoveOfficial);
        return actions;
    }
}
