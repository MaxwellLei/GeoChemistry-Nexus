using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    /// <summary>
    /// Message sent when script syntax validation passes
    /// </summary>
    public class ScriptValidatedMessage : ValueChangedMessage<bool>
    {
        public ScriptValidatedMessage(bool isValid) : base(isValid)
        {
        }
    }
}
