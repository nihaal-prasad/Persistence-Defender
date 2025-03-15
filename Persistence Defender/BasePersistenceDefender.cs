using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence_Defender
{
    public abstract class BasePersistenceDefender
    {
        protected int Mode;

        protected BasePersistenceDefender(int mode)
        {
            Mode = mode;
        }

        public abstract void StartDefender();
        public abstract void StopDefender();
    }
}
