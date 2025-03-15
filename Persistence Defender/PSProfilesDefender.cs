using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Persistence_Defender
{
    public class PSProfilesDefender : BasePersistenceDefender
    {
        private FileSystemWatcher[] watchers;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileAttributes(string lpFileName, uint dwFileAttributes);

        private const uint FILE_ATTRIBUTE_READONLY = 0x1;

        public PSProfilesDefender(int mode) : base(mode) { }

        public override void StartDefender()
        {
            if (Mode != 0)
            {
                try
                {
                    string usersDirectory = "C:\\Users";
                    string[] userProfiles = Directory.GetDirectories(usersDirectory);

                    string systemProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                    string[] psProfilePaths = userProfiles.SelectMany(userDir => new[]
                    {
                    Path.Combine(userDir, "Documents", "WindowsPowerShell", "Profile.ps1"),
                    Path.Combine(userDir, "Documents", "PowerShell", "Profile.ps1"),
                    Path.Combine(userDir, "Documents", "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1"),
                    Path.Combine(userDir, "Documents", "PowerShell", "Microsoft.PowerShell_profile.ps1")
                }).ToArray();

                    string[] allProfilePaths = psProfilePaths.Concat(new[]
                    {
                    Path.Combine(systemProfilePath, "Microsoft", "WindowsPowerShell", "Profile.ps1"),
                    Path.Combine(systemProfilePath, "Microsoft", "PowerShell", "Profile.ps1"),
                    Path.Combine(systemProfilePath, "Microsoft", "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1"),
                    Path.Combine(systemProfilePath, "Microsoft", "PowerShell", "Microsoft.PowerShell_profile.ps1")
                }).ToArray();

                    foreach (string path in psProfilePaths)
                    {
                        if (File.Exists(path))
                        {
                            SetFileAttributes(path, FILE_ATTRIBUTE_READONLY);
                        }
                    }

                    watchers = allProfilePaths.Select(CreateWatcher).Where(w => w != null).ToArray();

                    if (Mode == 1)
                        EventLogger.WriteInfo("Started PowerShell profiles defender.");
                    else if (Mode == 2)
                        EventLogger.WriteInfo("Started PowerShell profiles defender (logging-only mode).");
                }
                catch (Exception ex)
                {
                    EventLogger.WriteError($"Error starting PowerShell profiles defender: {ex.Message}");
                }
            }
        }

        private FileSystemWatcher CreateWatcher(string path)
        {
            try
            {

                if (!Directory.Exists(path))
                {
                    return null;
                }

                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(path),
                    Filter = Path.GetFileName(path),
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Security | NotifyFilters.Attributes
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

        private void OnProfileAccessAttempt(object sender, FileSystemEventArgs e)
        {
            try
            {
                EventLogger.WriteWarning($"Modification attempt detected on PowerShell profile: {e.FullPath}");
                if (Mode == 1)
                {
                    SetFileAttributes(e.FullPath, FILE_ATTRIBUTE_READONLY);
                    EventLogger.WriteInfo($"Reapplied read-only protection to PowerShell profile: {e.FullPath}");
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error handling PowerShell profile modification attempt: {ex.Message}");
            }
        }

        public override void StopDefender()
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
