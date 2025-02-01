using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence_Defender
{
    internal class EventLogger
    {
        public static void WriteWarning(string message)
        {
            string source = "PersistenceDefender";
            string log = "Application";

            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, log);
            }

            EventLog.WriteEntry(source, message, EventLogEntryType.Warning);
        }

        public static void WriteError(string message)
        {
            string source = "PersistenceDefender";
            string log = "Application";

            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, log);
            }

            EventLog.WriteEntry(source, message, EventLogEntryType.Error);
        }

        public static void WriteInfo(string message)
        {
            string source = "PersistenceDefender";
            string log = "Application";

            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, log);
            }

            EventLog.WriteEntry(source, message, EventLogEntryType.Information);
        }
    }
}
