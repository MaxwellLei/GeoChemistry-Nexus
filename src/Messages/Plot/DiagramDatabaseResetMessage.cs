namespace GeoChemistryNexus.Messages
{
    /// <summary>
    /// 图解模板本地数据发生整体变化（数据库被重置、批量改写文件夹名称等）时发送的消息，
    /// 图解页收到后会重新加载模板库
    /// </summary>
    public sealed class DiagramDatabaseResetMessage
    {
    }
}
