using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;
using static System.Net.Mime.MediaTypeNames;

namespace Persistence_Defender
{
    internal class SchTasksDefender : BasePersistenceDefender
    {
        private static ManagementEventWatcher watcher;

        public SchTasksDefender(int mode) : base(mode) { }

        public override void StartDefender()
        {
            if (Mode != 0)
            {
                try
                {
                    string query = "SELECT * FROM __InstanceCreationEvent WITHIN 10 WHERE TargetInstance ISA 'MSFT_ScheduledTask'";
                    watcher = new ManagementEventWatcher(new ManagementScope(@"\\.\root\Microsoft\Windows\TaskScheduler"), new EventQuery(query));
                    watcher.EventArrived += OnScheduledTaskCreated;
                    watcher.Start();
                    EventLogger.WriteInfo("Started scheduled tasks defender.");
                }
                catch (Exception ex)
                {
                    EventLogger.WriteError($"Error starting scheduled tasks defender: {ex.Message}");
                }
            }
        }

        private static void OnScheduledTaskCreated(object sender, EventArrivedEventArgs e)
        {
            try
            {
                // Extract task name
                var scheduledTask = e.NewEvent["TargetInstance"] as ManagementBaseObject;
                string taskName = scheduledTask?["URI"]?.ToString() ?? "Unknown";
                EventLogger.WriteWarning($"New scheduled task created: {taskName}");

                // Undo the change
                RemoveScheduledTask(taskName);
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error in scheduled tasks defender watcher: {ex.Message}");
            }
        }
        private static void RemoveScheduledTask(string taskName)
        {
            try
            {
                using (TaskService ts = new TaskService())
                {
                    Microsoft.Win32.TaskScheduler.Task task = ts.GetTask(taskName);
                    if (task != null)
                    {
                        ts.RootFolder.DeleteTask(taskName, false);
                        EventLogger.WriteInfo($"Scheduled task '{taskName}' deleted successfully.");
                    }
                    else
                    {
                        EventLogger.WriteError($"Scheduled task '{taskName}' not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error removing scheduled task '{taskName}': {ex.Message}");
            }
        }

        public override void StopDefender()
        {
            try
            {
                if (watcher != null)
                {
                    watcher.Stop();
                    watcher.Dispose();
                    watcher = null;
                    EventLogger.WriteInfo("Stopped scheduled tasks defender.");
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping scheduled tasks defender: {ex.Message}");
            }
        }
    }
}
