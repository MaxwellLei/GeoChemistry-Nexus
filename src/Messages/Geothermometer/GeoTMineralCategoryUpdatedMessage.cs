using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    public class GeoTMineralCategoryUpdatedMessage : ValueChangedMessage<string>
    {
        public GeoTMineralCategoryUpdatedMessage(string value) : base(value)
        {
        }
    }
}
