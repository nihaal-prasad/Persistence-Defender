using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Persistence_Defender
{
    public class PSProfilesDefender : IPersistenceDefender
    {
        private FileSystemWatcher[] watchers;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileAttributes(string lpFileName, uint dwFileAttributes);

        private const uint FILE_ATTRIBUTE_READONLY = 0x1;

        public void StartDefender()
        {
            try
            {
                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string systemProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                string[] psProfilePaths =
                {
                    Path.Combine(userProfilePath, "WindowsPowerShell", "Profile.ps1"),
                    Path.Combine(userProfilePath, "PowerShell", "Profile.ps1"),
                    Path.Combine(systemProfilePath, "Microsoft", "WindowsPowerShell", "Profile.ps1"),
                    Path.Combine(systemProfilePath, "Microsoft", "PowerShell", "Profile.ps1")
                };

                foreach (string path in psProfilePaths)
                {
                    if (File.Exists(path))
                    {
                        SetFileAttributes(path, FILE_ATTRIBUTE_READONLY);
                    }
                }

                watchers = psProfilePaths.Select(CreateWatcher).Where(w => w != null).ToArray();

                EventLogger.WriteInfo("Started PowerShell profiles defender.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error starting PowerShell profiles defender: {ex.Message}");
            }
        }

        private static FileSystemWatcher CreateWatcher(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(path),
                    Filter = Path.GetFileName(path),
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Security
                };

                watcher.Changed += OnProfileAccessAttempt;
                watcher.Created += OnProfileAccessAttempt;
                watcher.Renamed += OnProfileAccessAttempt;
                watcher.EnableRaisingEvents = true;
                return watcher;
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error creating watcher for PowerShell profile: {ex.Message}");
                return null;
            }
        }

        private static void OnProfileAccessAttempt(object sender, FileSystemEventArgs e)
        {
            try
            {
                EventLogger.WriteWarning($"Unauthorized modification attempt detected on PowerShell profile: {e.FullPath}");
                SetFileAttributes(e.FullPath, FILE_ATTRIBUTE_READONLY);
                EventLogger.WriteInfo($"Reapplied read-only protection to PowerShell profile: {e.FullPath}");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error handling PowerShell profile modification attempt: {ex.Message}");
            }
        }

        public void StopDefender()
        {
            try
            {
                foreach (FileSystemWatcher watcher in watchers)
                {
                    watcher?.Dispose();
                }
                EventLogger.WriteInfo("Stopped PowerShell profiles defender.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping PowerShell profiles defender: {ex.Message}");
            }
        }
    }
}
