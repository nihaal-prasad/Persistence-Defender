using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.IO;

namespace Persistence_Defender
{
    public class RegKeysDefender : IPersistenceDefender
    {
        private readonly RegistryKey rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        private readonly Dictionary<string, Dictionary<string, object>> registrySnapshot = new Dictionary<string, Dictionary<string, object>>();
        private readonly List<string> registryPaths = new List<string>
        {
            "Software\\Microsoft\\Windows\\CurrentVersion\\Run",
            "Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
            "Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows\\AppInit_DLLs",
            "Software\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options",
            "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions",
            "Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon",
            "SYSTEM\\CurrentControlSet\\Services",
            "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Custom",
            "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\InstalledSDB"
        };

        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, int dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

        private const int REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;
        private const int REG_NOTIFY_THREAD_SLEEP_MS = 10000;
        private Thread monitorThread;
        private bool stopRequested = false;

        public void DisplayRegistrySnapshot()
        {
            string snapshotInfo = string.Join(" | ", registrySnapshot.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value.Select(v => $"{v.Key}={v.Value}"))}"));
            EventLogger.WriteInfo($"Registry Snapshot: {snapshotInfo}");
        }

        public void StartDefender()
        {
            try
            {
                StopDefender();
                TakeRegistrySnapshot();

                stopRequested = false;
                monitorThread = new Thread(MonitorRegistryKeys) { IsBackground = true };
                monitorThread.Start();
                EventLogger.WriteInfo("Started registry keys defender.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error starting registry keys defender: {ex.Message}");
            }
        }

        private void MonitorRegistryKeys()
        {
            try
            {
                while (!stopRequested)
                {
                    foreach (var path in registryPaths)
                    {
                        using (RegistryKey regKey = rootKey.OpenSubKey(path, true))
                        {
                            if (regKey != null)
                            {
                                bool ownsHandle = false;
                                try
                                {
                                    regKey.Handle.DangerousAddRef(ref ownsHandle);
                                    IntPtr hKey = regKey.Handle.DangerousGetHandle();
                                    RegNotifyChangeKeyValue(hKey, true, REG_NOTIFY_CHANGE_LAST_SET, IntPtr.Zero, true);
                                    RestoreRegistryValues(path);
                                }
                                finally
                                {
                                    if (ownsHandle)
                                    {
                                        regKey.Handle.DangerousRelease();
                                    }
                                }
                            }
                        }
                    }
                    Thread.Sleep(REG_NOTIFY_THREAD_SLEEP_MS);
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error monitoring registry keys: {ex.Message}");
            }
        }

        private void TakeRegistrySnapshot()
        {
            foreach (var path in registryPaths)
            {
                try
                {
                    CaptureRegistryValues(path);
                }
                catch (Exception ex)
                {
                    EventLogger.WriteError($"Failed to take registry snapshot for {path}: {ex.Message}");
                }
            }
            EventLogger.WriteInfo("Registry snapshot taken successfully.");
        }

        private void CaptureRegistryValues(string path)
        {
            using (RegistryKey regKey = rootKey.OpenSubKey(path, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
            {
                if (regKey == null)
                {
                    return;
                }

                var values = new Dictionary<string, object>();
                foreach (string valueName in regKey.GetValueNames())
                {
                    values[valueName] = regKey.GetValue(valueName) ?? "(null)";
                }
                registrySnapshot[path] = values;

                foreach (string subKey in regKey.GetSubKeyNames())
                {
                    CaptureRegistryValues($"{path}\\{subKey}");
                }
            }
        }

        private void RestoreRegistryValues(string path)
        {
            try
            {
                if (registrySnapshot.ContainsKey(path))
                {
                    using (RegistryKey regKey = rootKey.OpenSubKey(path, true))
                    {
                        if (regKey != null)
                        {
                            foreach (string existingValue in regKey.GetValueNames())
                            {
                                if (!registrySnapshot[path].ContainsKey(existingValue))
                                {
                                    EventLogger.WriteWarning($"Detected unauthorized registry key: {path}\\{existingValue}");
                                    regKey.DeleteValue(existingValue, false);
                                    EventLogger.WriteInfo($"Deleted unauthorized key: {path}\\{existingValue}");
                                }
                            }

                            foreach (var kvp in registrySnapshot[path])
                            {
                                object currentValue = regKey.GetValue(kvp.Key);
                                if (currentValue == null || !currentValue.Equals(kvp.Value))
                                {
                                    EventLogger.WriteWarning($"Detected unauthorized registry modification in {path}\\{kvp.Key}");
                                    regKey.SetValue(kvp.Key, kvp.Value);
                                    EventLogger.WriteInfo($"Reverted unauthorized registry modification in {path}\\{kvp.Key}");
                                }
                            }

                            foreach (string subKey in regKey.GetSubKeyNames())
                            {
                                RestoreRegistryValues($"{path}\\{subKey}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Failed to revert registry changes in {path}: {ex.Message}");
            }
        }

        public void StopDefender()
        {
            try
            {
                stopRequested = true;
                if (monitorThread != null && monitorThread.IsAlive)
                {
                    monitorThread.Join();
                }
                rootKey.Close();
                EventLogger.WriteInfo("Stopped registry keys defender.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping registry keys defender: {ex.Message}");
            }
        }
    }
}
