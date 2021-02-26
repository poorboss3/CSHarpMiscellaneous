using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CLR.ThreadingAndRaceConditions
{
    public struct SpinLock
    {
        private Int32 m_lock;

        public void Enter()
        {
            while (true)
            {
                if (Interlocked.Exchange(ref m_lock,1) == 0)
                    return;
            }
        }

        public void Leave()
        {
            Volatile.Write(ref m_lock, 0);
        }
    }
}
