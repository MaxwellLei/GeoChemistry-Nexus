using CommunityToolkit.Mvvm.Messaging.Messages;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Messages
{
    public class TemplateCardLayoutChangedMessage : ValueChangedMessage<TemplateCardLayoutSettings>
    {
        public TemplateCardLayoutChangedMessage(TemplateCardLayoutSettings value) : base(value)
        {
        }
    }
}
