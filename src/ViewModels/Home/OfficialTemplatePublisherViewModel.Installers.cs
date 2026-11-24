using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 官方发布器：软件安装包（完整版 / 精简版）的 SHA256 与所需运行时版本。
    /// 选好安装包文件后自动计算哈希，随“最新版本”一起写入 server_info.json。
    /// </summary>
    public partial class OfficialTemplatePublisherViewModel
    {
        private const string InstallerFileFilter = "安装包 (*.exe)|*.exe|所有文件 (*.*)|*.*";

        // 服务器上当前的值，用于判断“有改动”和提示忘记更新
        private string _remoteFullInstallerSha256 = string.Empty;
        private string _remoteLiteInstallerSha256 = string.Empty;
        private int _remoteRequiredRuntimeMajor;

        // 本次会话里选过的安装包路径。发布前会按路径重新计算，避免选完后又重新打包导致哈希过期
        private string _fullInstallerPath = string.Empty;
        private string _liteInstallerPath = string.Empty;

        [ObservableProperty]
        private string fullInstallerSha256Text = string.Empty;

        [ObservableProperty]
        private string liteInstallerSha256Text = string.Empty;

        [ObservableProperty]
        private string requiredRuntimeMajorText = string.Empty;

        /// <summary>界面上显示“哈希来自哪个文件”</summary>
        [ObservableProperty]
        private string fullInstallerFileLabel = string.Empty;

        [ObservableProperty]
        private string liteInstallerFileLabel = string.Empty;

        [ObservableProperty]
        private bool installerHasRemoteChanges;

        partial void OnFullInstallerSha256TextChanged(string value) => UpdateServerConfigChangeState();

        partial void OnLiteInstallerSha256TextChanged(string value) => UpdateServerConfigChangeState();

        partial void OnRequiredRuntimeMajorTextChanged(string value) => UpdateServerConfigChangeState();

        /// <summary>
        /// 记下服务器上的安装包信息。<paramref name="fillEditor"/> 为 true 时同时覆盖编辑框（“从服务器载入”）。
        /// </summary>
        private void ApplyRemoteInstallerInfo(ServerInfo? info, bool fillEditor)
        {
            _remoteFullInstallerSha256 = UpdateHelper.NormalizeSha256(info?.LatestAppInstallerSha256);
            _remoteLiteInstallerSha256 = UpdateHelper.NormalizeSha256(info?.LatestAppInstallerLiteSha256);
            _remoteRequiredRuntimeMajor = Math.Max(0, info?.RequiredRuntimeMajor ?? 0);

            if (!fillEditor)
                return;

            _fullInstallerPath = string.Empty;
            _liteInstallerPath = string.Empty;
            FullInstallerFileLabel = string.Empty;
            LiteInstallerFileLabel = string.Empty;
            FullInstallerSha256Text = _remoteFullInstallerSha256;
            LiteInstallerSha256Text = _remoteLiteInstallerSha256;
            // 服务器上没有记录时直接填上默认值，发布出去的值一目了然，需要改的人自己改
            RequiredRuntimeMajorText = _remoteRequiredRuntimeMajor > 0
                ? _remoteRequiredRuntimeMajor.ToString()
                : DefaultRequiredRuntimeMajorText;
        }

        /// <summary>默认值：发布器自己运行的 .NET 主版本（与软件同一套代码，即新版软件的目标框架主版本）。</summary>
        private static string DefaultRequiredRuntimeMajorText => DotNetRuntimeHelper.CurrentRuntimeMajor.ToString();

        private void UpdateInstallerChangeState()
        {
            InstallerHasRemoteChanges =
                !string.Equals(UpdateHelper.NormalizeSha256(FullInstallerSha256Text), _remoteFullInstallerSha256, StringComparison.Ordinal)
                || !string.Equals(UpdateHelper.NormalizeSha256(LiteInstallerSha256Text), _remoteLiteInstallerSha256, StringComparison.Ordinal)
                || ParseRuntimeMajorOrZero(RequiredRuntimeMajorText) != _remoteRequiredRuntimeMajor;
        }

        private static int ParseRuntimeMajorOrZero(string? text)
        {
            return int.TryParse(text?.Trim(), out int value) && value > 0 ? value : 0;
        }

        /// <summary>
        /// 一次选择一个或两个安装包，按文件名自动区分完整版和精简版（文件名以 -lite 结尾的是精简版），再分别计算 SHA256。
        /// </summary>
        [RelayCommand]
        private async Task SelectInstallersAsync()
        {
            if (!EnsureDeveloperMode()) return;
            if (IsBusy) return;

            string[] paths = FileHelper.GetFilePaths(InstallerFileFilter, OwnerWindow);
            if (paths.Length == 0)
                return;

            string? fullPath = null;
            string? litePath = null;
            foreach (string path in paths)
            {
                bool lite = IsLiteInstallerFileName(Path.GetFileName(path));
                string? existing = lite ? litePath : fullPath;
                if (existing != null)
                {
                    ShowWarning($"选了两个{(lite ? "精简版" : "完整版")}安装包：{Path.GetFileName(existing)} 和 {Path.GetFileName(path)}。每种只能选一个，请重新选择。");
                    return;
                }

                if (lite)
                    litePath = path;
                else
                    fullPath = path;
            }

            IsBusy = true;
            try
            {
                Log($"正在计算安装包 SHA256：{string.Join("、", paths.Select(p => Path.GetFileName(p)))} ...");

                Task<string> fullTask = fullPath == null ? Task.FromResult(string.Empty) : UpdateHelper.ComputeFileSha256Async(fullPath);
                Task<string> liteTask = litePath == null ? Task.FromResult(string.Empty) : UpdateHelper.ComputeFileSha256Async(litePath);
                await Task.WhenAll(fullTask, liteTask);

                if (fullPath != null)
                    ApplySelectedInstaller(fullPath, fullTask.Result, lite: false);
                if (litePath != null)
                    ApplySelectedInstaller(litePath, liteTask.Result, lite: true);

                if (fullPath == null)
                    Log("这次没有选完整版安装包，完整版 SHA256 保持原值。");
                if (litePath == null)
                    Log("这次没有选精简版安装包，精简版 SHA256 保持原值。");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>精简版安装包的文件名以 -lite 结尾（不含扩展名），例如 GeoChemistryNexus-Setup-0.9.1-x64-lite.exe。</summary>
        private static bool IsLiteInstallerFileName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName)
                .EndsWith("-lite", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplySelectedInstaller(string path, string hash, bool lite)
        {
            string kind = lite ? "精简版" : "完整版";
            string fileName = Path.GetFileName(path);

            if (string.IsNullOrEmpty(hash))
            {
                ShowWarning($"读取不到{kind}安装包：{path}");
                return;
            }

            if (lite)
            {
                _liteInstallerPath = path;
                LiteInstallerFileLabel = fileName;
                LiteInstallerSha256Text = hash;

                // 精简版要靠运行时主版本才会被客户端启用；用户手动清空了的话补回默认值
                if (ParseRuntimeMajorOrZero(RequiredRuntimeMajorText) == 0)
                    RequiredRuntimeMajorText = DefaultRequiredRuntimeMajorText;
            }
            else
            {
                _fullInstallerPath = path;
                FullInstallerFileLabel = fileName;
                FullInstallerSha256Text = hash;
            }

            Log($"{kind}安装包 SHA256 = {hash}");

            string? nameWarning = CheckInstallerFileName(fileName, lite);
            if (nameWarning != null)
            {
                Log(nameWarning);
                ShowWarning(nameWarning);
            }
        }

        /// <summary>
        /// 文件名与“最新版本”对不上，说明这个哈希很可能属于别的版本，客户端按版本号拼出的下载文件会校验失败。
        /// </summary>
        private string? CheckInstallerFileName(string fileName, bool lite)
        {
            if (!TryNormalizeLatestAppVersion(LatestAppVersionText, out string version, out _)
                || string.IsNullOrWhiteSpace(version))
                return null;

            string expected = OfficialContentEndpoints.BuildInstallerFileName(version, lite);
            if (string.Equals(fileName, expected, StringComparison.OrdinalIgnoreCase))
                return null;

            return $"文件名是 {fileName}，而最新版本 {version} 对应的{(lite ? "精简版" : "完整版")}文件名应为 {expected}。请确认选对了安装包，并用这个名字上传。";
        }

        /// <summary>
        /// 选过安装包的，按路径重新算一遍；打包后又重新生成过文件时，能避免把过期的哈希发布出去。
        /// </summary>
        private async Task RefreshInstallerHashFromFileAsync(bool lite)
        {
            string path = lite ? _liteInstallerPath : _fullInstallerPath;
            if (string.IsNullOrEmpty(path))
                return;

            string kind = lite ? "精简版" : "完整版";
            if (!File.Exists(path))
            {
                Log($"{kind}安装包文件已不存在，沿用已填写的 SHA256：{path}");
                return;
            }

            string hash = await UpdateHelper.ComputeFileSha256Async(path);
            string current = UpdateHelper.NormalizeSha256(lite ? LiteInstallerSha256Text : FullInstallerSha256Text);
            if (string.IsNullOrEmpty(hash) || string.Equals(hash, current, StringComparison.Ordinal))
                return;

            Log($"{kind}安装包文件在选择之后有变化，已重新计算 SHA256：{hash}");
            if (lite)
                LiteInstallerSha256Text = hash;
            else
                FullInstallerSha256Text = hash;
        }

        /// <summary>
        /// 发布前取得要写入 server_info.json 的安装包信息。没勾选“公告与版本”时返回 null，不改动服务器上的值。
        /// </summary>
        private async Task<AppInstallerReleaseInfo?> ResolveInstallerInfoForPublishAsync()
        {
            if (!PublishAnnouncement)
                return null;

            await RefreshInstallerHashFromFileAsync(lite: false);
            await RefreshInstallerHashFromFileAsync(lite: true);

            string full = UpdateHelper.NormalizeSha256(FullInstallerSha256Text);
            string lite = UpdateHelper.NormalizeSha256(LiteInstallerSha256Text);

            if (full.Length > 0 && !UpdateHelper.IsValidSha256(full))
                throw new InvalidOperationException("完整版安装包 SHA256 格式不对，应为 64 位十六进制。");
            if (lite.Length > 0 && !UpdateHelper.IsValidSha256(lite))
                throw new InvalidOperationException("精简版安装包 SHA256 格式不对，应为 64 位十六进制。");

            string majorText = RequiredRuntimeMajorText?.Trim() ?? string.Empty;
            int major = 0;
            if (majorText.Length > 0 && (!int.TryParse(majorText, out major) || major < 0 || major > 99))
                throw new InvalidOperationException("所需运行时主版本应为整数，例如 10。");

            if (lite.Length > 0 && major <= 0)
                throw new InvalidOperationException("填写了精简版 SHA256，就必须同时填写所需运行时主版本（例如 10），否则客户端不会使用精简版。");

            return new AppInstallerReleaseInfo
            {
                FullSha256 = full,
                LiteSha256 = lite,
                RequiredRuntimeMajor = major
            };
        }

        /// <summary>
        /// 发布前检查安装包信息是否缺漏。哈希为空不会报错，客户端只是少了一道校验，
        /// 所以这里必须明确提示，不能悄悄放过。
        /// </summary>
        private List<string> CollectInstallerWarnings(string normalizedLatestVersion)
        {
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(normalizedLatestVersion))
                return warnings;

            string full = UpdateHelper.NormalizeSha256(FullInstallerSha256Text);
            string lite = UpdateHelper.NormalizeSha256(LiteInstallerSha256Text);
            int major = ParseRuntimeMajorOrZero(RequiredRuntimeMajorText);

            if (full.Length == 0)
                warnings.Add("⚠ 完整版安装包 SHA256 为空：用户下载更新后不会做哈希校验，只检查文件长度。请点“选择安装包”自动计算。");
            else if (!UpdateHelper.IsValidSha256(full))
                warnings.Add("⚠ 完整版安装包 SHA256 格式不对，应为 64 位十六进制。");

            if (lite.Length > 0 && !UpdateHelper.IsValidSha256(lite))
                warnings.Add("⚠ 精简版安装包 SHA256 格式不对，应为 64 位十六进制。");

            if (lite.Length > 0 && major <= 0)
                warnings.Add("⚠ 已填精简版 SHA256 但没填所需运行时主版本，客户端不会使用精简版。");

            // 版本号改了而哈希还是服务器上的旧值，几乎一定是忘了重新选安装包
            if (LatestAppVersionHasRemoteChanges)
            {
                if (full.Length > 0 && full == _remoteFullInstallerSha256)
                    warnings.Add("⚠ 最新版本号已变化，但完整版 SHA256 与服务器上的旧值相同，可能忘了重新选择新版本的安装包。");
                if (lite.Length > 0 && lite == _remoteLiteInstallerSha256)
                    warnings.Add("⚠ 最新版本号已变化，但精简版 SHA256 与服务器上的旧值相同，可能忘了重新选择新版本的安装包。");
            }

            return warnings;
        }

        private string BuildInstallerWarningText()
        {
            if (!PublishAnnouncement
                || !TryNormalizeLatestAppVersion(LatestAppVersionText, out string version, out _))
                return string.Empty;

            var warnings = CollectInstallerWarnings(version);
            return warnings.Count == 0 ? string.Empty : string.Join("\n", warnings);
        }
    }
}
