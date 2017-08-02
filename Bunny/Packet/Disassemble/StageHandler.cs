using System;
using System.Collections.Generic;
using System.Threading;
using Bunny.Core;
using Bunny.Enums;
using Bunny.GameTypes;
using Bunny.Packet.Assembled;
using Bunny.Stages;
using Bunny.Utility;
using Bunny.Channels;

namespace Bunny.Packet.Disassemble
{
    class StageHandler
    {
        [PacketHandler(Operation.StageCreate, PacketFlags.Character)]
        public static void ProcessStageCreate(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadMuid();
            var name = packetReader.ReadString();
            var locked = packetReader.ReadBoolean();
            var password = packetReader.ReadString();

            if (uid != client.GetMuid())
            {
                client.Disconnect();
                return;
            }

            var traits = new StageTraits(); 
            traits.StageId = Globals.StageCounter.GetNext();
            traits.Name = name;
            traits.Locked = locked;
            traits.Password = password;
            traits.Master = client;
            
            client.ClientPlayer.PlayerStage = client.GetChannel().Add(traits);
            client.GetStage().Join(client);
        }

        [PacketHandler(Operation.StageRequestJoin, PacketFlags.Character)]
        public static void ProcessStageJoin(Client client, PacketReader packetReader)
        {
            var uidChar = packetReader.ReadMuid();
            var uidStage = packetReader.ReadMuid();

            var stage = client.GetChannel().Find(uidStage);

            if (stage == null)
            {
                Log.Write("Failed to join stage: {0}", uidStage.HighId);
                return;
            }

            stage.Join(client);
        }

        [PacketHandler(Operation.StageLeave, PacketFlags.Stage)]
        public static void ProcessStageLeave (Client client, PacketReader packet)
        {
            client.ClientPlayer.PlayerLocation = Place.Lobby;
            if (client.GetStage() != null)
                client.GetStage().Leave(client);
        }

        [PacketHandler(Operation.StageListRequest, PacketFlags.None)]
        public static void ProcessStageList(Client client, PacketReader packetReader)
        {
            var uidChar = packetReader.ReadUInt64();
            var uidChan = packetReader.ReadUInt64();
            var page = packetReader.ReadInt32();

            client.ClientPlayer.StageIndex = Convert.ToByte(page);
            if (client.GetChannel() != null)
                client.GetChannel().StageList(client);
        }


        [PacketHandler(Operation.StageRequestSettings, PacketFlags.Stage)]
        public static void ProcessStageSettings(Client client, PacketReader packet)
        {
            if (client.GetStage() != null)
                client.GetStage().Settings(client, true);
        }

        [PacketHandler(Operation.StageUpdateSettings, PacketFlags.Stage)]
        public static void ProcessStageSetting(Client client, PacketReader packetReader)
        {
            if (client.GetStage() == null)
                return;

            var stage = client.GetStage().GetTraits();

            var uidChar = packetReader.ReadMuid();
            var uidStage = packetReader.ReadMuid();
            var total = packetReader.ReadInt32();
            var size = packetReader.ReadInt32();
            var count = packetReader.ReadInt32();
            var uidStage2 = packetReader.ReadMuid();
            var map = packetReader.ReadString(32);
            var index = packetReader.ReadInt32();
            var type = packetReader.ReadInt32();
            var rounds = packetReader.ReadInt32();
            var time = packetReader.ReadInt32();
            var level = packetReader.ReadInt32();
            var players = packetReader.ReadInt32();
            var teamkill = packetReader.ReadBoolean();
            var balance = packetReader.ReadBoolean();
            var join = packetReader.ReadBoolean();
            var win = packetReader.ReadBoolean();

            if ((ObjectStageGameType)type != stage.Gametype)
            {
                if (!Enum.IsDefined(typeof(ObjectStageGameType), (byte)type))
                {
                    client.Disconnect();
                    return;
                }

                stage.Gametype = (ObjectStageGameType)type;

            }

            switch ((ObjectStageGameType)type)
            {
                case ObjectStageGameType.DeathMatch:
                    stage.Ruleset = new Deathmatch(client.GetStage());
                    break;
                case ObjectStageGameType.Berserker:
                    stage.Ruleset = new Berserker(client.GetStage());
                    break;
                case ObjectStageGameType.Duel:
                    stage.Ruleset = new Duel(client.GetStage());
                    break;
                case ObjectStageGameType.Gladiator:
                    stage.Ruleset = new Deathmatch(client.GetStage());
                    break;
                case ObjectStageGameType.Training:
                    stage.Ruleset = new Deathmatch(client.GetStage());
                    break;
                case ObjectStageGameType.TeamDeathMatch:
                    stage.Ruleset = new TeamDeathmatch(client.GetStage());
                    break;
                case ObjectStageGameType.TeamGladiator:
                    stage.Ruleset = new TeamDeathmatch(client.GetStage());
                    break;
                case ObjectStageGameType.Assassination:
                    stage.Ruleset = new Assassination(client.GetStage());
                    break;
                case ObjectStageGameType.TeamDeathMatchExtreme:
                    stage.Ruleset = new TeamDeathmatchExtreme(client.GetStage());
                    break;
                default:
                    Log.Write("Unknown ruleset: {0}", type);
                    stage.Ruleset = new Deathmatch(client.GetStage());
                    break;
            }
            
            stage.RoundCount =  rounds;

            stage.Time = time > Byte.MaxValue ? (byte)0 : Convert.ToByte(time);
            stage.Level = Convert.ToByte(level);
            stage.MaxPlayers = Convert.ToByte(players);
            stage.ForcedEntry = join;
            stage.TeamBalanced = balance;
            stage.TeamKill = teamkill;

            if ((ObjectStageGameType)type == ObjectStageGameType.Duel && type != stage.Type)
            {
                stage.CurrentMap = Globals.Maps.GetMap("Hall");
                stage.Map = "Hall";
            }
            else
            {
                stage.Type = type;
            }

            client.GetStage().Settings(client, true);
            Channel.Refresh(client);
        }

