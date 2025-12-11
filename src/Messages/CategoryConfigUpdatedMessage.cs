using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    /// <summary>
    /// Message sent when the plot category configuration has been updated.
    /// </summary>
    public class CategoryConfigUpdatedMessage : ValueChangedMessage<string>
    {
        public CategoryConfigUpdatedMessage(string value) : base(value)
        {
        }
    }
}
