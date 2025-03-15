using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Persistence_Defender
{
    public class SettingsReader
    {
        private const string RegistryPath = "Software\\PersistenceDefenderService";
        private static readonly string[] KeysToCheck =
        {
            "Running",
            "SchTasksDefender",
            "StartupFoldersDefender",
            "AppShimsDefender",
            "ServicesDefender",
            "PSProfilesDefender",
            "BITSJobsDefender",
            "RegKeysDefender"
        };

        /*
         * In startupSettings defender settings:
         * 0 indicates that the defender is disabled.
         * 1 indicates that the defender is enabled.
         * 2 indicates that only logging is enabled.
         * 
         * For Running:
         * 0 indicates that the program immediately terminates.
         * 1 indicates that the program executes properly.
         * Note that this setting is only checked when the service is initially
         * started in order to prevent attackers from easily stopping the
         * service without rebooting.
         */
        private Dictionary<string, int> startupSettings = new Dictionary<string, int>();

        public void LoadRegistryValues()
        {
            try
            {
                RegistryKey rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                using (RegistryKey key = rootKey.OpenSubKey(RegistryPath, true) ??
                                         rootKey.CreateSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        foreach (string valueName in KeysToCheck)
                        {
                            object value = key.GetValue(valueName);

                            if(value == null)
                            {
                                key.SetValue(valueName, 1);
                                startupSettings[valueName] = 1;
                            }
                            else if (value is int intValue)
                            {
                                if (valueName == "Running" && intValue != 1)
                                {
                                    EventLogger.WriteError($"Error: {RegistryPath} is not equal to 1.");
                                    Environment.Exit(1);
                                }
                                else if (valueName.EndsWith("Defender") && (intValue < 0 || intValue > 2))
                                {
                                    EventLogger.WriteError($"Error: Invalid value detected for '{valueName}': {intValue}. Must be 0, 1, or 2.");
                                    Environment.Exit(1);
                                }

                                startupSettings[valueName] = intValue;
                            }
                            else
                            {
                                // If the value exists but isn't an int, throw an error and exit
                                EventLogger.WriteError($"Error accessing non-int registry value in {RegistryPath}.");
                                Environment.Exit(1);
                            }
                        }
                    }
                    else
                    {
                        EventLogger.WriteError($"Error opening registry key: {RegistryPath}");
                        Environment.Exit(1); // Force exit on failure to access registry key
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error accessing registry: {ex.Message}");
                Environment.Exit(1); // Force exit on registry access error
            }
        }

        public int GetRegistryValue(string key)
        {
            if (startupSettings.ContainsKey(key))
            {
                return startupSettings[key];
            }
            else
            {
                EventLogger.WriteError($"Requested registry key '{key}' does not exist in '{RegistryPath}'.");
                Environment.Exit(1); // Force exit on registry access error
                return 0;
            }
        }
    }
}
