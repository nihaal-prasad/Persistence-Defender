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
        public PersistenceDefenderService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Load kernel driver


        }

        protected override void OnStop()
        {
        }
    }
}
