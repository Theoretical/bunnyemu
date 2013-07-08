using System;
using System.Threading;

namespace Bunny.Core
{
    class Muid
    {
        public Int32 LowId;
        public Int32 HighId;

        public Muid()
        {

        }

        public Muid(Int32 first, Int32 second)
        {
            LowId = first;
            HighId = second;
        }

        public static bool operator !=(Muid uid1, Muid uid2)
        {
            return !(uid1 == uid2);
        }

        public static bool operator ==(Muid uid1, Muid uid2)
        {
            return uid1.LowId == uid2.LowId && uid1.HighId == uid2.HighId;  
        }

        public override bool Equals(object obj)
        {
            var uid = (Muid) obj;
            return uid.LowId == LowId && uid.HighId == HighId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    class MuidWrapper
    {
        private Int32 _firstCounter;
        private Int32 _secondCounter;

        public Muid GetNext()
        {
            if (_secondCounter > Int32.MaxValue)
            {
                Interlocked.Increment(ref _firstCounter);
                _secondCounter = 1;
            }
            else
            {
                Interlocked.Increment(ref _secondCounter);
            }

            return new Muid(_firstCounter, _secondCounter);
        }
    }
}
