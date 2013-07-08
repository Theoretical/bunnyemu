using System;

namespace Bunny.Enums
{
    public enum ItemType
    {
        Melee,
        Range,
        Equipment,
        Custom,
        None
    }

    public enum ItemSlotType
    {
        head_slot,
        chest_slot,
        hands_slot,
        legs_slot,
        feet_slot,
        fingerl_slot,
        fingerr_slot,
        melee_slot,
        primary_slot,
        secondary_slot,
        custom1_slot,
        custom2_slot,
        avatar_slot,    
        community1_slot,
        community2_slot,
        longbuff1_slot,
        longbuff2_slot
    }

    public class Position
    {
        public float X;
        public float Y;
        public float Z;
    }

    public class Direction : Position {}

    public class ItemSpawn : ICloneable
    {
        public Position Position = new Position();
        public Int32 ItemId;
        public Int32 SpawnTime;
        public DateTime NextSpawn = DateTime.Now;
        public bool Taken = true;
        public bool Exist;
        public Int32 ItemUid;

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public ItemSpawn Clone()
        {
            return (ItemSpawn)this.MemberwiseClone();
        }
    }
}
