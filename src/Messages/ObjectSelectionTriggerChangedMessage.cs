using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    public class ObjectSelectionTriggerChangedMessage : ValueChangedMessage<string>
    {
        public ObjectSelectionTriggerChangedMessage(string value) : base(value)
        {
        }
    }
}
