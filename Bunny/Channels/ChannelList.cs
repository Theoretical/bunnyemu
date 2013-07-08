using System;
using System.Collections.Generic;
using System.Xml;
using Bunny.Core;
using Bunny.Enums;

namespace Bunny.Channels
{
    class ChannelList
    {
        private static readonly List<Channel> Channels = new List<Channel>();
        private static readonly MuidWrapper Muids = new MuidWrapper();
        private static readonly object ObjectLock = new object();

        public static void Add(Channel c)
        {
            lock (ObjectLock)
                Channels.Add(c);
        }

        public static void Add(ChannelTraits c)
        {
            lock (ObjectLock)
                Channels.Add(new Channel(c));
        }

        public static void Remove(Channel c)
        {
            lock (ObjectLock)
                Channels.Remove(c);
        }

        public static void Remove(ChannelTraits c)
        {
            lock(ObjectLock)
                Channels.Remove(Channels.Find(channel => channel.GetTraits().Equals(c)));
        }

        public static Channel Find (Muid uid)
        {
            lock (ObjectLock)
                return
                    Channels.Find(
                        c => c.GetTraits().ChannelId == uid && c.GetTraits().MaxUsers != c.GetTraits().Playerlist.Count);
        }

        public static Channel Find (Stages.Stage stage)
        {
            lock (ObjectLock)
                return Channels.Find(c => c.StageExists(stage));
        }

        public static Channel Find(ChannelType type, string name)
        {
            lock (ObjectLock)
                return Channels.Find(c => c.GetTraits().Type == type && c.GetTraits().ChannelName == name);
        }

        public static List<Channel> GetList(ChannelType type)
        {
            lock (ObjectLock)
                return Channels.FindAll(c => c.GetTraits().Type == type);
        }

        public static void AddAndJoin(Client client, ChannelTraits channelTraits)
        {
            channelTraits.ChannelId = Muids.GetNext();
            Add(channelTraits);
            Find(channelTraits.ChannelId).Join(client);
        }

        public static Channel Recommend(Int32 level, bool bClan = false, string clanName = "")
        {
            lock (ObjectLock)
            {
                Channel channel;
                if (bClan)
                {
                    channel = Channels.Find(c => c.GetTraits().ChannelName == clanName);

                    if (channel != null)
                        return channel;
                }

                channel =
                    Channels.Find(
                        c =>
                        c.GetTraits().Type == ChannelType.General &&
                        c.GetTraits().MaxUsers < c.GetTraits().Playerlist.Count && c.GetTraits().MinLevel <= level &&
                        c.GetTraits().MaxLevel >= level);

                if (channel == null)
                {
                    return Channels[0];
                }

                return channel;
            }
        }

        public static void Load()
        {
            // Why wo uld you ever load more than once?
            if (Channels.Count > 0)
                return;

            using (var reader = new XmlTextReader("channel.xml"))
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "CHANNEL":
                            var channel = new ChannelTraits(Muids.GetNext());
                            channel.ChannelName = reader.GetAttribute("name");

                            if (!Int32.TryParse(reader.GetAttribute("levelmin"), out channel.MinLevel))
                                channel.MinLevel = 0;

                            channel.MaxUsers = Int32.Parse(reader.GetAttribute("maxplayers"));

                            switch (reader.GetAttribute("rule"))
                            {
                                case "elite":
                                    channel.Rule = ChannelRule.Elite;
                                    break;
                                case "duel":
                                    channel.Rule = ChannelRule.Elite;
                                    channel.Type = ChannelType.DuelTournament;
                                    break;
                            }
                            Add(channel);

                            break;
                    }
                }
            }
        }
    }
}
