using Microsoft.Win32;
using System;
using static OlegMC.REST_API.Data.Global;

namespace OlegMC.REST_API.Data
{
    public static class RegistryHelper
    {
        private static readonly string run_registry_location = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool ShouldStartOnBoot()
        {
            if (OperatingSystem.IsWindows())
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(run_registry_location);
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
            Logger.Info("Enabling Start On Boot");
            if (force || !ShouldStartOnBoot())
            {
                if (OperatingSystem.IsWindows())
                {
                    Logger.Debug("Enabling for Windows");
                    using RegistryKey key = Registry.CurrentUser.OpenSubKey(run_registry_location, true);
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
            Logger.Info("Disabling Start On Boot");
            if (ShouldStartOnBoot())
            {
                if (OperatingSystem.IsWindows())
                {
                    Logger.Debug("Disabling for Windows");
                    using RegistryKey key = Registry.CurrentUser.OpenSubKey(run_registry_location, true);
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