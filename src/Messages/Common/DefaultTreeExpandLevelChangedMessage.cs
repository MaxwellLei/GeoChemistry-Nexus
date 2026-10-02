using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    public class DefaultTreeExpandLevelChangedMessage : ValueChangedMessage<int>
    {
        public DefaultTreeExpandLevelChangedMessage(int value) : base(value)
        {
        }
    }
}