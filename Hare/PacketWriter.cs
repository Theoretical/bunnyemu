﻿using System;
using System.IO;
using System.Text;

namespace Hare
{
    sealed class PacketWriter : BinaryWriter
    {
        public byte Flags { get; internal set; }
        public UInt16 Operation { get; internal set; }

        public PacketWriter(UInt16 operation, byte flag)
            : base(new MemoryStream(4096))
        {
            Flags = flag;
            Operation = (UInt16)operation;

            this.Write((UInt16)operation);
            this.Write((byte)0);
        }
        public override void Write(string value)
        {
            if (value == null) value = "";
            Write((UInt16)(value.Length + 2));
            var buf = new byte[value.Length + 2];
            Encoding.GetEncoding(1252).GetBytes(value, 0, value.Length, buf, 0);
            base.Write(buf);
        }

        public void Write(string pString, int pLength)
        {
            if (pString == null) pString = "";
            if (pString.Length > pLength)
                throw new Exception("Could not write string.");

            byte[] buf = new byte[pLength];
            var used = Encoding.GetEncoding(1252).GetBytes(pString, 0, pString.Length, buf, 0);
            var unused = Math.Min(pLength - 1, used);
            Array.Clear(buf, unused, Math.Max(1, pLength - unused));
            this.Write(buf);
        }

        public void Write(int pCount, int pSize)
        {
            var total = (pCount * pSize) + 8;
            this.Write(total);
            this.Write(pSize);
            this.Write(pCount);
        }


        public void WriteSkip(int pCount)
        {
            this.Write(new byte[pCount]);
        }

        public static UInt16 CalculateChecksum(byte[] buf, int index, int length)
        {
            UInt32[] intermediateValues = new UInt32[4];

            for (int i = 0; i < 4; ++i)
                intermediateValues[0] += buf[index + i];

            for (int i = 6; i < length; ++i)
                intermediateValues[1] += buf[index + i];

            intermediateValues[2] = intermediateValues[1] - intermediateValues[0];
            intermediateValues[3] = intermediateValues[2] >> 0x10;

            return (UInt16)(intermediateValues[2] + intermediateValues[3]);
        }

        public byte[] Process(byte pCount, byte[] bEncrypt)
        {
            var totalSize = (int)(this.BaseStream.Length + 8);
            var buffer = new byte[totalSize];
            this.BaseStream.Position = 0;
            this.BaseStream.Read(buffer, 8, (int)(totalSize - 8));

            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)Flags), 0, buffer, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)totalSize), 0, buffer, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)0), 0, buffer, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)(totalSize - 6)), 0, buffer, 6, 2);
            buffer[10] = pCount;

            Buffer.BlockCopy(BitConverter.GetBytes(CalculateChecksum(buffer, 0, totalSize)), 0, buffer, 4, 2);
            return buffer;
        }
    }
}
