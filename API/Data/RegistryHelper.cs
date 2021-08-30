using ChaseLabs.CLLogger.Interfaces;
using Microsoft.Win32;
using System;

namespace OlegMC.REST_API.Data
{
    public static class RegistryHelper
    {
        private static readonly ILog log = Global.Logger;
        private static readonly string run = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool ShouldStartOnBoot()
        {
            if (OperatingSystem.IsWindows())
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(run);
                if (key.GetValue(@"OlegMC_Server_Manager") != null && key.GetValue(@"OlegMC_Server_Manager").GetType().Equals(typeof(string)) && !((string)key.GetValue(@"OlegMC_Server_Manager")).Equals($"\"{Global.Paths.ExecutingBinary}\""))
                {
                    EnableStartOnBoot(true);
                }

                return key.GetValue(@"OlegMC_Server_Manager") != null;
            }
            else if (OperatingSystem.IsLinux())
            {
            }
            else if (OperatingSystem.IsMacOS())
            {
            }
            return false;
        }

        public static void EnableStartOnBoot(bool force = false)
        {
            log.Info("Enabling Start On Boot");
            if (force || !ShouldStartOnBoot())
            {
                if (OperatingSystem.IsWindows())
                {
                    log.Debug("Enabling for Windows");
                    using RegistryKey key = Registry.CurrentUser.OpenSubKey(run, true);
                    key.SetValue(@"OlegMC_Server_Manager", $"\"{Global.Paths.ExecutingBinary}\"");
                }
                else if (OperatingSystem.IsLinux())
                {

                }
                else if (OperatingSystem.IsMacOS())
                {

                }
            }
        }

        public static void DisableStartOnBoot()
        {
            log.Info("Disabling Start On Boot");
            if (ShouldStartOnBoot())
            {
                if (OperatingSystem.IsWindows())
                {
                    log.Debug("Disabling for Windows");
                    using RegistryKey key = Registry.CurrentUser.OpenSubKey(run, true);
                    key.DeleteValue(@"OlegMC_Server_Manager");
                }
                else if (OperatingSystem.IsLinux())
                {

                }
                else if (OperatingSystem.IsMacOS())
                {

                }
            }
        }
    }
}
