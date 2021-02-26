using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CLR.RaceConditions
{
    public class MutilHybridLock : IDisposable
    {
        private readonly AutoResetEvent m_lock;
        private Int32 m_waiters;
        private Int32 m_ownerThreadID, m_recursive = 0;
        private const Int32 recursivecount = 4000;

        public MutilHybridLock()
        {
            m_lock = new AutoResetEvent(true);
        }

        public void Enter()
        {
            var currentThreadID = Thread.CurrentThread.ManagedThreadId;
            if (m_ownerThreadID == currentThreadID)
            {
                m_recursive++;
                return;
            }
            SpinWait spinWait = new SpinWait();
            for (int i = 0; i < recursivecount; i++)
            {
                if (Interlocked.CompareExchange(ref m_waiters, 1, 0) == 0)
                {
                    goto GetLock;
                }
                spinWait.SpinOnce();
            }
            if (Interlocked.Increment(ref m_waiters) > 1)
            {
                m_lock.WaitOne();
            }
            GetLock:
            m_ownerThreadID = currentThreadID;
            m_recursive = 1;
        }

        private void Leave()
        {
            var currentThreadID = Thread.CurrentThread.ManagedThreadId;
            if (currentThreadID != m_ownerThreadID)
                throw new InvalidOperationException();
            if (--m_recursive > 0)
            {
                return;
            }
            currentThreadID = 0;
            if(Interlocked.Decrement(ref m_waiters)==0)
            {
                return;
            }
            m_lock.Set();
        }
        public void Dispose()
        {
            m_lock.Dispose();
        }
    }
}
