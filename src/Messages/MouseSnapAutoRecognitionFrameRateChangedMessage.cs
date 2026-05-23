using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    public class MouseSnapAutoRecognitionFrameRateChangedMessage : ValueChangedMessage<int>
    {
        public MouseSnapAutoRecognitionFrameRateChangedMessage(int value) : base(value)
        {
        }
    }
}
