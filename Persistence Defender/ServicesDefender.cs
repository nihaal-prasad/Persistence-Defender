using System;
using System.Management;
using System.ServiceProcess;

namespace Persistence_Defender
{
    public class ServicesDefender : BasePersistenceDefender
    {
        private ManagementEventWatcher watcher;

        public ServicesDefender(int mode) : base(mode) { }

        public override void StartDefender()
        {
            if (Mode != 0)
            {
                try
                {
                    string query = "SELECT * FROM __InstanceCreationEvent WITHIN 10 WHERE TargetInstance ISA 'Win32_Service'";
                    watcher = new ManagementEventWatcher(new ManagementScope("\\\\.\\root\\cimv2"), new EventQuery(query));
                    watcher.EventArrived += OnServiceCreated;
                    watcher.Start();
                    EventLogger.WriteInfo("Started services defender.");
                }
                catch (Exception ex)
                {
                    EventLogger.WriteError($"Error starting services defender: {ex.Message}");
                }
            }
        }

        private static void OnServiceCreated(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var newService = e.NewEvent["TargetInstance"] as ManagementBaseObject;
                string serviceName = newService?["Name"]?.ToString() ?? "Unknown";
                EventLogger.WriteWarning($"New service detected: {serviceName}");

                // Attempt to remove the service
                RemoveService(serviceName);
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error in services defender watcher: {ex.Message}");
            }
        }
        private static void RemoveService(string serviceName)
        {
            try
            {
                using (ServiceController service = new ServiceController(serviceName))
                {
                    if (service.Status != ServiceControllerStatus.Stopped)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }

                using (var managementObject = new ManagementObject($"Win32_Service.Name='{serviceName}'"))
                {
                    managementObject.Get();
                    managementObject.InvokeMethod("Delete", null);
                }

                EventLogger.WriteInfo($"Service '{serviceName}' deleted successfully.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error removing service '{serviceName}': {ex.Message}");
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
                    EventLogger.WriteInfo("Stopped services defender.");
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteError($"Error stopping services defender: {ex.Message}");
            }
        }
    }
}
