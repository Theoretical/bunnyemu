using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunny.Channels;
using Bunny.Core;
using Bunny.Enums;

namespace Bunny.Packet.Assembled
{
    class ChannelPackets
    {
        public static void ResponseChannelJoin(Client client)
        {
            var traits = client.GetChannel().GetTraits();

            using (var packet = new PacketWriter(Operation.ChannelResponseJoin, CryptFlags.Encrypt))
            {
                packet.Write(traits.ChannelId);
                packet.Write((Int32)traits.Type);
                packet.Write(traits.ChannelName);
                

                client.Send(packet);
            }
        }

        public static void ResponseRuleSet(Client client)
        {
            var traits = client.GetChannel().GetTraits();
            using (var packet = new PacketWriter(Operation.MatchResponseRuleset, CryptFlags.Encrypt))
            {
                packet.Write(traits.ChannelId);
                packet.Write(traits.Rule.ToString().ToLower());
                
                client.Send(packet);
            }
        }

        public static void ResponseLeave(Client client, Muid channel)
        {
            using (var packet = new PacketWriter(Operation.ChannelLeave, CryptFlags.Encrypt))
            {
                packet.Write(client.GetMuid());
                packet.Write(channel);

                client.Send(packet);
            }
        }
        public static void ResponsePlayerList (List<Client> sendTo, byte playerCount,byte page, byte count, List<Client> clients)
        {
            using (var packet = new PacketWriter(Operation.ChannelResponsePlayerList, CryptFlags.Encrypt))
            {
                packet.Write(playerCount);
                packet.Write(page);
                packet.Write(clients.Count, 108);

                foreach (var c in clients)  
                {
                    packet.Write(c.GetMuid());
                    packet.Write(c.GetCharacter().Name, 32);
                    packet.Write(c.GetCharacter().ClanName, 16);
                    packet.Write((byte)c.GetCharacter().Level);
                    packet.Write((Int32)c.ClientPlayer.PlayerLocation);
                    packet.Write((byte)c.ClientPlayer.PlayerAccount.Access);
                    packet.Write((byte)c.ClientPlayer.PlayerAccount.Access); // pgrade
                    packet.Write((byte)2);
                    packet.Write(c.GetCharacter().ClanId);
                    packet.Write("", 32); //discord
                    packet.Write(0); // discord
                    packet.Write(0); //emblem.
                }

                sendTo.ForEach(c => c.Send(packet));
            }
        }

        public static void ResponseAllPlayerList(Client client, List<Client> clients, Muid channelId)
        {
            using (var packet = new PacketWriter(Operation.ChannelResponseAllPlayerList, CryptFlags.Encrypt))
            {
                packet.Write(channelId);
                packet.Write(clients.Count, 71);

                foreach (var c in clients)
                {
                    packet.Write(c.GetMuid());
                    packet.Write(c.GetCharacter().Name, 32);
                    packet.Write(c.GetCharacter().ClanName, 16);
                    packet.Write((byte)c.GetCharacter().Level);
                    packet.Write((Int32)c.ClientPlayer.PlayerLocation);
                    packet.Write((byte)c.ClientPlayer.PlayerAccount.Access);
                    packet.Write((byte)2);
                    packet.Write(c.GetCharacter().ClanId);
                    packet.Write(0); //emblem.
                }

                client.Send(packet);
            }
        }

        public static void ResponseChat (List<Client> clients, Muid channelId, string charName, string message, UGradeId access)
        {
            using (var packet = new PacketWriter(Operation.ChannelChat, CryptFlags.Encrypt))
            {
                packet.Write(channelId);
                packet.Write(charName);
                packet.Write(message);
                packet.Write((Int32)access);

                clients.ForEach(c => c.Send(packet));
            }
        }

        public static void ResponseChannelList(Client client, List<Channel> channels)
        {
            using (var packet = new PacketWriter(Operation.ChannelList, CryptFlags.Decrypt))
            {
                packet.Write(channels.Count, 88);

                Int16 index = 0;
                foreach (var c in channels) 
                {
                    var traits = c.GetTraits();
                    packet.Write(traits.ChannelId);
                    packet.Write(++index);
                    packet.Write((Int16)traits.Playerlist.Count);
                    packet.Write((Int16)traits.MaxUsers);
                    packet.Write((Int16)traits.MinLevel);
                    packet.Write((Int16)traits.MaxLevel);
                    packet.Write((byte)traits.Type);
                    packet.Write(traits.ChannelName, 64);
                    packet.Write(false);
                    packet.Write(0);
                }

                client.Send(packet);
            }
        }
    }
}
