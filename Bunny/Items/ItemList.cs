using System;
using System.Collections.Generic;
using System.Xml;
using Bunny.Core;

namespace Bunny.Items
{
    class ItemList
    {
        private static readonly List<Item> Items = new List<Item>();

        public static void AddItem(Item i)
        {
            Items.Add(i);
        }

        public static void Remove(Item i)
        {
            Items.Remove(i);
        }

        public static Item Find(int itemId)
        {
            return Items.Find(i => i.ItemId == itemId);
        }

        public static void SetShop(int id)
        {
            var item = Items.Find(i => i.ItemId == id);

            if (item != null)
                item.ShopItem = true;
        }

        public static List<Item> GetShopItems(int sex)
        {
            return Items.FindAll(i => i.ShopItem && (i.Sex == sex || i.Sex == 2));
        }

        public static void Load()
        {
            using (var reader = new XmlTextReader("zitem.xml"))
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "ITEM":
                            var item = new Item();
                            item.ItemId = Int32.Parse(reader.GetAttribute("id"));
                            item.Level = (byte) Int32.Parse(reader.GetAttribute("res_level"));
                            item.Weight = Int32.Parse(reader.GetAttribute("weight"));
                            item.MaxWeight = reader.GetAttribute("maxwt") == null
                                                 ? 0
                                                 : Int32.Parse(reader.GetAttribute("maxwt"));
                            item.Price = reader.GetAttribute("bt_price") == null
                                             ? 0
                                             : Int32.Parse(reader.GetAttribute("bt_price"));
                            switch (reader.GetAttribute("res_sex"))
                            {
                                case "m":
                                    item.Sex = 0;
                                    break;
                                case "f":
                                    item.Sex = 1;
                                    break;
                                case "a":
                                    item.Sex = 2;
                                    break;
                            }

                            Items.Add(item);
                            break;
                    }
                }
            }

            using (var reader = new XmlTextReader("shop.xml"))
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "SELL":
                            var itemId = (UInt32.Parse(reader.GetAttribute("itemid")));
                            var item = Items.Find(i => i.ItemId == itemId);

                            if (item == null)
                            {
                                Log.Write("Found non-existing item in shop: {0}", itemId);
                                continue;
                            }

                            item.ShopItem = true;
                            break;
                    }
                }
            }
        }
    }
}
