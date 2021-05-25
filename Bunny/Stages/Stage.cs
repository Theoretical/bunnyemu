using Bunny.Channels;
using Bunny.Core;
using Bunny.Enums;
using Bunny.Packet.Assembled;
using Bunny.Utility;

namespace Bunny.Stages
{
    class Stage
    {
        private readonly StageTraits _traits;
        public object ObjectLock = new object();

        public StageTraits GetTraits()
        {
            return _traits;
        }
        public Stage(StageTraits traits)
        {
            _traits = traits;
        }
        public void Join(Client client)
        {
            var result = Results.Accepted;

            if (_traits.Players.Count == _traits.MaxPlayers && _traits.MaxPlayers != 0)
            {
                result = Results.StageRoomFull;
                StagePackets.ResponseStageJoin(client, result);
            }
            else if (_traits.Level != 0 && !(client.GetCharacter().Level > _traits.Level && client.GetCharacter().Level < _traits.Level + 10))
            {
                result = Results.StageInvalidLevel;
                StagePackets.ResponseStageJoin(client, result);
            }
            else
            {
                client.ClientPlayer.PlayerStage = this;
                client.ClientFlags = PacketFlags.Stage;
                client.ClientPlayer.PlayerLocation = Place.Stage;
                _traits.Players.Add(client);


                lock (ObjectLock)
                {
                    StagePackets.ResponseStageJoin(client, result);
                    StagePackets.ResponseObjectCache(client, ObjectCache.Expire, _traits.Players);
                    StagePackets.ResponseStageMaster(_traits.Players, this);

                    var clients = _traits.Players.FindAll(c => c != client);

                    StagePackets.ResponseObjectCacheExclusive(clients, ObjectCache.Keep, client);
                    StagePackets.ResponseStageMaster(_traits.Players, this);

                   Channel.Refresh(client);
                }
            }
        }
        public void Leave(Client client)
        {
            lock (ObjectLock)
            {
                client.ClientPlayer.PlayerLocation = Place.Lobby;
                client.ClientFlags = PacketFlags.Character;
                client.ClientPlayer.PlayerStage = null;

                StagePackets.ResponseStageLeave(_traits.Players, this, client.GetMuid());
                _traits.Players.Remove(client);

                if (_traits.Players.Count == 0)
                {
                    if (Globals.NatAgent != null)
                    {
                        AgentPackets.ReleaseStageToAgent(Globals.NatAgent, _traits.StageId);
                        Log.Write("Released stage: {0} from NAT Server", _traits.Name);
                    }

                    var channel = ChannelList.Find(this);
                    if (channel != null)
                    {
                        channel.Remove(this);
                        channel.Refresh();
                    }
                    return;
                }

                if (_traits.Master == client)
                    _traits.Master = _traits.Players[0];

                StagePackets.ResponseObjectCache(_traits.Players, ObjectCache.New, _traits.Players);
                StagePackets.ResponseStageMaster(_traits.Players, this);
                StagePackets.ResponseSettings(_traits.Players, _traits);


                StagePackets.ResponseStageMaster(_traits.Players, this);
                Channel.Refresh(client);
            }
        }
        public void Settings(Client client, bool toAll = false)
        {
            lock (ObjectLock)
            {
                if (!toAll)
                    StagePackets.ResponseSettings(_traits.Players, _traits);
                else
                {
                    _traits.Players.ForEach(c => StagePackets.ResponseSettings(_traits.Players, _traits));
                }
            }
        }
        public void Chat(Client client, string message)
        {
            lock(ObjectLock)
               StagePackets.ResponseStageChat( _traits.Players, client.GetMuid(), _traits.StageId, message);
            Settings(client,true);
        }
        public void PlayerState(Client client)
        {
            lock (ObjectLock)
                StagePackets.ResponsePlayerState(_traits.Players, client.GetMuid(), _traits.StageId, client.ClientPlayer.PlayerState);  
        }
        public void Team(Client client)
        {
            lock (ObjectLock)
                StagePackets.ResponseStageTeam(_traits.Players, client.GetMuid(), _traits.StageId,
                                               client.ClientPlayer.PlayerTeam);
        }
        public void Start(Client client)
        {
            lock (ObjectLock)
            {
                var clients =
                    _traits.Players.FindAll(
                        c => c.ClientPlayer.PlayerState != ObjectStageState.Ready && _traits.Master != c);

                if (clients.Count > 0)
                {
                    StagePackets.QuestError( _traits.Players, 1, _traits.StageId);
                    return;
                }

                _traits.Ruleset.GameStartCallback(client);
                Channel.Refresh(client);
            }
        }
    }
}
 