using System;
using System.IO;
using System.Linq;

namespace Persistence_Defender
{
    internal class StartupFoldersDefender : IPersistenceDefender
    {
        private static FileSystemWatcher[] startupWatchers;

        public void StartDefender()
        {
            // Get all user-specific startup folders
            string usersDirectory = "C:\\Users";
            string[] userStartupPaths = Directory.GetDirectories(usersDirectory)
                .Select(userDir => Path.Combine(userDir, "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "Startup"))
                .Where(Directory.Exists)
                .ToArray();

            // System-wide startup folders
            string commonStartupPath = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";

            // Combine all startup folders (user-specific + system-wide)
            string[] allStartupPaths = userStartupPaths.Append(commonStartupPath).ToArray();

            // Monitor all startup folders
            startupWatchers = allStartupPaths.Select(CreateWatcher).Where(w => w != null).ToArray();
        }

        private static FileSystemWatcher CreateWatcher(string path)
        {
            try
            {
                // Check if the directory exists
                if (!Directory.Exists(path))
                {
                    return null;
                }

                // Create a new file system watcher for that directory
                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = path,
                    Filter = "*.*", // Monitor all file types
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
                };
                watcher.Created += OnFileCreated;
                watcher.EnableRaisingEvents = true;
                return watcher;
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error creating startup folders defender watcher: {ex.Message}");
                return null;
            }
        }

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                EventLogger.WriteWarning($"New startup file detected: {e.FullPath}");
                if (File.Exists(e.FullPath))
                {
                    File.Delete(e.FullPath);
                    EventLogger.WriteInfo($"Startup file '{e.FullPath}' deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error in startup folders defender watcher: {ex.Message}");
            }
        }

        public void StopDefender()
        {
            try
            {
                foreach (FileSystemWatcher watcher in startupWatchers)
                {
                    watcher?.Dispose();
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping startup folders defender");
            }
        }
    }
}