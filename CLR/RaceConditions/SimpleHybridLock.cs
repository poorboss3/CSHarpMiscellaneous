using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CLR.RaceConditions
{
    public class SimpleHybridLock : IDisposable
    {
        private readonly AutoResetEvent m_lock;
        private Int32 m_waiters=0;

        public SimpleHybridLock()
        {
            m_lock = new AutoResetEvent(true);
        }

        public void Enter()
        {
            if(Interlocked.Increment(ref m_waiters)==1)
            {
                return;
            }
            m_lock.WaitOne();
        }

        public void Leave()
        {
            if (Interlocked.Decrement(ref m_waiters) == 0)
                return;
            m_lock.Set();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
