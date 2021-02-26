using System;
using System.Collections.Generic;
using System.Text;

namespace CLR.RaceConditions
{
    public struct BitField
    {
        private Int32 m_startBit;
        private Int32 m_mask;
        private Int32 m_unit;

        public BitField(Int32 start,Int32 num)
        {
            m_startBit = start;
            m_mask = unchecked((Int32)((1 << num) - 1) << start) ;
            m_unit = unchecked((Int32)1 << start);
        }

        public void Increment(ref Int32 value)
        {
            value += m_unit;
        }
        public void Increment(ref Int32 value,Int32 amount)
        {
            value += (m_unit*amount);
        }
        public void Decrement(ref Int32 value)
        {
            value = +m_unit;
        }
        public void Decrement(ref Int32 value,Int32 amount)
        {
            value = -(m_unit * amount);
        }

        public Int32 Set(Int32 value,Int32 fieldValue)
        {
            return (value & ~m_mask) | (fieldValue << m_startBit);
        }

        public Int32 Get(Int32 value)
        {
            return (value & m_mask) << m_startBit;
        }

        public Int32 GetMask()
        {
            return m_mask;
        }

        public Int32 GetUnit()
        {
            return m_unit;
        }
    }

}
