using Bunny.Core;
using Bunny.Enums;
using Bunny.Items;
using Bunny.Packet.Assembled;
using System;

namespace Bunny.Packet.Disassemble
{
    class ItemHandler
    {
        [PacketHandler(Operation.MatchRequestShopItemList, PacketFlags.Character)]
        public static void ProcessShopItemList(Client client, PacketReader packetReader)
        {
            Match.ResponseShopItemList(client);
        }

        [PacketHandler(Operation.MatchRequestCharacterItemList, PacketFlags.Character)]
        public static void ProcessCharItemList(Client client, PacketReader packetReader)
        {
            Match.ResponseCharacterItemList(client);
        }

        [PacketHandler(Operation.MatchRequestBuyItem, PacketFlags.Character)]
        public static void ProcessBuyItem(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadUInt64();
            var itemid = packetReader.ReadInt32();
            Results result = Results.Accepted;

            Items.Item item = ItemList.Find(itemid);
            if (item == null)
                result = Results.ShopItemNonExistant;
            else if ((item.Price > client.GetCharacter().Bp && Globals.Config.Items.UseBounty) ||  item.Price > client.GetCharacter().Bp && Globals.Config.Items.UseBounty)
                result = Results.ShopInsufficientBounty;
            else if (client.GetCharacter().Items.Count == Globals.Config.Character.MaxItems)
                result = Results.ShopInventoryFull;
            else
            {
                var temp = new Items.Item();
                temp.ItemId = item.ItemId;
                temp.Level = item.Level;
                temp.MaxWeight = item.MaxWeight;
                temp.Weight = item.Weight;
                temp.Price = item.Price;
                temp.Quantity = 1;
                temp.ItemCid = Globals.GunzDatabase.AddItem(client.GetCharacter().CharacterId, item.ItemId, 1);
                client.GetCharacter().Items.Add(temp);

                if (Globals.Config.Items.UseBounty)
                {
                    client.GetCharacter().Bp -= item.Price;
                    Globals.GunzDatabase.UpdateBp(client.GetCharacter().Bp, client.GetCharacter().CharacterId);
                }
            }

            Match.ResponseBuyItem(client, result);
            Match.ResponseCharacterItemList(client);
        }

        [PacketHandler(Operation.MatchRequestSellItem, PacketFlags.Character)]
        public static void ProcessSellItem(Client client, PacketReader packetReader)
        {
            var uidChar = packetReader.ReadUInt64();
            var low = packetReader.ReadInt32();
            var high = packetReader.ReadInt32();
            var result = Results.Accepted;

            var item = client.GetCharacter().Items.Find(i => i.ItemCid == high);
            if (item == null)
                result = Results.ShopItemNonExistant;
            else
            {
                Globals.GunzDatabase.DeleteItem(item.ItemCid);
                client.GetCharacter().Bp += item.Price;
                Globals.GunzDatabase.UpdateBp(client.GetCharacter().Bp, client.GetCharacter().CharacterId);
                client.GetCharacter().Items.Remove(item);
            }
            
            Match.ResponseSellItem(client, result);
            Match.ResponseCharacterItemList(client);
        }

        [PacketHandler(Operation.MatchRequestEquipItem, PacketFlags.Character)]
        public static void ProcessEquipItem(Client client, PacketReader packetReader)
        {
            var uidChar = packetReader.ReadUInt64();
            var nItemLow = packetReader.ReadInt32();
            var nItemHigh = packetReader.ReadInt32();
            var nItemSlot = packetReader.ReadInt32();
            Results result = Results.Accepted;

            if (!Enum.IsDefined(typeof(ItemSlotType), nItemSlot))
            {
                client.Disconnect();
                return;
            }

            Items.Item nItem = client.GetCharacter().Items.Find(i => i.ItemCid == nItemHigh);
            if (nItem == null)
                result = Results.ShopItemNonExistant;
            else if (nItem.Level > client.GetCharacter().Level)
            {
                result = Results.ShopLevelTooLow;
            }
            else if ((ItemSlotType)nItemSlot == ItemSlotType.primary_slot && nItem.ItemId == client.GetCharacter().EquippedItems[(int)ItemSlotType.secondary_slot].ItemId)
            {
                result = Results.ShopInvalidItem;
            }
            else if ((ItemSlotType)nItemSlot == ItemSlotType.secondary_slot && nItem.ItemId == client.GetCharacter().EquippedItems[(int)ItemSlotType.primary_slot].ItemId)
            {
                result = Results.ShopInvalidItem;
            }
            else
            {
                client.GetCharacter().EquippedItems[nItemSlot].ItemCid = nItemHigh;
                client.GetCharacter().EquippedItems[nItemSlot].ItemId = nItem.ItemId;
                Globals.GunzDatabase.UpdateSlot(client.GetCharacter().CharacterId, (ItemSlotType)nItemSlot, nItemHigh);
            }

            Match.ResponseEquipItem(client, result);
            Match.ResponseCharacterItemList(client);
        }

        [PacketHandler(Operation.MatchRequestTakeOffItem, PacketFlags.Character)]
        public static void ProcessTakeOffItem(Client client, PacketReader packetReader)
        {
            var uidChar = packetReader.ReadUInt64();
            var nItemSlot = packetReader.ReadInt32();

            if (!Enum.IsDefined(typeof(ItemSlotType), nItemSlot))
            {
                client.Disconnect();
                return;
            }
            client.GetCharacter().EquippedItems[nItemSlot].ItemCid = 0;
            client.GetCharacter().EquippedItems[nItemSlot].ItemId = 0;
            Globals.GunzDatabase.UpdateSlot(client.GetCharacter().CharacterId, (ItemSlotType)nItemSlot, 0);

            Match.ResponseTakeOffItem(client);
            Match.ResponseCharacterItemList(client);
        }

    }
}
