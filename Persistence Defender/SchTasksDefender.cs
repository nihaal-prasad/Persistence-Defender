using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Persistence_Defender
{
    internal class SchTasksDefender
    {
        private static ManagementEventWatcher watcher;

        public static void StartDefender()
        {
            try
            {
                string query = "SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'MSFT_ScheduledTask'";
                watcher = new ManagementEventWatcher(new ManagementScope(@"\\.\root\Microsoft\Windows\TaskScheduler"), new EventQuery(query));
                watcher.EventArrived += OnScheduledTaskCreated;
                watcher.Start();
            }
            catch (Exception e) { /* TODO: Add logs for exceptions */ }
        }

        private static void OnScheduledTaskCreated(object sender, EventArrivedEventArgs e)
        {
            try
            {
                // Extract task name
                var scheduledTask = e.NewEvent["TargetInstance"] as ManagementBaseObject;
                string taskName = scheduledTask?["Name"]?.ToString() ?? "Unknown";

                Console.WriteLine($"[ALERT] New Scheduled Task Created: {taskName}");

                // TODO: Write to Windows Event Log
                // WriteToEventLog($"New scheduled task created: {taskName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing event: {ex.Message}");
            }
        }

        /*
        private static void WriteToEventLog(string message)
        {
            string source = "ScheduledTaskMonitor";
            string log = "Application";

            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, log);
            }

            EventLog.WriteEntry(source, message, EventLogEntryType.Warning);
        }
         */

        public static void StopDefender()
        {
            try
            {
                if (watcher != null)
                {
                    watcher.Stop();
                    watcher.Dispose();
                    watcher = null;
                    Console.WriteLine("WMI subscription removed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing WMI subscription: {ex.Message}");
            }
        }
    }
}
