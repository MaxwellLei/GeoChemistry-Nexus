using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    class BootHelper
    {
        //开机自动启动
        private const string RunValueName = "GeoChemistry Nexus";
        private const string LegacyRunValueName = "Geochemistrt Nexus";

        public static void SetAutoRun(bool isAutoRun)
        {
            using RegistryKey? reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (reg == null)
                return;

            if (isAutoRun)
            {
                reg.SetValue(RunValueName, System.Windows.Forms.Application.ExecutablePath);
            }
            else
            {
                reg.DeleteValue(RunValueName, throwOnMissingValue: false);
            }

            reg.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
        }
    }
}
