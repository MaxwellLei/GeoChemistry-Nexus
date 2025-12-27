using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    public class UnsavedChangesMessage : ValueChangedMessage<bool>
    {
        public UnsavedChangesMessage(bool value) : base(value)
        {
        }
    }
}
