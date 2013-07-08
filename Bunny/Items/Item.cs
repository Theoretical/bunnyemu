using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bunny.Items
{
    class Item
    {
        public Int32 ItemCid;
        public Int32 ItemId;
        public Int32 RentHour = 525600;
        public Int32 Price;
        public byte Sex;
        public byte Level;
        public Int32 Weight;
        public Int32 MaxWeight;
        public Int32 Quantity;
        public bool ShopItem;
    }
}
