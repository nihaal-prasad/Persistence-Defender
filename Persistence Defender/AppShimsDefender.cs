using System;
using System.IO;
using System.Linq;

namespace Persistence_Defender
{
    public class AppShimsDefender : IPersistenceDefender
    {
        private FileSystemWatcher[] watchers;

        public void StartDefender()
        {
            try
            {
                string[] systemShimPaths =
                {
                    "C:\\Windows\\AppCompat\\Programs",
                    "C:\\Windows\\AppPatch\\Custom",
                    "C:\\Windows\\AppPatch\\Custom\\Custom64",
                    "C:\\Windows\\AppPatch\\CustomSDB",
                    "C:\\Windows\\System32\\AppCompat\\Programs",
                    "C:\\Windows\\Temp",
                    "C:\\Windows\\Tasks",
                    "C:\\Windows\\System32\\Tasks"
                };

                string usersDirectory = "C:\\Users";
                string[] userShimPaths = Directory.GetDirectories(usersDirectory)
                    .Select(userDir => Path.Combine(userDir, "AppData", "Local", "Microsoft", "Windows", "AppCompat", "Programs"))
                    .Where(Directory.Exists)
                    .ToArray();

                string[] allShimPaths = systemShimPaths.Concat(userShimPaths).ToArray();

                watchers = allShimPaths.Select(CreateWatcher).Where(w => w != null).ToArray();

                EventLogger.WriteInfo("Started application shims defender.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error starting application shims defender: {ex.Message}");
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
                    Path = path,
                    Filter = "*.sdb", // Monitor only .sdb shim files
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
                };

                watcher.Created += OnAppShimCreated;
                watcher.Renamed += OnAppShimCreated;
                watcher.EnableRaisingEvents = true;
                return watcher;
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error creating watcher for {path}: {ex.Message}");
                return null;
            }
        }

        private static void OnAppShimCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                EventLogger.WriteWarning($"New application shim detected: {e.FullPath}");

                if (File.Exists(e.FullPath))
                {
                    File.Delete(e.FullPath);
                    EventLogger.WriteInfo($"Application shim '{e.FullPath}' deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error in application shims defender watcher: {ex.Message}");
            }
        }

        public void StopDefender()
        {
            try
            {
                if (watchers != null)
                {
                    foreach (var watcher in watchers)
                    {
                        watcher?.Dispose();
                    }
                }
                watchers = new FileSystemWatcher[0];
                EventLogger.WriteInfo("Stopped application shims defender.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping application shims defender: {ex.Message}");
            }
        }
    }
}
