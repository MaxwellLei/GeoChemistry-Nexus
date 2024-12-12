using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    //更新版本信息
    public class VersionInfo
    {
        public string Version { get; set; }
        public string Url { get; set; }
    }

    //检测更新
    public static class UpdateHelper
    {
        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                // 请求version.json
                using var httpClient = new HttpClient();
                var jsonResponse = await httpClient.GetStringAsync("https://tves-1303234197.cos.ap-beijing.myqcloud.com/Releases/version.json");

                // 解析version.json
                var latestVersionInfo = JsonSerializer.Deserialize<VersionInfo>(jsonResponse);
                var latestVersion = new Version(latestVersionInfo.Version);
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                // 比较版本
                if (latestVersion > currentVersion)
                {
                    Growl.Ask((string)System.Windows.Application.Current.Resources["FindNewVersion"], isConfirmed =>
                    {
                        //确认更新
                        if (isConfirmed)
                        {

                            Task.Run(async () =>
                            {
                                // 下载最新版本
                                var tempFilePath = Path.GetTempFileName();
                                using (var downloadStream = await httpClient.GetStreamAsync(latestVersionInfo.Url))
                                {
                                    using var fileStream = File.Create(tempFilePath);
                                    await downloadStream.CopyToAsync(fileStream);
                                }

                                // 解压并启动新版本
                                var tempFolderPath = Path.Combine(Path.GetTempPath(), "TimberValueEvaluationSystem");
                                Directory.CreateDirectory(tempFolderPath);
                                ZipFile.ExtractToDirectory(tempFilePath, tempFolderPath);
                                File.Delete(tempFilePath);

                                var newAppExePath = Path.Combine(tempFolderPath, "TimberValueEvaluationSystem.exe");

                                Process.Start(newAppExePath);
                                System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
                            });
                        }
                        return true;
                    });
                }
            }
            catch (Exception ex)
            {
                // 异常处理
                MessageHelper.Error("检查更新时发生错误：" + ex.Message);
            }
        }

    }
}
