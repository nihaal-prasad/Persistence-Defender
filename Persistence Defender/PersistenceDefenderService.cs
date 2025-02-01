using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Persistence_Defender
{
    public partial class PersistenceDefenderService : ServiceBase
    {
        private List<IPersistenceDefender> defenders;

        public PersistenceDefenderService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Load kernel driver

            // Define all persistence defenders that will be loaded
            defenders = new List<IPersistenceDefender>
            {
                new SchTasksDefender()
            };

            // TODO: Check user configuration before simply loading everything

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
