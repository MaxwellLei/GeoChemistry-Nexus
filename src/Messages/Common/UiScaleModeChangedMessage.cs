using CommunityToolkit.Mvvm.Messaging.Messages;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Messages
{
    public sealed class UiScaleModeChangedMessage : ValueChangedMessage<UiScaleMode>
    {
        public UiScaleModeChangedMessage(UiScaleMode mode) : base(mode)
        {
        }
    }
}