        [PacketHandler(Operation.StageMap, PacketFlags.Stage)]
        public static void ProcessStageMap(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadMuid();
            var map = packetReader.ReadString();

            if (map.Length > 127 || client.GetStage() == null || client.GetStage().GetTraits().Master != client)
            {
                client.Disconnect();
                return;
            }

            if (map != "RelayMap")
            {
                client.GetStage().GetTraits().RelayEnabled = false;
                client.GetStage().GetTraits().Map = map;
                try
                {
                    client.GetStage().GetTraits().CurrentMap = Globals.Maps.GetMap(map);
                }
                catch
                {
                    Log.Write("Unable to load map: {0}. Defaulting to Mansion", map);
                    client.GetStage().GetTraits().CurrentMap = Globals.Maps.GetMap("Mansion");
                }
            }
            else
            {
                client.GetStage().GetTraits().RelayEnabled = true;
            }
            client.GetStage().Settings(client, true);
            Channel.Refresh(client);
        }

        [PacketHandler(Operation.StageChat, PacketFlags.Stage)]
        public static void ResponseStageChat(Client client, PacketReader packetReader)
        {
            var uidChar = packetReader.ReadUInt64();
            var uidStage = packetReader.ReadUInt64();
            var message = packetReader.ReadString();

            if (client.GetStage() != null)
                client.GetStage().Chat(client, message);
        }


        [PacketHandler(Operation.StageTeam, PacketFlags.Stage)]
        public static void ResponseStageTeam(Client client, PacketReader packetReader)
        {
            var charId = packetReader.ReadMuid();
            var stageId = packetReader.ReadMuid();
            var team = packetReader.ReadInt32();

            if (!Enum.IsDefined(typeof(Team), team) || client.GetStage() == null)
            {
                client.Disconnect();
                return;
            }

            client.ClientPlayer.PlayerTeam = (Team) team;
            client.GetStage().Team(client);
        }

        [PacketHandler(Operation.StageState, PacketFlags.Stage)]
        public static void ResponseStageState(Client client, PacketReader packetReader)
        {
            var charId = packetReader.ReadMuid();
            var stageId = packetReader.ReadMuid();
            var state = packetReader.ReadInt32();

            if (!Enum.IsDefined(typeof(ObjectStageState), state) || client.GetStage() == null)
            {
                client.Disconnect();
                return;
            }

            client.ClientPlayer.PlayerState = (ObjectStageState)state;
            client.GetStage().PlayerState(client);
        }


        [PacketHandler(Operation.StageStart, PacketFlags.Stage)]
        public static void ProcessStageStart(Client client, PacketReader packetReader)
        {
            if (client.GetStage() == null || client.GetStage().GetTraits().Master != client)
                return;
            
            client.GetStage().Start(client);
        }
       
