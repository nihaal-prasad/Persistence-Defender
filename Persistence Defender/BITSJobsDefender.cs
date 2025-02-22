using System;
using System.Diagnostics.Eventing.Reader;
using System.Management;
using System.Runtime.InteropServices;

namespace Persistence_Defender
{
    public class BITSJobsDefender : IPersistenceDefender
    {
        ManagementEventWatcher watcher;

        public void StartDefender()
        {
            try
            {
                // Define an XPath query to filter for BITS job creation events.
                // For example, suppose that BITS logs a job creation event with EventID=1 from the "BITS" provider.
                // You may need to adjust the Provider/@Name and EventID based on your environment.
                string queryString = "*[System/Provider[@Name='Microsoft-Windows-Bits-Client'] and System/EventID=3]";

                // Specify the event log channel that contains the BITS events.
                string logName = "Microsoft-Windows-Bits-Client/Operational";

                // Create an EventLogQuery for the channel with the specified query.
                EventLogQuery eventQuery = new EventLogQuery(logName, PathType.LogName, queryString);

                // Instantiate the EventLogWatcher with the query.
                EventLogWatcher watcher = new EventLogWatcher(eventQuery);

                // Register the callback that will be triggered when a matching event is written.
                watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(OnEventRecordWritten);

                // Enable the watcher. This starts monitoring without a manual loop.
                watcher.Enabled = true;

                EventLogger.WriteInfo("Started BITS Jobs defender.");
            } catch (Exception ex)
            {
                EventLogger.WriteError($"Error creating BITS Jobs Defender: {ex.Message}");
            }
        }

        static void OnEventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            if (e.EventRecord != null)
            {
                EventLogger.WriteWarning($"New BITS job detected: {e.EventRecord.RecordId}");

                // TODO: Insert BITS job cancellation code here.
            }
        }

        public void StopDefender()
        {
            try
            {
                watcher.Dispose();
            } catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping BITS Jobs Defender: {ex.Message}");
            }
        }
    }
}