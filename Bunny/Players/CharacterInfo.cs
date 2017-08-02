using System;
using System.Collections.Generic;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Items;

namespace Bunny.Players
{
    class CharacterInfo
    {
        public Int32 CharacterId;
        public string Name = "";
        public string ClanName = "";
        public ClanGrade ClanGrade;
        public Int16 ClanPoint;
        public byte CharNum;
        public Int16 Level;
        public byte Sex;
        public byte Hair;
        public byte Face;
        public UInt32 Xp;
        public Int32 Bp;
        public Single BonusRate;
        public Int16 Prize;
        public Int16 Hp;
        public Int16 Ap;
        public Int16 MaxWeight = 0;
        public Int16 SafeFalls;
        public Int16 Fr;
        public Int16 Cr;
        public Int16 Er;
        public Int16 Wr;
        public Item[] EquippedItems = new Item[17];
        public UGradeId UGrade;
        public Int32 ClanId;
        public List<Item> Items = new List<Item>();
        public Int32 Rank;
        public Int32 Kills;
        public Int32 Deaths;

        public CharacterInfo()
        {
            MaxWeight = (Int16) Globals.Config.Items.MaxWeight;
        }
    }
}
