using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CLR.RaceConditions
{
    public class SimpleWaitLockSemaphore:IDisposable
    {
        private readonly Semaphore semaphore;

        public SimpleWaitLockSemaphore()
        {
            semaphore = new Semaphore(Int32.MaxValue, Int32.MaxValue);
        }

        public void Enter()
        {
            semaphore.WaitOne();
        }
        public void Leave()
        {
            semaphore.Release(1);
        }
        public  void Dispose()
        {
            semaphore.Dispose();
        }
    }
}
