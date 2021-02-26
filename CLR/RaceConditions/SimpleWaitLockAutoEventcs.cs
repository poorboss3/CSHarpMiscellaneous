using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace CLR.RaceConditions
{
    public sealed class SimpleWaitLockAutoEventcs:IDisposable
    {
        private readonly AutoResetEvent m_lock;
        public SimpleWaitLockAutoEventcs()
        {
            m_lock = new AutoResetEvent(true);
        }

        public void Dispose()
        {
            m_lock.Dispose();
        }

        public void Enter()
        {
            m_lock.WaitOne();
        }
        public void Leavr()
        {
            m_lock.Set();
        }
    }
}
