using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// .NET 运行时检测。用于判断更新时是否可以使用不内置运行时的精简版安装包。
    /// </summary>
    public static class DotNetRuntimeHelper
    {
        // 与 installer\GeoChemistryNexus.iss 中 IsDotNet10DesktopInstalled 读取的是同一个注册表键，
        // 保证“客户端认为已安装”与“精简版安装程序认为已安装”结论一致。
        // dotnet 宿主读取该键使用 32 位视图（WOW6432Node），这里同样用 Registry32。
        private const string DesktopRuntimeRegistryKey =
            @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App";

        /// <summary>
        /// 当前进程正在使用的 .NET 运行时主版本号。
        /// </summary>
        public static int CurrentRuntimeMajor => Environment.Version.Major;

        /// <summary>
        /// 本机是否已注册指定主版本的 x64 .NET Desktop 运行时。
        /// </summary>
        public static bool IsDesktopRuntimeRegistered(int major)
        {
            if (major <= 0)
                return false;

            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                using var key = baseKey.OpenSubKey(DesktopRuntimeRegistryKey);
                if (key == null)
                    return false;

                string prefix = major + ".";
                foreach (string valueName in key.GetValueNames())
                {
                    if (valueName.StartsWith(prefix, StringComparison.Ordinal))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DotNetRuntimeHelper] Registry check failed: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 目标版本所需的运行时是否已满足：当前进程就运行在该主版本上，且注册表中能找到对应运行时。
        /// </summary>
        public static bool IsRequiredRuntimeSatisfied(int requiredMajor)
        {
            return requiredMajor > 0
                && CurrentRuntimeMajor == requiredMajor
                && IsDesktopRuntimeRegistered(requiredMajor);
        }
    }
}
