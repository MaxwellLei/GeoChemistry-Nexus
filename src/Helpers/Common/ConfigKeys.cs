namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 用户配置（App.config 的 appSettings）键名集中定义。
    /// 与 <see cref="ConfigHelper"/> 配合使用，禁止在业务代码中直接书写键名字符串。
    /// 键名需与 Data\Config\App.config 保持一致。
    /// </summary>
    public static class ConfigKeys
    {
        // ── 通用 ──────────────────────────────────────────────
        /// <summary>界面语言</summary>
        public const string Language = "language";
        /// <summary>应用主题</summary>
        public const string AppTheme = "app_theme";
        /// <summary>界面缩放模式</summary>
        public const string UiScaleMode = "ui_scale_mode";
        /// <summary>主窗体侧边栏收起样式（0 / 1）</summary>
        public const string MainSidebarCollapseMode = "main_sidebar_collapse_mode";
        /// <summary>通知自动关闭时间（下拉框索引）</summary>
        public const string AutoOffTime = "auto_off_time";
        /// <summary>开机启动</summary>
        public const string Boot = "boot";
        /// <summary>退出程序模式</summary>
        public const string ExitProgramMode = "exit_program_mode";
        /// <summary>开发者模式</summary>
        public const string DeveloperMode = "developer_mode";
        /// <summary>自动检查软件更新</summary>
        public const string AutoCheckUpdate = "auto_check_update";

        // ── 图解 ──────────────────────────────────────────────
        /// <summary>自动检查图解模板更新</summary>
        public const string AutoCheckTemplateUpdate = "auto_check_template_update";
        /// <summary>鼠标吸附自动识别帧率</summary>
        public const string MouseSnapAutoRecognitionFrameRate = "mouse_snap_auto_recognition_frame_rate";
        /// <summary>对象选择触发方式</summary>
        public const string ObjectSelectionTrigger = "object_selection_trigger";
        /// <summary>默认树展开层级</summary>
        public const string DefaultTreeExpandLevel = "default_tree_expand_level";
        /// <summary>最近使用的图解模板</summary>
        public const string RecentTemplates = "recent_templates";
        /// <summary>图解卡片大小档位</summary>
        public const string TemplateCardSizePreset = "template_card_size_preset";
        /// <summary>图解卡片样式</summary>
        public const string TemplateCardAppearance = "template_card_appearance";
        /// <summary>图解默认初始表格行数</summary>
        public const string DiagramDefaultWorksheetRowCount = "diagram_default_worksheet_row_count";

        // ── 第三方矢量编辑程序 ────────────────────────────────
        /// <summary>默认第三方程序</summary>
        public const string DefaultThirdPartyApp = "default_third_party_app";
        /// <summary>CorelDRAW 程序路径</summary>
        public const string CorelDrawPath = "coreldraw_path";
        /// <summary>Inkscape 程序路径</summary>
        public const string InkscapePath = "inkscape_path";
        /// <summary>Adobe Illustrator 程序路径</summary>
        public const string AdobeIllustratorPath = "adobe_illustrator_path";
        /// <summary>自定义第三方程序路径</summary>
        public const string CustomThirdPartyAppPath = "custom_third_party_app_path";

        // ── 地质温压计 ────────────────────────────────────────
        /// <summary>自动检查地质温压计更新</summary>
        public const string AutoCheckGtmUpdate = "auto_check_gtm_update";
        /// <summary>温压计默认初始表格行数</summary>
        public const string GtmDefaultWorksheetRowCount = "gtm_default_worksheet_row_count";
        /// <summary>温压计侧边栏状态</summary>
        public const string GeotSidebarState = "geot_sidebar_state";

        // ── EPMA ──────────────────────────────────────────────
        /// <summary>自动检查 EPMA 计算模板更新</summary>
        public const string AutoCheckEpmaUpdate = "auto_check_epma_update";
        /// <summary>EPMA 默认初始表格行数</summary>
        public const string EpmaDefaultWorksheetRowCount = "epma_default_worksheet_row_count";

        // ── 官方模板发布 ──────────────────────────────────────
        /// <summary>发布暂存目录</summary>
        public const string PublishStagingDir = "publish_staging_dir";
    }
}
