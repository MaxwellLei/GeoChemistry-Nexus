using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GeoChemistryNexus.Messages
{
    /// <summary>
    /// 应用主题色已切换，Value 为新的主题 Id。
    /// </summary>
    public sealed class AppThemeChangedMessage : ValueChangedMessage<string>
    {
        public AppThemeChangedMessage(string themeId) : base(themeId)
        {
        }
    }
}
