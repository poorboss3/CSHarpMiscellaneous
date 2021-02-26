using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

namespace CLR.RaceConditions
{
    public class OneManyLock
    {
        private enum OneMangLockState
        {
            Free = 0x00000000,
            OwnedByWriter = 0x00000001,
            OwnedByReader = 0x00000002,
            OwnedByReaderAndWriterPending = 0x00000003,
            ReservedForWriter = 0x00000004,
        }
        private static OneMangLockState State(Int32 value)
        {
            return (OneMangLockState)s_state.Get(value);
        }

        public enum LockModel
        {
            Exclusive, Shared
        }

        private static void SetState(ref Int32 ls, OneMangLockState state)
        {
            s_state.Set(ls, (Int32)state);
        }
        private const Int32 c_lsStateStartBit = 0;
        private const Int32 c_lsReadersReadingStartBit = 3;
        private const Int32 c_lsReadersWaitingStartBit = 12;
        private const Int32 c_lsWritersWaitingStartBit = 21;
        private static BitField s_state = new BitField(c_lsStateStartBit, 3);
        private static BitField s_readersReading = new BitField(c_lsReadersReadingStartBit, 9);
        private static BitField s_readersWaiting = new BitField(c_lsReadersWaitingStartBit, 9);
        private static BitField s_writersWaiting = new BitField(c_lsWritersWaitingStartBit, 9);

        private static Int32 c_lsReadersReadingMask = s_readersReading.GetMask();
        private static Int32 c_lsReadersWaitingMask = s_readersWaiting.GetMask();
        private static Int32 c_lsWritersWaitingMask = s_writersWaiting.GetMask();
        private static Int32 c_lsAnyWaitingMask = c_lsReadersWaitingMask | c_lsWritersWaitingMask;


        private static Int32 NumReadersReading(Int32 ls)
        {
            return (ls & c_lsReadersReadingMask) >> c_lsReadersReadingStartBit;
        }
        private static void AddReadersReading(ref Int32 ls, Int32 amount)
        {
            ls += s_readersReading.GetUnit() * amount;
        }


        private static Int32 NumReadersWaiting(Int32 ls) { return (ls & c_lsReadersWaitingMask) >> c_lsReadersWaitingStartBit; }
        private static void AddReadersWaiting(ref Int32 ls, Int32 amount) { ls += (s_readersWaiting.GetUnit() * amount); }

        private static Int32 NumWritersWaiting(Int32 ls) { return (ls & c_lsWritersWaitingMask) >> c_lsWritersWaitingStartBit; }
        private static void AddWritersWaiting(ref Int32 ls, Int32 amount) { ls += (s_writersWaiting.GetUnit() * amount); }

        private static Boolean AnyWaiters(Int32 ls) { return (ls & c_lsAnyWaitingMask) != 0; }

        private Int32 m_lockState = (Int32)OneMangLockState.Free;
        private LockModel m_lockModel;
        private Semaphore m_readersLock = new Semaphore(0, Int32.MaxValue);
        private Semaphore m_writersLock = new Semaphore(0, Int32.MaxValue);


        public void Enter(LockModel model)
        {
            switch (model)
            {
                case LockModel.Exclusive:
                    if (WaitToWrite(ref m_lockState))
                        m_writersLock.WaitOne();
                    break;
                case LockModel.Shared:
                    if (WaitToRead(ref m_lockState))
                        m_readersLock.Release();
                    break;
                default:
                    Debug.Assert(false, "Invalid Lock Model");
                    break;
            }
            m_lockModel = model;
        }