        [PacketHandler(Operation.LoadingComplete, PacketFlags.Stage)]
        public static void ProcessLoading (Client client, PacketReader packetReader)
        {
            if (client.GetStage() == null) return;
            client.ClientFlags = PacketFlags.Battle;
            client.GetStage().GetTraits().Ruleset.GameLoadedCallback(client);
        }

        [PacketHandler(Operation.StageRequestEnterBattle, PacketFlags.Stage)]
        public static void ProcessEnterBattle(Client client, PacketReader packetReader)
        {
            if (client.GetStage() == null) return;
            client.GetStage().GetTraits().Ruleset.GameEnterCallback(client);
        }

        [PacketHandler(Operation.GameRequestTimeSync, PacketFlags.Stage)]
        public static void ProcessGameTimeSync(Client client, PacketReader packetReader)
        {
            var nTime = packetReader.ReadInt32();

            Battle.GameResponseTimesync(client, nTime);
        }

        [PacketHandler(Operation.GameReportTimeSync, PacketFlags.Stage)]
        public static void ProcessGameReportTimeSync(Client client, PacketReader packetReader)
        {
            //EXPERIMENTAL!
            return;
            /*
            var time = packetReader.ReadInt32();
            var memory = packetReader.ReadInt32();

            if ((time - client.ClientPlayer.LastTimeSync) * 0.00004999999873689376 > 2.0)
            {
                Log.Write("Player: {0} was speed hacking! Found: {1}", client.GetCharacter().Name, client.ClientPlayer.LastTimeSync * 0.00004999999873689376);
                client.Disconnect();
            }
             */
        }

        [PacketHandler(Operation.BattleRequestInfo, PacketFlags.Stage)]
        public static void ProcessBattleInfo(Client client, PacketReader packetReader)
        {
            if (client.GetStage() == null)
            {
                Log.Write("NULL Stage?");
                return;
            }
            client.GetStage().GetTraits().Ruleset.GameInfoCallback(client);
        }

        [PacketHandler(Operation.GameKill, PacketFlags.Stage)]
        public static void ProcessGameKill(Client client, PacketReader packet)
        {
            if (client.GetStage() == null)
            {
                Log.Write("Client doesn't have a stage...?");
                return;
            }

            var uidKiller = packet.ReadMuid();
            Client killer;

            lock (client.GetStage().ObjectLock)
            {
                killer =
                    client.GetStage().GetTraits().Players.Find(c => c.GetMuid() == uidKiller);
            }

            if (killer != null)
                client.GetStage().GetTraits().Ruleset.GameKillCallback(killer, client);
            else
            {
                Log.Write("Invalid killer");
            }
        }


        [PacketHandler(Operation.StageLeaveBattle, PacketFlags.Character)]
        public static void ProcessLeaveBattle (Client client, PacketReader packet)
        {
            if (client.GetStage() != null)
            {
                client.ClientPlayer.PlayerLocation = Place.Stage;
                client.GetStage().GetTraits().Ruleset.GameLeaveBattle(client);
            }
        }

        
        //How am I being forced to enter if i request a late join??????? also fuck my laptop. k.
        [PacketHandler(Operation.StageRequestForcedEntry, PacketFlags.Stage)]
        public static void ProccessLateJoin (Client client, PacketReader packet)
        {
            if (client.GetStage() == null)
                return;
           
            Battle.ResponseLateJoin(client);
            client.GetStage().GetTraits().Ruleset.GameLateJoinCallback(client);
        }

        [PacketHandler(Operation.StageRequestPeerList, PacketFlags.Stage)]
        public static void ProcessPeerList (Client client, PacketReader packet)
        {
            if (client.GetStage() != null)
            {
                lock (client.GetStage().ObjectLock)
                    Battle.ResponsePeerList(client, client.GetStage().GetTraits().Players);
            }
        }


