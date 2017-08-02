using System;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Stages;
using System.Collections.Generic;

namespace Bunny.Channels
{
    class Channel
    {
        private readonly ChannelTraits _traits;
        private readonly StageList _stages = new StageList();
        private readonly object _objectLock = new object();

        public ChannelTraits GetTraits()
        {
            return _traits;
        }

        public Channel(ChannelTraits traits)
        {
            _traits = traits;
        }

        public bool StageExists (Stage stage)
        {
            return _stages.Find(stage.GetTraits().StageId) != null;
        }

        public Stage Find (Muid stageId)
        {
            return _stages.Find(stageId);
        }
        public void Leave(Client client)
        {
            lock (_objectLock)
            {
                _traits.Playerlist.Remove(client);
                ChannelPackets.ResponseLeave(client, _traits.ChannelId);
                client.ClientPlayer.PlayerChannel = null;

                if (_traits.Playerlist.Count == 0 &&
                    (_traits.Type == ChannelType.Private || _traits.Type == ChannelType.Clan))
                {
                    ChannelList.Remove(this);
                    return;
                }

                _traits.Playerlist.ForEach(c => PlayerList(c));
            }
        }

        public Stage Add(StageTraits traits)
        {
            lock (_objectLock)
            {
                _stages.Add(traits);
                var stage = _stages.Find(traits.StageId);
                stage.GetTraits().StageIndex = _stages.Index(stage);

                return stage;
            }
        }

        public void Remove(Stage stage)
        {
            lock (_objectLock)
            {
                _stages.Remove(stage);
            }
        }

        public void Join(Client client)
        {
            lock (_objectLock)
            {
                if (client.GetChannel() != null)
                    client.GetChannel().Leave(client);

                _traits.Playerlist.Add(client);
                client.ClientPlayer.PlayerChannel = this;

                ChannelPackets.ResponseChannelJoin(client);
                ChannelPackets.ResponseRuleSet(client);

                _traits.Playerlist.ForEach(delegate(Client c)
                                               {
                                                   PlayerList(c);
                                                   StageList(c);
                                               });
            }
        }

        public StageList StageList()
        {
            return _stages;
        }

        public void StageList(Client client)
        {
            var stages = _stages.GetList();

            lock (stages)
            {
                var curr = Convert.ToByte(Math.Min(8, Math.Max(0, stages.Count - client.ClientPlayer.StageIndex)));
                var prev = Convert.ToByte(Math.Min(8, Math.Max(0, stages.Count - Math.Max(0, client.ClientPlayer.StageIndex - 8))));
                var next = Convert.ToByte(Math.Min(8, Math.Max(0, stages.Count - (client.ClientPlayer.StageIndex + 8))));

                var page = stages.GetRange(client.ClientPlayer.StageIndex, curr);
                StagePackets.ResponseStageList(client, prev, next, page);
            }

        }

        public void PlayerList(Client client)
        {
            var pages = Convert.ToByte(_traits.Playerlist.Count / 6);
            var page = Math.Min(client.ClientPlayer.ChannelPage, pages);
            var start = page * 6;
            var count = Math.Min(_traits.Playerlist.Count - start, 6);

            lock (_objectLock)
            {
                var clients = _traits.Playerlist.GetRange(start, count);
                ChannelPackets.ResponsePlayerList(_traits.Playerlist, (byte)_traits.Playerlist.Count, (byte) page, (byte) count, clients);
            }
        }

        public void Refresh()
        {
            lock (_objectLock)
                _traits.Playerlist.ForEach(delegate(Client c)
                {
                    PlayerList(c);
                    StageList(c);
                });
        }

        public void Chat(Client client, string message)
        {
            if (message.StartsWith("/cw "))
            {
                var args = message.Substring(message.IndexOf(" ") + 1);
                var c = Network.TcpServer.GetClientFromName(args);

                List<Client> red = new List<Client>();
                List<Client> blue = new List<Client>();

                red.Add(c);
                blue.Add(client);

                _stages.CreateClanwar(red, blue);
            }
            lock (_objectLock)
                ChannelPackets.ResponseChat(_traits.Playerlist, _traits.ChannelId, client.GetCharacter().Name, message, client.ClientPlayer.PlayerAccount.Access);
        }

        public static void Refresh (Client client)
        {
            if (client.GetChannel() != null)
                client.GetChannel().Refresh();
        }

        public void AllPlayerList(Client client)
        {
            lock (_objectLock)
            {

                var clients =
                    _traits.Playerlist.FindAll(
                        c =>
                        c.ClientPlayer.PlayerLocation != Place.Battle && c.ClientPlayer.PlayerLocation != Place.Stage);

                ChannelPackets.ResponseAllPlayerList(client,clients, _traits.ChannelId);
            }
        }
    }
}