        public void Leave()
        {
            Int32 wakeup;
            switch (m_lockModel)
            {
                case LockModel.Exclusive:
                    Debug.Assert((State(m_lockState) == OneMangLockState.OwnedByWriter && NumReadersReading(m_lockState) == 0));
                    wakeup = DoneWriting(ref m_lockState);
                    break;
                case LockModel.Shared:
                    Debug.Assert((State(m_lockState) == OneMangLockState.OwnedByReader || (State(m_lockState) == OneMangLockState.OwnedByReaderAndWriterPending)));
                    wakeup = DoneReading(ref m_lockState);
                    break;
                default:
                    Debug.Assert(false, "Invalid Lock Model");
                    wakeup = 0;
                    break;
            }
            if (wakeup == -1)
                m_writersLock.Release();
            else if (wakeup > 0)
                m_readersLock.Release(wakeup);
        }
        private Boolean WaitToWrite(ref Int32 target)
        {
            Int32 start, current = target;
            Boolean isWait = false;
            do
            {
                start = current;
                Int32 desire = start;
                switch (State(desire))
                {
                    case OneMangLockState.Free:
                    case OneMangLockState.ReservedForWriter:
                        SetState(ref desire, OneMangLockState.OwnedByWriter);
                        break;
                    case OneMangLockState.OwnedByWriter:
                        AddWritersWaiting(ref desire, 1);
                        isWait = true;
                        break;
                    case OneMangLockState.OwnedByReader:
                    case OneMangLockState.OwnedByReaderAndWriterPending:
                        SetState(ref desire, OneMangLockState.OwnedByReaderAndWriterPending);
                        AddWritersWaiting(ref desire, 1);
                        isWait = true;
                        break;
                    default:
                        Debug.Assert(false, "Invalid Lock State");
                        break;
                }
                current = Interlocked.CompareExchange(ref target, desire, start);
            } while (start != current);
            return isWait;
        }
        private Boolean WaitToRead(ref Int32 target)
        {
            Int32 start, current = target;
            Boolean isWait = false;
            do
            {
                Int32 desire = (start = current);
                switch (State(desire))
                {
                    case OneMangLockState.Free:
                    
                        SetState(ref desire, OneMangLockState.OwnedByReader);
                        isWait = false;
                        break;
                    case OneMangLockState.OwnedByReader:
                        isWait = false;
                        AddReadersReading(ref desire, 1);
                        break;
                    case OneMangLockState.OwnedByWriter:
                    case OneMangLockState.OwnedByReaderAndWriterPending:
                    case OneMangLockState.ReservedForWriter:
                        AddReadersWaiting(ref desire, 1);
                        isWait = true;
                        break;
                    default:
                        Debug.Assert(false, "Invalid Lock State");
                        break;
                }
                current = Interlocked.CompareExchange(ref target, desire, start);
            } while (start != current);
            return isWait;
        }
        private Int32 DoneReading(ref Int32 target)
        {
            Int32 start, current = target;
            Int32 wakeup = 0;
            do
            {
                Int32 desire = (start = current);
                AddReadersReading(ref desire, -1);
                if(NumReadersReading(desire)>0)
                {
                    wakeup = 0;
                }else if (!AnyWaiters(desire))
                {
                    SetState(ref desire, OneMangLockState.Free);
                    wakeup = 0;
                }
                else
                {
                    Debug.Assert(NumWritersWaiting(desire) > 0);
                    SetState(ref desire, OneMangLockState.OwnedByWriter);
                    wakeup = -1;
                    AddWritersWaiting(ref desire, -1);
                }
                current = Interlocked.CompareExchange(ref target, desire, start);
            } while (start != current);
            return wakeup;
        }

        private Int32 DoneWriting(ref Int32 target)
        {
            Int32 start, current = target;
            Int32 wakeup = 0;
            do
            {
                Int32 desire = (start = current);
                if(!AnyWaiters(desire))
                {
                    SetState(ref desire, OneMangLockState.Free);
                    wakeup = 0;
                }else if (NumWritersWaiting(desire) > 0)
                {
                    SetState(ref desire, OneMangLockState.ReservedForWriter);
                    wakeup = -1;
                    AddWritersWaiting(ref desire, -1);
                }
                else
                {
                    wakeup = NumReadersWaiting(desire);
                    Debug.Assert(wakeup > 0);
                    SetState(ref desire, OneMangLockState.OwnedByReader);
                    AddReadersReading(ref desire, wakeup);
                }
                current = Interlocked.CompareExchange(ref target, desire, start);
            } while (start != current);
            return wakeup;
        }
        private static String DebugState(Int32 ls)
        {
            return String.Format(CultureInfo.InvariantCulture, "State={0},RR={1},RW={2},WW={3}",
                State(ls), NumReadersReading(ls), NumReadersWaiting(ls), NumWritersWaiting(ls));
        }
        public override string ToString()
        {
            return DebugState(m_lockState);
        }
    }
}
