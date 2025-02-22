using System;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Management;

namespace Persistence_Defender
{
    public class BITSJobsDefender : IPersistenceDefender
    {
        ManagementEventWatcher watcher;

        public void StartDefender()
        {
            try
            {
                string queryString = "*[System/Provider[@Name='Microsoft-Windows-Bits-Client'] and System/EventID=3]";
                string logName = "Microsoft-Windows-Bits-Client/Operational";
                EventLogQuery eventQuery = new EventLogQuery(logName, PathType.LogName, queryString);
                EventLogWatcher watcher = new EventLogWatcher(eventQuery);
                watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(OnEventRecordWritten);
                watcher.Enabled = true;
                EventLogger.WriteInfo("Started BITS Jobs defender.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error creating BITS Jobs Defender: {ex.Message}");
            }
        }

        static void OnEventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            if (e.EventRecord != null)
            {
                try
                {
                    string jobIdString = e.EventRecord.Properties[1].Value.ToString();
                    if (Guid.TryParse(jobIdString, out Guid jobId))
                    {
                        EventLogger.WriteWarning($"New BITS job detected: {jobId}. Attempting to cancel it...");
                        CancelBITSJob(jobId);
                    }
                    else
                    {
                        EventLogger.WriteError($"Failed to parse BITS job ID: {jobIdString}");
                    }
                }
                catch (Exception ex)
                {
                    EventLogger.WriteError($"Error processing BITS event: {ex.Message}");
                }
            }
        }

        static void CancelBITSJob(Guid jobId)
        {
            try
            {
                IBackgroundCopyManager manager = (IBackgroundCopyManager)new BackgroundCopyManager();
                manager.GetJob(ref jobId, out IBackgroundCopyJob job);

                if (job != null)
                {
                    job.Cancel();
                    EventLogger.WriteInfo($"Successfully canceled BITS job: {jobId}");
                }
                else
                {
                    EventLogger.WriteWarning($"BITS job {jobId} not found.");
                }
            }
            catch (COMException ex)
            {
                EventLogger.WriteError($"Failed to cancel BITS job {jobId}: {ex.Message}");
            }
        }

        public void StopDefender()
        {
            try
            {
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping BITS Jobs Defender: {ex.Message}");
            }
        }
    }

    [ComImport, Guid("5CE34C0D-0DC9-4C1F-897C-DAA1B78CEE7C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IBackgroundCopyManager
    {
        void CreateJob([MarshalAs(UnmanagedType.LPWStr)] string displayName,
                       BG_JOB_TYPE type,
                       out Guid jobId,
                       out IBackgroundCopyJob job);
        void GetJob(ref Guid jobId, out IBackgroundCopyJob job);
    }

    [ComImport, Guid("37668D37-507E-4160-9316-26306D150B12"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IBackgroundCopyJob
    {
        void AddFile([MarshalAs(UnmanagedType.LPWStr)] string remoteUrl,
                     [MarshalAs(UnmanagedType.LPWStr)] string localName);
        void Resume();
        void Suspend();
        void Cancel();
        void Complete();
    }

    [ComImport, Guid("659CDEA7-489E-11D9-A9CD-000D56965251"), ClassInterface(ClassInterfaceType.None)]
    class BackgroundCopyManager { }

    enum BG_JOB_TYPE
    {
        BG_JOB_TYPE_DOWNLOAD = 0,
        BG_JOB_TYPE_UPLOAD = 1,
        BG_JOB_TYPE_UPLOAD_REPLY = 2
    }
}
