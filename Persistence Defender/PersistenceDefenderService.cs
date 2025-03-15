using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

// Command for installing: sc create "Persistence Defender Service" binPath="C:\Users\Administrator\Desktop\Debug\Persistence Defender.exe" start= auto
// Command for running: sc start "Persistence Defender Service"

namespace Persistence_Defender
{
    public partial class PersistenceDefenderService : ServiceBase
    {
        private List<BasePersistenceDefender> defenders;

        public PersistenceDefenderService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Check service settings in registry
            SettingsReader settingsReader = new SettingsReader();
            settingsReader.LoadRegistryValues();

            // Define all persistence defenders that will be loaded
            defenders = new List<BasePersistenceDefender>
            {
                new SchTasksDefender(settingsReader.GetRegistryValue("SchTasksDefender")),
                new StartupFoldersDefender(settingsReader.GetRegistryValue("StartupFoldersDefender")),
                new AppShimsDefender(settingsReader.GetRegistryValue("AppShimsDefender")),
                new ServicesDefender(settingsReader.GetRegistryValue("ServicesDefender")),
                new PSProfilesDefender(settingsReader.GetRegistryValue("PSProfilesDefender")),
                new BITSJobsDefender(settingsReader.GetRegistryValue("BITSJobsDefender")),
                new RegKeysDefender(settingsReader.GetRegistryValue("RegKeysDefender"))
            };

            // TODO: Check for logging-only mode

            // Load all persistence defenders
            foreach (var defender in defenders)
            {
                defender.StartDefender();
            }
        }

        protected override void OnStop()
        {
            foreach(var defender in defenders)
            {
                defender.StopDefender();
            }
        }
    }
}
