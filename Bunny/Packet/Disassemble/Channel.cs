using System;
using System.Collections.Generic;
using Bunny.Channels;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Utility;

namespace Bunny.Packet.Disassemble
{
    class ChannelHandler
    {
        [PacketHandler(Operation.MatchRequestRecommendedChannel, PacketFlags.Character)]
        public static void ProcessRecommendedChannel(Client client, PacketReader packet)
        {
            if (client.GetCharacter().ClanId == 0)
                client.ClientPlayer.PlayerChannel = ChannelList.Recommend(client.GetCharacter().Level);
            else
            {
                client.ClientPlayer.PlayerChannel = ChannelList.Recommend(client.GetCharacter().Level, true, client.GetCharacter().ClanName);
            }

            Match.ResponseRecommendedChannel(client, client.GetChannel().GetTraits().ChannelId);

            if (client.GetCharacter().ClanId != 0)
            {
                ClanPackets.MemberConnected(client, client.GetCharacter().Name);
            }
        }

        [PacketHandler(Operation.ChannelJoin, PacketFlags.Character)]
        public static void ProcessChannelJoin(Client client, PacketReader packet)
        {
            var playerId = packet.ReadMuid();
            var channelId = packet.ReadMuid();

            var channel = ChannelList.Find(channelId);

            if (channel != null)
            {
                channel.Join(client);
            }
        }

        [PacketHandler(Operation.ChannelRequestPlayerList, PacketFlags.Character)]
        public static void ProcessPlayerList(Client client, PacketReader packet)
        {
            var playerId = packet.ReadUInt64();
            var channelId = packet.ReadUInt64();
            var page = packet.ReadInt32();

            client.ClientPlayer.ChannelPage = page;

            if (client.GetChannel() != null)
                client.GetChannel().PlayerList(client);
        }

        [PacketHandler(Operation.ChannelRequestChat, PacketFlags.Character)]
        public static void ProcessChat(Client client, PacketReader packet)
        {
            var uidChar = packet.ReadMuid();
            var uidChan = packet.ReadMuid();
            var message = packet.ReadString();

            if (client.GetChannel() != null)
                client.GetChannel().Chat(client, message);
        }

        [PacketHandler(Operation.ChannelListStart, PacketFlags.Character)]
        public static void ProcessChannelList(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadUInt64();
            var type = packetReader.ReadInt32();

            if (!Enum.IsDefined(typeof (ChannelType), (byte) type))
            {
                client.Disconnect();
                return;
            }

            var channels = ChannelList.GetList((ChannelType)type);
            if (channels.Count == 0)
                return;

            ChannelPackets.ResponseChannelList(client, channels);
        }

        [PacketHandler(Operation.ChannelRequestJoinFromName, PacketFlags.Character)]
        public static void ResponseChannelJoinFromName(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadUInt64();
            var type = packetReader.ReadInt32();
            var name = packetReader.ReadString();

            if (!Enum.IsDefined(typeof(ChannelType), (byte)type))
            {
                client.Disconnect();
                return;
            }

            Channels.Channel c = ChannelList.Find((ChannelType) type, name);
            if (c == null)
            {
                var channel = new ChannelTraits();
                channel.ChannelName = name;
                channel.Rule = ChannelRule.Elite;
                channel.Type = (ChannelType)type;
                channel.MaxLevel = 100;
                channel.MinLevel = 0;
                channel.MaxUsers = 100;

                ChannelList.AddAndJoin(client, channel);
                return;
            }
            client.ClientPlayer.PlayerChannel = c;
            c.Join(client);
            
        }

        [PacketHandler(Operation.ChannelRequestAllPlayerList, PacketFlags.Character)]
        public static void ProcessAllChannelPlayerList(Client client, PacketReader packet)
        {
            if (client.GetChannel() != null)
                client.GetChannel().AllPlayerList(client);
        }
    }
}
