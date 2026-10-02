using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    public class DeveloperModeChangedMessage : ValueChangedMessage<bool>
    {
        public DeveloperModeChangedMessage(bool isDeveloperMode) : base(isDeveloperMode)
        {
        }
    }
}
