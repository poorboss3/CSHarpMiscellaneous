using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
namespace CLR.RaceConditions
{
    public class RecursiveAutoRestEvent : IDisposable
    {
        private readonly AutoResetEvent m_lock;

        private Int32 m_ownerThreadID;
        private Int32 m_recursiveAmount;
        public RecursiveAutoRestEvent()
        {
            m_lock = new AutoResetEvent(true);
        }

        public void Enter()
        {
            var currentThread = Thread.CurrentThread.ManagedThreadId;
            if(currentThread==m_ownerThreadID)
            {
                m_recursiveAmount++;
                return;
            }
            m_lock.WaitOne();
            m_ownerThreadID = currentThread;
            m_recursiveAmount = 1;
        }

        public void Leave()
        {
            var currentThread = Thread.CurrentThread.ManagedThreadId;
            if (currentThread != m_ownerThreadID)
                throw new InvalidOperationException();
            if(--m_recursiveAmount==0)
            {
                m_lock.Set();
                m_ownerThreadID = 0;
            }
        }

        void IDisposable.Dispose()
        {
            m_lock.Dispose();
        }
    }
}
