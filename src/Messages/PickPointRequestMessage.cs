using CommunityToolkit.Mvvm.Messaging.Messages;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Messages
{
    // 请求拾取点的消息
    public class PickPointRequestMessage : ValueChangedMessage<PointDefinition>
    {
        public PickPointRequestMessage(PointDefinition value) : base(value)
        {
        }
    }
}
