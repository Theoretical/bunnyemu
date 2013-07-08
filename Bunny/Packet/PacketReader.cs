using System;
using System.IO;
using System.Text;
using Bunny.Core;
using Bunny.Enums;

namespace Bunny.Packet
{
    sealed class PacketReader : BinaryReader
    {
        private Operation Opcode;
        public Operation GetOpcode() { return Opcode; }

        public PacketReader(byte[] pBuffer, int nSize) :
            base(new MemoryStream(pBuffer, 0, nSize, false, true))
        {
            BaseStream.Position = 8;
            Opcode = (Operation)ReadUInt16();
            ReadByte();
        }

        public override string ReadString()
        {
            var len = ReadUInt16();
            if (len < 1)
                return String.Empty;

            var buffer = new byte[len];
            buffer = ReadBytes(len);
            var pString = Encoding.GetEncoding(1252).GetString(buffer);
            return pString.Substring(0, pString.IndexOf('\0'));
        }

        public string ReadString(int len)
        {
            var buffer = new byte[len];
            var pString = "";
            var i = 0;

            Read(buffer, 0, len);
            for (; i < len; ++i)
                if (buffer[i] == 0)
                    break;
            pString = Encoding.GetEncoding(1252).GetString(buffer, 0, i);
            return pString;
        }

        public Muid ReadMuid()
        {
            Muid uid = new Muid();
            var first = ReadInt32();
            var second = ReadInt32();

            uid.LowId = first;
            uid.HighId = second;

            return uid;
        }
    }
}
