using System;
using System.Collections.Generic;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Items;
using Bunny.Network;
using Bunny.Utility;

namespace Bunny.Packet.Assembled
{
    class Match
    {
        public static void ResponseLogin (Client client, Results results,  string user, UGradeId ugrade, PGradeId pgrade, Muid playerId)
        {
            using (var packetWriter = new PacketWriter(Operation.MatchLoginResponse, CryptFlags.Encrypt))
            {
                packetWriter.Write((Int32)results);
                packetWriter.Write(Globals.Config.Server.Name);

                switch (Globals.Config.Server.Mode.ToLower())
                {
                    case "match":
                        packetWriter.Write((byte)0);
                        break;

                    case "clan":
                        packetWriter.Write((byte)1);
                        break;
                    
                    case "quest":
                        packetWriter.Write((byte)4);
                        break;;

                    case "test":
                        packetWriter.Write((byte)4);
                        break;

                    case "develop":
                        packetWriter.Write((byte)3);
                        break;
                    default:
                        packetWriter.Write((byte)1);
                        break;
                }

                packetWriter.Write(user);
                packetWriter.Write((byte)ugrade);
                packetWriter.Write((byte)pgrade);
                packetWriter.Write(playerId);
                packetWriter.Write(Globals.Config.Server.Survival);
                packetWriter.Write(Globals.Config.Server.DuelTourney);
                packetWriter.Write(1, 20);
                packetWriter.WriteSkip(20);

                client.Send(packetWriter);
            }
        }
        public static void ResponseCharList (Client client, List<Pair<string, byte>> characters)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseAccountCharList, CryptFlags.Encrypt))
            {
                packet.Write(characters.Count, 34);

                for (byte a = 0; a < characters.Count; a++)
                {
                    packet.Write(characters[a].First, 32);
                    packet.Write(a);
                    packet.Write(characters[a].Second);
                }

                client.Send(packet);
            }
        }

        public static void ResponseCreateChar (Client client, Results result, string name)
        {
             using (var packet = new PacketWriter(Operation.MatchResponseCreateChar, CryptFlags.Encrypt))
             {
                 packet.Write((Int32)result);
                 packet.Write(name);

                 client.Send(packet);
             }
        }

        public static void ResponseCharInfo (Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseAccountCharInfo, CryptFlags.Encrypt))
            {
                packet.Write(client.GetCharacter().CharNum);
                packet.Write(client.GetCharacter());
                
                client.Send(packet);
            }
        }

        public static void ResponseDeleteCharacter(Client client, Results result)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseDeleteChar, CryptFlags.Encrypt))
            {
                packet.Write((Int32)result);
                client.Send(packet);
            }
        }

        public static void ResponseSelectCharacter(Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseSelectChar, CryptFlags.Encrypt))
            {
                packet.Write(0);
                packet.Write(client.GetCharacter(), false);
                packet.Write(1,1);
                packet.Write((byte)ExpManager.PercentToNextLevel((int)client.GetCharacter().Xp));

                client.Send(packet);
            }
        }

        public static void ResponseBridgePeer(Client client)
        {
            using (var packet = new PacketWriter(Operation.BridgeResponse, CryptFlags.Decrypt))
            {
                packet.Write(client.GetMuid());
                packet.Write((Int32)Results.Accepted);

                client.Send(packet);
            }
        }

        public static void ResponseRecommendedChannel(Client client, Muid channelId)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseRecommendedChannel, CryptFlags.Encrypt))
            {
                packet.Write(channelId);
                client.Send(packet);
            }
        }

        public static void ResponseShopItemList(Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseShopItemList, CryptFlags.Encrypt))
            {
                packet.Write(0, 12);
                List<Item> items = ItemList.GetShopItems(client.GetCharacter().Sex);
                
                packet.Write(items.Count, 4);
                items.ForEach(i => packet.Write(i.ItemId));

                client.Send(packet);
            }
        }

        public static void ResponseCharacterItemList(Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseCharacterItemList, CryptFlags.Decrypt))
            {
                packet.Write(client.GetCharacter().Bp);

                packet.Write(17, 8);
                for (var i = 0; i < 17; ++i)
                {
                    packet.Write(0);
                    packet.Write(client.GetCharacter().EquippedItems[i].ItemCid);
                }


                packet.Write(client.GetCharacter().Items.Count, 24);
                foreach (var i in client.GetCharacter().Items)
                {
                    packet.Write(0);
                    packet.Write(i.ItemCid);
                    packet.Write(i.ItemId);
                    packet.Write(i.RentHour);
                    packet.Write(0);
                    packet.Write(i.Quantity);
                }
                packet.Write(0, 12);

                client.Send(packet);
            }
        }

        public static void ResponseBuyItem(Client client, Results results)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseBuyItem, CryptFlags.Decrypt))
            {
                packet.Write((Int32)results);
                client.Send(packet);
            }
        }

        public static void ResponseSellItem(Client client, Results results)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseSellItem, CryptFlags.Decrypt))
            {
                packet.Write((Int32)results);
                client.Send(packet);
            }
        }

        public static void ResponseEquipItem(Client client, Results results)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseEquipItem, CryptFlags.Decrypt))
            {
                packet.Write((Int32)results);
                client.Send(packet);
            }
        }

        public static void ResponseTakeOffItem(Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseTakeOffItem, CryptFlags.Decrypt))
            {
                packet.Write(0);
                client.Send(packet);
            }
        }

        public static void Whisper (Client client, string from, string to, string message)
        {
            using (var packet = new PacketWriter(Operation.MatchWhisper, CryptFlags.Encrypt))
            {
                packet.Write(from);
                packet.Write(to);
                packet.Write(message);

                client.Send(packet);
            }
        }

        public static void Notify (Client client, Int32 error)
        {
            using (var packet = new PacketWriter(Operation.MatchNotify, CryptFlags.Encrypt))
            {
                packet.Write(error);
                client.Send(packet);
            }
        }

        public static void Announce(Client client, string message)
        {
            using (var packet = new PacketWriter(Operation.AdminAnnounce, CryptFlags.Decrypt))
            {
                packet.Write(client.GetMuid());
                packet.Write(message);
                packet.Write(0);

                TcpServer.GlobalPacket(packet);
            }
        }

        public static void ResponseMySimpleCharInfo(Client client)
        {
            using (var packet = new PacketWriter(Operation.MatchResponseMySimpleCharInfo, CryptFlags.Encrypt))
            {
                packet.Write(1, 9);
                packet.Write((int)client.GetCharacter().Xp);
                packet.Write((int)client.GetCharacter().Bp);
                packet.Write((byte)client.GetCharacter().Level);

                client.Send(packet);
            }
        }
        
        public static void ResponseNotify(Client client, string message, Int32 type = 0)
        {
            using (var packet = new PacketWriter(Operation.AdminAnnounce, CryptFlags.Encrypt))
            {
                packet.Write(client.GetMuid());
                packet.Write(message);
                packet.Write(type);

                client.Send(packet);
            }
        }

    }
}
