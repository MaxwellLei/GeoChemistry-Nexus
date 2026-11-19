using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 「搜索无结果」统一反馈提示控件（GitHub Issues / 邮箱均为可点击的主题色链接）。
    /// 仅负责展示与转发链接打开，不含业务逻辑；各模块直接放入空状态下方即可。
    /// </summary>
    public partial class TemplateFeedbackHint : UserControl
    {
        public const string GitHubIssuesUrl = "https://github.com/MaxwellLei/GeoChemistry-Nexus/issues";
        public const string ContactEmail = "maxwelllei@qq.com";
        public const string ContactMailtoUrl = "mailto:" + ContactEmail;

        public ICommand OpenLinkCommand { get; } = new RelayCommand<string>(ExternalLinkHelper.OpenUrl);

        public TemplateFeedbackHint()
        {
            InitializeComponent();
        }
    }
}
