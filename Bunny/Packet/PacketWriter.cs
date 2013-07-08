using System;
using System.IO;
using System.Text;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Players;

namespace Bunny.Packet
{
    sealed class PacketWriter : BinaryWriter
    {
        public CryptFlags Flags { get; internal set; }
        public UInt16 Operation { get; internal set; }

        public PacketWriter(Operation operation, CryptFlags flag)
            : base(new MemoryStream(4096))
        {
            Flags = flag;
            Operation = (UInt16)operation;

            Write((UInt16)operation);
            Write((byte)0);
        }

        public override void Write(string value)
        {
            Write((UInt16)(value.Length + 2));
            var buf = new byte[value.Length + 2];
            Encoding.GetEncoding(1252).GetBytes(value, 0, value.Length, buf, 0);
            Write(buf);
        }

        public void Write(string pString, int pLength)
        {
            if (pString == null) pString = "";
            if (pString.Length > pLength)
                throw new InsufficientMemoryException("Could not write string.");

            var buf = new byte[pLength];
            var used = Encoding.GetEncoding(1252).GetBytes(pString, 0, pString.Length, buf, 0);
            var unused = Math.Min(pLength - 1, used);
            Array.Clear(buf, unused, Math.Max(1, pLength - unused));
            Write(buf);
        }

        public void Write(int pCount, int pSize)
        {
            var total = (pCount * pSize) + 8;
            Write(total);
            Write(pSize);
            Write(pCount);
        }

        public void Write(CharacterInfo charInfo, bool bObfuscation = true)
        {
            Write(1, 374);
            Write(charInfo.Name, 32);
            Write(charInfo.ClanName, 16);
            Write((Int32)charInfo.ClanGrade);
            Write(charInfo.ClanPoint);
            Write(charInfo.CharNum);
            Write(charInfo.Level);
            Write(charInfo.Sex);
            Write(charInfo.Hair);
            Write(charInfo.Face);
            Write(charInfo.Xp);
            Write(charInfo.Bp);
            Write(charInfo.BonusRate);
            Write(charInfo.Prize);
            Write(charInfo.Hp);
            Write(charInfo.Ap);
            Write(charInfo.MaxWeight);
            Write(charInfo.SafeFalls);
            Write(charInfo.Fr);
            Write(charInfo.Cr);
            Write(charInfo.Er);
            Write(charInfo.Wr);
            for (int i = 0; i < 17; i++)
                Write(charInfo.EquippedItems[i].ItemId);
            Write((Int32)charInfo.UGrade);
            Write(charInfo.ClanId);
            if (bObfuscation)
            {
                byte[] sequence = { 0xFF, 0xFF, 0xFF, 0xFF };
                Write(sequence);
                WriteSkip(204);
            }
            else
            {
                Write((int)0);

                //Since faggots will be faggots.
                //This is acctually a list: UInt64[17] CIID, UInt32[17] Quantity
                byte[] obfuscation = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x79, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7B, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x6E, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x6D, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x6C, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x49, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4B, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x72, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4C, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4A, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x4C, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                Log.Write(obfuscation.Length.ToString());
                Write(obfuscation);
            }

        }


        public void WriteSkip(int pCount)
        {
            Write(new byte[pCount]);
        }

        public void Write(Muid uid)
        {
            Write(uid.LowId);    
            Write(uid.HighId);
        }
        public byte[] Process(byte pCount, byte[] bEncrypt)
        {
            var totalSize = (int)(BaseStream.Length + 8);
            var buffer = new byte[totalSize];
            BaseStream.Position = 0;
            BaseStream.Read(buffer, 8, (totalSize - 8));

            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)Flags), 0, buffer, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)totalSize), 0, buffer, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)0), 0, buffer, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)(totalSize - 6)), 0, buffer, 6, 2);
            buffer[10] = pCount;

            if (Flags == CryptFlags.Encrypt)
            {
                PacketCrypt.Encrypt(buffer, 2, 2, bEncrypt);
                PacketCrypt.Encrypt(buffer, 6, (totalSize - 6), bEncrypt);
            }

            Buffer.BlockCopy(BitConverter.GetBytes(PacketCrypt.CalculateChecksum(buffer, 0, totalSize)), 0, buffer, 4, 2);
            return buffer;
        }
    }
}
