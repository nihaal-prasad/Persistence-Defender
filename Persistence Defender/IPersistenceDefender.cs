using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence_Defender
{
    internal interface IPersistenceDefender
    {
        void StartDefender();
        void StopDefender();
    }
}
