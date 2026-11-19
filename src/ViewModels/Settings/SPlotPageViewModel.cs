using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SPlotPageViewModel : ObservableObject,
        IRecipient<TemplateCardLayoutChangedMessage>,
        IRecipient<DeveloperModeChangedMessage>
    {
        [ObservableProperty]
        private bool isDeveloperMode;

        [ObservableProperty]
        private string corelDRAWPath = string.Empty;

        [ObservableProperty]
        private string inkscapePath = string.Empty;

        [ObservableProperty]
        private string adobeIllustratorPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomPathInvalid))]
        private string customThirdPartyAppPath = string.Empty;

        // 默认图解列表展开层级 (1-4)
        [ObservableProperty]
        private int defaultTreeExpandLevel;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomEditorSelected))]
        private string selectedThirdPartyApp = string.Empty;

        /// <summary>默认第三方编辑程序下拉的选项（含徽标与检测状态）。</summary>
        public ObservableCollection<ThirdPartyEditorOption> ThirdPartyEditorOptions { get; } = new();

        /// <summary>当前是否选中“自定义”：为 true 时在下拉下方展开程序路径输入区。</summary>
        public bool IsCustomEditorSelected => SelectedThirdPartyApp == ThirdPartyEditorOption.CustomId;

        /// <summary>自定义路径非空但格式不合法（需以盘符开头并指向 .exe）。</summary>
        public bool IsCustomPathInvalid =>
            !string.IsNullOrWhiteSpace(CustomThirdPartyAppPath) && !ExternalEditorLocator.IsValidExePath(CustomThirdPartyAppPath);

        [ObservableProperty]
        private bool autoCheckTemplateUpdate;

        // 选中对象触发方式：SingleClick 或 DoubleClick
        [ObservableProperty]
        private string objectSelectionTrigger = string.Empty;

        // 鼠标吸附自动识别帧率
        [ObservableProperty]
        private int mouseSnapAutoRecognitionFrameRate;

        // 默认初始表格行数
        [ObservableProperty]
        private int defaultWorksheetRowCount;

        // 图解模板卡片大小档位
        [ObservableProperty]
        private TemplateCardSizePreset templateCardSizePreset = TemplateCardSizePreset.Standard;

        [ObservableProperty]
        private TemplateCardAppearance templateCardAppearance = TemplateCardAppearance.Frameless;

        public TemplateCardSizePreset[] TemplateCardSizePresetOptions { get; } =
        {
            TemplateCardSizePreset.Standard,
            TemplateCardSizePreset.Compact
        };

        // 当前图解版本（只读）
        public string CurrentDiagramVersion { get; } = ContentVersionHelper.GetDiagramFormatVersion();

        public ObservableCollection<int> MouseSnapAutoRecognitionFrameRates { get; } = new() { 24, 30, 60, 90, 144 };

        public ObservableCollection<int> DefaultWorksheetRowCounts { get; } =
            new(WorksheetDefaultsHelper.AllowedRowCounts);

        public RelayCommand SelectCorelDRAWPathCommand { get; }
        public RelayCommand SelectInkscapePathCommand { get; }
        public RelayCommand SelectAdobeIllustratorPathCommand { get; }
        public RelayCommand SelectCustomThirdPartyAppPathCommand { get; }
        public RelayCommand OpenInkscapeDownloadCommand { get; }

        private bool isLoading = true;

        public SPlotPageViewModel()
        {
            WeakReferenceMessenger.Default.RegisterAll(this);

            BuildThirdPartyEditorOptions();

            SelectCorelDRAWPathCommand = new RelayCommand(ExecuteSelectCorelDRAWPath);
            SelectInkscapePathCommand = new RelayCommand(ExecuteSelectInkscapePath);
            SelectAdobeIllustratorPathCommand = new RelayCommand(ExecuteSelectAdobeIllustratorPath);
            SelectCustomThirdPartyAppPathCommand = new RelayCommand(ExecuteSelectCustomThirdPartyAppPath);
            OpenInkscapeDownloadCommand = new RelayCommand(ExecuteOpenInkscapeDownload);

            LoadConfig();
        }

        public void Receive(TemplateCardLayoutChangedMessage message)
        {
            ApplyTemplateCardLayoutSettings(message.Value, persist: false, notify: false, showToast: false);
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            IsDeveloperMode = message.Value;
        }

        private void LoadConfig()
        {
            isLoading = true;

            IsDeveloperMode = bool.TryParse(ConfigHelper.GetConfig(ConfigKeys.DeveloperMode), out bool devMode) && devMode;

            // 自动检测 CorelDRAW / Inkscape / Illustrator：已安装但未配置（或原路径失效）时自动填入路径
            ExternalEditorLocator.EnsureConfiguredPaths();

            CorelDRAWPath = ConfigHelper.GetConfig(ConfigKeys.CorelDrawPath) ?? string.Empty;
            InkscapePath = ConfigHelper.GetConfig(ConfigKeys.InkscapePath) ?? string.Empty;
            AdobeIllustratorPath = ConfigHelper.GetConfig(ConfigKeys.AdobeIllustratorPath) ?? string.Empty;
            CustomThirdPartyAppPath = ConfigHelper.GetConfig(ConfigKeys.CustomThirdPartyAppPath) ?? string.Empty;
            
            if (int.TryParse(ConfigHelper.GetConfig(ConfigKeys.DefaultTreeExpandLevel), out int expandLevel))
            {
                DefaultTreeExpandLevel = expandLevel;
            }
            else
            {
                DefaultTreeExpandLevel = 2; // Default to level 2
            }

            SelectedThirdPartyApp = ConfigHelper.GetConfig(ConfigKeys.DefaultThirdPartyApp) ?? string.Empty;
            if (string.IsNullOrEmpty(SelectedThirdPartyApp))
            {
                SelectedThirdPartyApp = "Inkscape";
            }

            // 配置缺失时默认开启，与温压计、EPMA 保持一致
            AutoCheckTemplateUpdate = !bool.TryParse(ConfigHelper.GetConfig(ConfigKeys.AutoCheckTemplateUpdate), out bool checkTemp) || checkTemp;

            ObjectSelectionTrigger = ConfigHelper.GetConfig(ConfigKeys.ObjectSelectionTrigger) ?? string.Empty;
            if (string.IsNullOrEmpty(ObjectSelectionTrigger))
            {
                ObjectSelectionTrigger = "SingleClick"; // 默认单击
            }

            if (int.TryParse(ConfigHelper.GetConfig(ConfigKeys.MouseSnapAutoRecognitionFrameRate), out int snapFrameRate)
                && MouseSnapAutoRecognitionFrameRates.Contains(snapFrameRate))
            {
                MouseSnapAutoRecognitionFrameRate = snapFrameRate;
            }
            else
            {
                MouseSnapAutoRecognitionFrameRate = 24;
            }

            DefaultWorksheetRowCount = WorksheetDefaultsHelper.GetDefaultRowCount(WorksheetDefaultsHelper.DiagramConfigKey);

            ApplyTemplateCardLayoutSettings(TemplateCardLayoutHelper.LoadFromConfig(), persist: false, notify: false, showToast: false);

            RefreshThirdPartyEditorOptions();

            isLoading = false;
        }

        private void BuildThirdPartyEditorOptions()
        {
            // 徽标为通用字母标识（非官方 Logo），配色沿用设计稿
            ThirdPartyEditorOptions.Add(new ThirdPartyEditorOption("CorelDRAW", "CorelDRAW", "Cd", "#2F9B48", "#FFFFFF"));
            ThirdPartyEditorOptions.Add(new ThirdPartyEditorOption("Inkscape", "Inkscape", "Ink", "#1B1B1B", "#FFFFFF"));
            ThirdPartyEditorOptions.Add(new ThirdPartyEditorOption("Adobe Illustrator", "Adobe Illustrator", "Ai", "#2B1700", "#FF9A00"));
            ThirdPartyEditorOptions.Add(new ThirdPartyEditorOption(
                ThirdPartyEditorOption.CustomId,
                LanguageService.GetString("geo_label_custom", "自定义"),
                string.Empty, "#00000000", "#00000000"));
        }

        /// <summary>
        /// 根据当前路径配置刷新各选项的检测状态与收起时显示的名称。
        /// </summary>
        private void RefreshThirdPartyEditorOptions()
        {
            string detected = LanguageService.GetString("editor_status_detected", "已检测到");
            string notDetected = LanguageService.GetString("editor_status_not_detected", "未检测到");

            foreach (var option in ThirdPartyEditorOptions)
            {
                switch (option.Id)
                {
                    case ExternalEditorLocator.CorelDrawId:
                        ApplyDetectedStatus(option, ExternalEditorLocator.IsAvailable(CorelDRAWPath, option.Id), detected, notDetected);
                        break;
                    case ExternalEditorLocator.InkscapeId:
                        ApplyDetectedStatus(option, ExternalEditorLocator.IsAvailable(InkscapePath, option.Id), detected, notDetected);
                        break;
                    case ExternalEditorLocator.IllustratorId:
                        ApplyDetectedStatus(option, ExternalEditorLocator.IsAvailable(AdobeIllustratorPath, option.Id), detected, notDetected);
                        break;
                    default:
                        bool specified = ExternalEditorLocator.IsValidExePath(CustomThirdPartyAppPath);
                        option.IsStatusOk = specified;
                        option.StatusText = specified
                            ? LanguageService.GetString("editor_status_specified", "已指定")
                            : LanguageService.GetString("editor_status_pick_exe", "选择 .exe");
                        option.TriggerText = specified
                            ? System.IO.Path.GetFileName(CustomThirdPartyAppPath.Trim())
                            : option.DisplayName;
                        break;
                }
            }
        }

        private static void ApplyDetectedStatus(ThirdPartyEditorOption option, bool available, string detected, string notDetected)
        {
            option.IsStatusOk = available;
            option.StatusText = available ? detected : notDetected;
        }

        // 加载配置期间不逐项刷新，统一在 LoadConfig 末尾刷新一次
        partial void OnCorelDRAWPathChanged(string value)
        {
            if (!isLoading) RefreshThirdPartyEditorOptions();
        }

        partial void OnInkscapePathChanged(string value)
        {
            if (!isLoading) RefreshThirdPartyEditorOptions();
        }

        partial void OnAdobeIllustratorPathChanged(string value)
        {
            if (!isLoading) RefreshThirdPartyEditorOptions();
        }

        partial void OnCustomThirdPartyAppPathChanged(string value)
        {
            if (isLoading) return;

            RefreshThirdPartyEditorOptions();

            // 路径格式不合法时只提示，不写入配置
            if (!string.IsNullOrWhiteSpace(value) && !ExternalEditorLocator.IsValidExePath(value)) return;

            ConfigHelper.SetConfig(ConfigKeys.CustomThirdPartyAppPath, value.Trim());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        private void ApplyTemplateCardLayoutSettings(TemplateCardLayoutSettings settings, bool persist, bool notify, bool showToast)
        {
            bool previousLoading = isLoading;
            isLoading = true;
            try
            {
                TemplateCardSizePreset = settings.SizePreset;
                TemplateCardAppearance = settings.Appearance;
            }
            finally
            {
                isLoading = previousLoading;
            }

            if (persist)
            {
                TemplateCardLayoutHelper.SaveToConfig(settings);
            }

            if (notify)
            {
                WeakReferenceMessenger.Default.Send(new TemplateCardLayoutChangedMessage(new TemplateCardLayoutSettings
                {
                    SizePreset = TemplateCardSizePreset,
                    Appearance = TemplateCardAppearance
                }));
            }

            if (showToast)
            {
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        private void PersistTemplateCardLayoutFromUi()
        {
            if (isLoading) return;

            ApplyTemplateCardLayoutSettings(new TemplateCardLayoutSettings
            {
                SizePreset = TemplateCardSizePreset,
                Appearance = TemplateCardAppearance
            }, persist: true, notify: true, showToast: true);
        }

        private void ExecuteSelectCorelDRAWPath()
        {
            string? path = FileHelper.GetFilePath("CorelDRAW Executable (*.exe)|*.exe");
            if (!string.IsNullOrEmpty(path))
            {
                CorelDRAWPath = path;
                ConfigHelper.SetConfig(ConfigKeys.CorelDrawPath, path);
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        private void ExecuteSelectInkscapePath()
        {
            string? path = FileHelper.GetFilePath("Inkscape Executable (*.exe)|*.exe");
            if (!string.IsNullOrEmpty(path))
            {
                InkscapePath = path;
                ConfigHelper.SetConfig(ConfigKeys.InkscapePath, path);
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        private void ExecuteOpenInkscapeDownload()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://inkscape.org/") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageService.Instance["unable_to_open_link"] + ex.Message);
            }
        }

        private void ExecuteSelectAdobeIllustratorPath()
        {
            string? path = FileHelper.GetFilePath("Adobe Illustrator Executable (*.exe)|*.exe");
            if (!string.IsNullOrEmpty(path))
            {
                AdobeIllustratorPath = path;
                ConfigHelper.SetConfig(ConfigKeys.AdobeIllustratorPath, path);
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        private void ExecuteSelectCustomThirdPartyAppPath()
        {
            string? path = FileHelper.GetFilePath("Executable (*.exe)|*.exe");
            if (!string.IsNullOrEmpty(path))
            {
                // 保存与提示由 OnCustomThirdPartyAppPathChanged 统一处理
                CustomThirdPartyAppPath = path;
            }
        }

        partial void OnSelectedThirdPartyAppChanged(string value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig(ConfigKeys.DefaultThirdPartyApp, value);
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnAutoCheckTemplateUpdateChanged(bool value)
        {
            if (isLoading) return;
             ConfigHelper.SetConfig(ConfigKeys.AutoCheckTemplateUpdate, value.ToString());
             MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnObjectSelectionTriggerChanged(string value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig(ConfigKeys.ObjectSelectionTrigger, value);
            WeakReferenceMessenger.Default.Send(new ObjectSelectionTriggerChangedMessage(value));
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnDefaultTreeExpandLevelChanged(int value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig(ConfigKeys.DefaultTreeExpandLevel, value.ToString());
            
            // 发送消息通知
            WeakReferenceMessenger.Default.Send(new DefaultTreeExpandLevelChangedMessage(value));

            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnMouseSnapAutoRecognitionFrameRateChanged(int value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig(ConfigKeys.MouseSnapAutoRecognitionFrameRate, value.ToString());
            WeakReferenceMessenger.Default.Send(new MouseSnapAutoRecognitionFrameRateChangedMessage(value));
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnDefaultWorksheetRowCountChanged(int value)
        {
            if (isLoading) return;
            WorksheetDefaultsHelper.SaveDefaultRowCount(WorksheetDefaultsHelper.DiagramConfigKey, value);
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnTemplateCardSizePresetChanged(TemplateCardSizePreset value)
        {
            PersistTemplateCardLayoutFromUi();
        }

        partial void OnTemplateCardAppearanceChanged(TemplateCardAppearance value)
        {
            PersistTemplateCardLayoutFromUi();
        }

        [RelayCommand]
        private async Task ResetDiagramDatabaseAsync()
        {
            string message = LanguageService.Instance["confirm_reset_diagram_database"]
                ?? "Are you sure you want to clear the local diagram template database? All local diagram template data will be removed. This action cannot be undone.";

            bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                message,
                LanguageService.Instance["Cancel"] ?? "Cancel",
                LanguageService.Instance["clear"] ?? "Clear");

            if (!isConfirmed)
                return;

            try
            {
                GraphMapDatabaseService.Instance.ClearDatabase();
                WeakReferenceMessenger.Default.Send(new DiagramDatabaseResetMessage());
                MessageHelper.Success(LanguageService.Instance["diagram_database_cleared"] ?? "Local diagram template database cleared successfully.");
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.Message);
            }
        }
    }
}
