using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CLR.RaceConditions
{
    public class AsyncOneMangLock
    {
        public enum OneMangMode { Exclusive,Shared}
        private SpinLock spinLock = new SpinLock();
        private Boolean lockToken=true;
        private void Lock() { spinLock.Enter(ref lockToken); }
        private void UnLock() { spinLock.Exit(); }

        private Int32 m_state = 0;
        private Boolean IsFree { get { return m_state == 0; } }
        private Boolean IsOwnedByReaders { get { return m_state > 0; } }
        private Boolean IsOwnedByWriter { get { return m_state == -1; } }

        private Int32 AddReaders(Int32 count) { return m_state = +count; }
        private Int32 SubtractReader() { return --m_state; }
        private void MakeFree() { m_state = 0; }
        private void MakeWriter() { m_state = -1; }

        private readonly Task m_noContentionAccessGranter;
        //等待写任务队列
        private readonly Queue<TaskCompletionSource<Object>> m_qWaitingWriters = new Queue<TaskCompletionSource<object>>();
        private  TaskCompletionSource<object> m_waitingReadersSignal = new TaskCompletionSource<object>();
        private Int32 m_numWaitingReaders = 0;
        public AsyncOneMangLock()
        {
            m_noContentionAccessGranter = Task.FromResult<Object>(null);
        }
        public Task AsyncWait(OneMangMode mode)
        {
            Task accressGranter = m_noContentionAccessGranter;
            Lock();
            switch (mode)
            {
                case OneMangMode.Exclusive:
                    if (IsFree)
                        MakeWriter();
                    var tcs = new TaskCompletionSource<Object>();
                    m_qWaitingWriters.Enqueue(tcs);
                    accressGranter = tcs.Task;
                    break;
                case OneMangMode.Shared:
                    if (IsFree || (IsOwnedByReaders && m_qWaitingWriters.Count == 0))
                        AddReaders(1);
                    else
                    {
                        m_numWaitingReaders++;
                        accressGranter = m_waitingReadersSignal.Task.ContinueWith(t => t.Result);
                    }
                    break;
                default:
                    break;
            }
            UnLock();
            return accressGranter;
        }
        public void Release()
        {
            TaskCompletionSource<object> accessGranter = null;
            Lock();
            if (IsOwnedByWriter)
                MakeFree();
            else
                SubtractReader();
            if(IsFree)
            {
                if(m_qWaitingWriters.Count>0)
                {
                    MakeWriter();
                    accessGranter = m_qWaitingWriters.Dequeue();
                }
                else if(m_numWaitingReaders>0)
                {
                    AddReaders(m_numWaitingReaders);
                    m_numWaitingReaders = 0;
                    accessGranter = m_waitingReadersSignal;
                    m_waitingReadersSignal = new TaskCompletionSource<object>();
                }
            }
            UnLock();
            if (accessGranter != null)
                accessGranter.SetResult(null);
        }
    }
}
