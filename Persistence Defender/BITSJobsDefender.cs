using System;
using System.Management;

namespace Persistence_Defender
{
    public class BITSJobsDefender : IPersistenceDefender
    {
        private ManagementEventWatcher watcher;

        public void StartDefender()
        {
            try
            {
                string query = "SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'MSFT_BitsJob'";
                watcher = new ManagementEventWatcher(new ManagementScope("\\\\.\\root\\Microsoft\\Windows\\Bits"), new EventQuery(query));
                watcher.EventArrived += OnBITSJobCreated;
                watcher.Start();
                EventLogger.WriteInfo("Started BITS jobs defender.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error starting BITS jobs defender: {ex.Message}");
            }
        }

        private static void OnBITSJobCreated(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var newBITSJob = e.NewEvent["TargetInstance"] as ManagementBaseObject;
                string jobName = newBITSJob?["DisplayName"]?.ToString() ?? "Unknown";
                EventLogger.WriteWarning($"New BITS job detected: {jobName}");

                // Attempt to remove the BITS job
                RemoveBITSJob(jobName);
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error in BITS jobs defender watcher: {ex.Message}");
            }
        }

        private static void RemoveBITSJob(string jobName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\Microsoft\\Windows\\Bits", "SELECT * FROM MSFT_BitsJob WHERE DisplayName='" + jobName + "'"))
                {
                    foreach (ManagementObject job in searcher.Get())
                    {
                        job.InvokeMethod("Cancel", null);
                        EventLogger.WriteInfo($"BITS job '{jobName}' canceled successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error removing BITS job '{jobName}': {ex.Message}");
            }
        }

        public void StopDefender()
        {
            try
            {
                if (watcher != null)
                {
                    watcher.Stop();
                    watcher.Dispose();
                    watcher = null;
                    EventLogger.WriteInfo("Stopped BITS jobs defender.");
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping BITS jobs defender: {ex.Message}");
            }
        }
    }
}
