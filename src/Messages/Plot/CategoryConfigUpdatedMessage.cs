using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    /// <summary>
    /// 绘图分类配置已更新时发送的消息
    /// </summary>
    public class CategoryConfigUpdatedMessage : ValueChangedMessage<string>
    {
        public CategoryConfigUpdatedMessage(string value) : base(value)
        {
        }
    }
}
