using System;

namespace Bunny.Packet
{
    class PacketCrypt
    {
        public static UInt16 CalculateChecksum(byte[] buf, int index, int length)
        {
            var intermediateValues = new UInt32[4];

            for (var i = 0; i < 4; ++i)
                intermediateValues[0] += buf[index + i];

            for (var i = 6; i < length; ++i)
                intermediateValues[1] += buf[index + i];

            intermediateValues[2] = intermediateValues[1] - intermediateValues[0];
            intermediateValues[3] = intermediateValues[2] >> 0x10;

            return (UInt16)(intermediateValues[2] + intermediateValues[3]);
        }


        public static void Decrypt(byte[] buf, int index, int length, byte[] key)
        {
            for (var i = 0; i < length; ++i)
            {
                var a = buf[index + i];
                a ^= 0x0F0;
                var b = (byte)(a & 0x1f);
                a >>= 5;
                b <<= 3;
                b = (byte)(a | b);
                buf[index + i] = (byte)(b ^ key[i % 32]);
            }
        }

        public static void Encrypt(byte[] buf, int index, int length, byte[] key)
        {
            for (var i = 0; i < length; ++i)
            {
                ushort a = buf[index + i];
                a ^= key[i % 32];
                a <<= 5;

                var b = (byte)(a >> 8);
                b |= (byte)(a & 0xFF);
                b ^= 0xF0;
                buf[index + i] = b;
            }
        }
    }
}
