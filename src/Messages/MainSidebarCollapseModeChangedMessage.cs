using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    public class MainSidebarCollapseModeChangedMessage : ValueChangedMessage<bool>
    {
        public MainSidebarCollapseModeChangedMessage(bool useIconOnlyMode) : base(useIconOnlyMode)
        {
        }
    }
}