        [PacketHandler(Operation.MatchRequestObtainWorldItem, PacketFlags.Stage)]
        public static void ProcessObtainWorldItem(Client client, PacketReader packet)
        {
            var uidChar = packet.ReadMuid();
            var nItem = packet.ReadInt32();

            lock (client.GetStage().GetTraits().CurrentMap)
            {
                var i =
                    client.GetStage().GetTraits().CurrentMap.DeathMatchItems.Find(
                        ii => ii.ItemUid == nItem);

                if (i != null)
                {
                    i.Taken = true;
                    i.NextSpawn = DateTime.Now.AddSeconds(i.SpawnTime);
                    Log.Write("Spawning item: {0}. Next Spawn: {1}", i.ItemId, DateTime.Now.AddSeconds(i.SpawnTime / 1000));
                } 
                else
                {
                    lock (client.GetStage().GetTraits().WorldItems)
                    {
                        i = client.GetStage().GetTraits().WorldItems.Find(ii => ii.ItemUid == nItem);


                        if (i != null)
                        {
                            client.GetStage().GetTraits().WorldItems.Remove(i);
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                lock (client.GetStage().ObjectLock)
                    Battle.ObtainWorldItem(client.GetStage().GetTraits().Players,
                                           client.GetMuid(), nItem);
            }
        }

        [PacketHandler(Operation.MatchRequestSpawnWorldItem, PacketFlags.Battle)]
        public static void ResponseSpawnWorldItem(Client client, PacketReader packet)
        {
            var charId = packet.ReadMuid();
            var itemId = packet.ReadInt32();
            var X = packet.ReadSingle();
            var Y = packet.ReadSingle();
            var Z = packet.ReadSingle();

            var spawn = new ItemSpawn();
            spawn.Position.X = X;
            spawn.Position.Y = Y;
            spawn.Position.Z = Z;
            spawn.ItemId = (ushort)itemId;
            spawn.Taken = false;
            spawn.ItemUid = client.GetStage().GetTraits().WorldItemUid;
            spawn.NextSpawn = DateTime.Now.AddMilliseconds(WorldItemManager.GetTime(itemId));

            Interlocked.Increment(ref client.GetStage().GetTraits().WorldItemUid);

            lock (client.GetStage().GetTraits().WorldItems)
                client.GetStage().GetTraits().WorldItems.Add(spawn);

            lock (client.GetStage().ObjectLock)
                Battle.SpawnWorldItem(client.GetStage().GetTraits().Players,
                                       spawn);
        }

        [PacketHandler(Operation.GameRequestSpawn, PacketFlags.Stage)]
        public static void ProcessGameSpawn(Client client, PacketReader packetReader)
        {
            var uid = packetReader.ReadUInt64();
            var xpos = packetReader.ReadSingle();
            var ypos = packetReader.ReadSingle();
            var zpos = packetReader.ReadSingle();
            var xdir = packetReader.ReadSingle();
            var ydir = packetReader.ReadSingle();
            var zdir = packetReader.ReadSingle();

            if (client.GetStage() != null)
            {
                var traits = client.GetStage().GetTraits();

                if (traits.Ruleset.IsDuel())
                {
                    if (traits.DuelQueue.Challenger != client && traits.DuelQueue.Champion != client)
                        return;
                }
                if (traits.Ruleset.IsTeam())
                    return;
                
                var position = new Position();
                var direction = new Direction();

                position.X = xpos;
                position.Y = ypos;
                position.Z = zpos;

                direction.X = xdir;
                direction.Y = ydir;
                direction.Z = zdir;
                lock (client.GetStage().ObjectLock)
                {
                    Battle.GameSpawn(client.GetStage().GetTraits().Players, client.GetMuid(),
                                     position, direction);
                }
            }
        }

        [PacketHandler(Operation.StageRelayMapInfo, PacketFlags.Stage)]
        public static void ProcessRelayMap(Client pClient, PacketReader pPacket)
        {
            var uid = pPacket.ReadMuid();
            var type = pPacket.ReadInt32();
            var count = pPacket.ReadInt32();
            var totalSize = pPacket.ReadInt32();
            var elementSize = pPacket.ReadInt32();
            var elementCount = pPacket.ReadInt32();

            if (elementCount < 0)
                return;

            var stage = pClient.GetStage().GetTraits();

            stage.RelayMaps = new RelayMapInfo();
            stage.RelayMaps.RepeatCount = count;
            stage.RelayMaps.RelayType = type;

            stage.RelayMaps.Maps = new List<RelayMaps>();
            for (var i = 0; i < elementCount; ++i)
            {
                var map = pPacket.ReadInt32();
                Log.Write("Found map: {0}", (RelayMaps)map);
                stage.RelayMaps.Maps.Add((RelayMaps)map);
            }
            stage.Map = (stage.RelayMaps.Maps[0]).ToString();
            stage.CurrentMap = Globals.Maps.GetMap(stage.RelayMaps.Maps[0].ToString());
        }

      }
}

