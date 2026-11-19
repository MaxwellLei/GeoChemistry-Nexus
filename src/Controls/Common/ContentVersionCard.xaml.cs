using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 图解、地质温压计、EPMA 共用的版本卡片。
    /// 只展示格式基线、内容修订和保存后版本；修订号的读写由所在页面的 ViewModel 完成。
    /// </summary>
    public partial class ContentVersionCard : UserControl
    {
        public ContentVersionCard()
        {
            InitializeComponent();
        }
    }
}
